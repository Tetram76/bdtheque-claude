using System.Net.Http.Json;
using Bdtheque.Api.Audience;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Retention of the audience measurement (choix-implementation/mesure-audience.md § Mesure d'audience : mise en
/// œuvre): a month is closed into counters once its first visits reach 25 months, a public visit removed 25 months
/// after its beginning, and the indicators of a period read the same before and after.
/// </summary>
public sealed class AudienceRetentionTests : IClassFixture<ApiWebApplicationFactory>
{
    // Closes March and May 2028 (25 months after their first day), not June; removes the public visits begun
    // before 2028-04-10.
    private static readonly DateTimeOffset Now = new(2030, 6, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AudienceRetentionTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task ClosingTheMonths_KeepsTheIndicators_RemovesOnlyThePublicVisitsAtTheEndOfTheirRetention()
    {
        var album = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        var other = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());
        var publicMarch = await VisitAsync(new(2028, 3, 15, 9, 0, 0, TimeSpan.Zero), false, "130", album, album, other);
        var adminMarch = await VisitAsync(new(2028, 3, 20, 9, 0, 0, TimeSpan.Zero), true, null, other);
        var publicMay = await VisitAsync(new(2028, 5, 20, 9, 0, 0, TimeSpan.Zero), false, "130", album);
        var publicJune = await VisitAsync(new(2028, 6, 5, 9, 0, 0, TimeSpan.Zero), false, null, other, album);
        var before = await IndicatorsAsync();

        await RunRetentionAsync();

        // A record compares its list of months by reference: compared apart, month by month.
        var after = await IndicatorsAsync();
        Assert.Equal(before with { Months = null! }, after with { Months = null! });
        Assert.Equal(before.Months, after.Months);
        var visits = await VisitIdsAsync();
        Assert.DoesNotContain(publicMarch, visits);
        Assert.Contains(adminMarch, visits);
        Assert.Contains(publicMay, visits);
        Assert.Contains(publicJune, visits);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Equal(
            [new DateOnly(2028, 3, 1), new DateOnly(2028, 5, 1)],
            await context.AudienceCounters.Where(c => c.Function == Function && c.Month >= new DateOnly(2028, 1, 1))
                .Select(c => c.Month).Distinct().OrderBy(m => m).ToListAsync());
        Assert.Equal(
            (7, 4, 1, 2, 3),
            (before.UsageCount, before.VisitCount, before.RepeatedUsageCount, before.DistinctObjectCount, before.Months.Count));
    }

    [Fact]
    public async Task ClosingTheMonthsAgain_ChangesNothing()
    {
        await VisitAsync(new(2027, 1, 10, 9, 0, 0, TimeSpan.Zero), false, null, new PresentedRecord(RecordType.Series, Guid.CreateVersion7()));
        await RunRetentionAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var counters = await context.AudienceCounters.CountAsync();

        await RunRetentionAsync();

        Assert.Equal(counters, await context.AudienceCounters.CountAsync());
    }

    private const string Function = "catalog.album";

    private Task<FunctionIndicators> IndicatorsAsync() =>
        _client.GetFromJsonAsync<List<FunctionIndicators>>("/admin/audience/indicators?from=2028-03-01&to=2028-06-01")
            .ContinueWith(task => Assert.Single(task.Result!, i => i.Function == Function));

    private async Task<List<Guid>> VisitIdsAsync() =>
        (await _client.GetFromJsonAsync<Page<AudienceVisitItem>>("/admin/audience/visits?pageSize=100"))!.Items.Select(v => v.Id).ToList();

    private async Task RunRetentionAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AudienceRetention>().RunAsync(Now, CancellationToken.None);
    }

    /// <summary>Records a visit presenting <paramref name="records"/> in turn, then moves it back to <paramref name="begin"/>.</summary>
    private async Task<Guid> VisitAsync(DateTimeOffset begin, bool isAdministrator, string? engineVersion, params PresentedRecord[] records)
    {
        var response = await _client.PostAsJsonAsync(
            "/audience/visits",
            new OpenVisitRequest(isAdministrator, DeviceType.Computer, RenderingEngine.Blink, engineVersion, new FunctionUse(Function, records[0], null)));
        var visitId = (await response.Content.ReadFromJsonAsync<OpenedVisit>())!.VisitId;
        foreach (var record in records.Skip(1))
            (await _client.PostAsJsonAsync($"/audience/visits/{visitId}/usages", new FunctionUse(Function, record, null))).EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        await context.AudienceVisits.Where(v => v.Id == visitId).ExecuteUpdateAsync(s => s.SetProperty(v => v.CreatedAt, begin));
        var usages = await context.AudienceUsages.Where(u => u.VisitId == visitId).OrderBy(u => u.CreatedAt).ThenBy(u => u.Id).Select(u => u.Id).ToListAsync();
        for (var i = 0; i < usages.Count; i++)
        {
            var date = begin.AddMinutes(i);
            await context.AudienceUsages.Where(u => u.Id == usages[i]).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, date));
        }

        return visitId;
    }
}
