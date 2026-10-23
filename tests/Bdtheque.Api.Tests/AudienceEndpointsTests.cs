using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Recording of the audience measurement by the frontend (<c>/audience</c>), and reading of its visits and
/// indicators in the administration (<c>/admin/audience</c>), while the months are still open: every indicator is
/// computed from the detail of the visits.
/// </summary>
public sealed class AudienceEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AudienceEndpointsTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task AVisit_IsOpenedWithItsFirstUsage_ThenFollowed_AndReadAsItsPath()
    {
        var album = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        var function = Function("catalog.album");

        var visitId = await OpenAsync(false, DeviceType.Tablet, RenderingEngine.WebKit, "605.1", new FunctionUse(function, album, null));
        await RecordAsync(visitId, new FunctionUse(function, album, null));
        await RecordAsync(visitId, new FunctionUse(Function("catalog.search"), null, "{\"q\":\"lotus\"}"));

        var detail = await _client.GetFromJsonAsync<AudienceVisitDetail>($"/admin/audience/visits/{visitId}");

        Assert.Equal(new AudienceVisitItem(visitId, detail!.Usages[0].Date, false, DeviceType.Tablet, RenderingEngine.WebKit, "605.1", 3), detail.Visit);
        Assert.Equal([false, true, false], detail.Usages.Select(u => u.IsRepeat));
        Assert.Equal([album, album, null], detail.Usages.Select(u => u.Record));
        Assert.Equal("{\"q\":\"lotus\"}", detail.Usages[2].Parameters);
        Assert.True(detail.Usages.Select(u => u.Date).SequenceEqual(detail.Usages.Select(u => u.Date).Order()));
    }

    [Fact]
    public async Task Visits_ReadFromTheLatestToTheOldest()
    {
        var older = await OpenAsync(true, DeviceType.Computer, RenderingEngine.Gecko, null, new FunctionUse(Function("admin.home"), null, null));
        var newer = await OpenAsync(false, DeviceType.Smartphone, RenderingEngine.Blink, "130", new FunctionUse(Function("dashboard"), null, null));

        var visits = await _client.GetFromJsonAsync<Page<AudienceVisitItem>>("/admin/audience/visits?pageSize=100");

        var ids = visits!.Items.Select(v => v.Id).ToList();
        Assert.True(ids.IndexOf(newer) < ids.IndexOf(older));
    }

    [Fact]
    public async Task AUsage_OfAnUnknownVisit_IsAFunctionalError()
    {
        var response = await _client.PostAsJsonAsync(
            $"/audience/visits/{Guid.CreateVersion7()}/usages", new FunctionUse(Function("dashboard"), null, null));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task AUsage_PresentingARecordAndBearingParameters_IsATechnicalError()
    {
        var response = await _client.PostAsJsonAsync(
            "/audience/visits",
            new OpenVisitRequest(false, DeviceType.Computer, RenderingEngine.Blink, null,
                new FunctionUse(Function("admin.album.save"), new PresentedRecord(RecordType.Album, Guid.CreateVersion7()), "{}")));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    [Fact]
    public async Task Indicators_CountUsagesVisitsRepeatsAndDistinctObjects_ByFunction()
    {
        var function = Function("catalog.album");
        var first = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        var second = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        var visit = await OpenAsync(false, DeviceType.Computer, RenderingEngine.Blink, "130", new FunctionUse(function, first, null));
        await RecordAsync(visit, new FunctionUse(function, first, null));
        await RecordAsync(visit, new FunctionUse(function, second, null));
        var other = await OpenAsync(true, DeviceType.Smartphone, RenderingEngine.WebKit, null, new FunctionUse(function, first, null));

        var indicators = await IndicatorsAsync(function, "");

        Assert.Equal((4, 2, 1, 2), (indicators.UsageCount, indicators.VisitCount, indicators.RepeatedUsageCount, indicators.DistinctObjectCount));
        var detail = await _client.GetFromJsonAsync<AudienceVisitDetail>($"/admin/audience/visits/{other}");
        Assert.Equal(detail!.Usages[0].Date, indicators.LastUsage);
        var month = Assert.Single(indicators.Months);
        Assert.Equal((4, 2), (month.UsageCount, month.VisitCount));
    }

    [Fact]
    public async Task Indicators_AreRestrictedToTheCompartmentRead()
    {
        var function = Function("catalog.series");
        await OpenAsync(false, DeviceType.Computer, RenderingEngine.Blink, "130", new FunctionUse(function, null, null));
        await OpenAsync(false, DeviceType.Computer, RenderingEngine.Blink, "131", new FunctionUse(function, null, null));
        await OpenAsync(true, DeviceType.Tablet, RenderingEngine.Gecko, null, new FunctionUse(function, null, null));

        Assert.Equal(2, (await IndicatorsAsync(function, "administrator=false")).VisitCount);
        Assert.Equal(1, (await IndicatorsAsync(function, "administrator=true")).VisitCount);
        Assert.Equal(1, (await IndicatorsAsync(function, "deviceType=Tablet")).VisitCount);
        Assert.Equal(2, (await IndicatorsAsync(function, "renderingEngine=Blink")).VisitCount);
        Assert.Equal(1, (await IndicatorsAsync(function, "renderingEngine=Blink&engineVersion=131")).VisitCount);
    }

    [Fact]
    public async Task Indicators_AreRestrictedToThePeriodRead()
    {
        var function = Function("dashboard");
        await OpenAsync(false, DeviceType.Computer, RenderingEngine.Blink, null, new FunctionUse(function, null, null));
        var thisMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var during = await _client.GetFromJsonAsync<List<FunctionIndicators>>(
            $"/admin/audience/indicators?from={thisMonth:yyyy-MM-dd}&to={thisMonth:yyyy-MM-dd}");
        var before = await _client.GetFromJsonAsync<List<FunctionIndicators>>(
            $"/admin/audience/indicators?to={thisMonth.AddMonths(-1):yyyy-MM-dd}");

        Assert.Contains(during!, i => i.Function == function);
        Assert.DoesNotContain(before!, i => i.Function == function);
    }

    private async Task<FunctionIndicators> IndicatorsAsync(string function, string query)
    {
        var indicators = await _client.GetFromJsonAsync<List<FunctionIndicators>>($"/admin/audience/indicators?{query}");
        return Assert.Single(indicators!, i => i.Function == function);
    }

    private async Task<Guid> OpenAsync(
        bool isAdministrator, DeviceType deviceType, RenderingEngine engine, string? engineVersion, FunctionUse firstUse)
    {
        var response = await _client.PostAsJsonAsync("/audience/visits", new OpenVisitRequest(isAdministrator, deviceType, engine, engineVersion, firstUse));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OpenedVisit>())!.VisitId;
    }

    private async Task RecordAsync(Guid visitId, FunctionUse use)
    {
        var response = await _client.PostAsJsonAsync($"/audience/visits/{visitId}/usages", use);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // The tests share the database of their class: each uses functions of its own.
    private static string Function(string name) => $"{name}.{Guid.NewGuid():N}";
}
