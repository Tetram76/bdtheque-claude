using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The recording of the screens displayed and of the actions triggered (fonctionnel/mesure-audience.md), in the visit of
/// the browser, never failing the page it serves.
/// </summary>
public sealed class AudienceRecorderTests
{
    private const string SafariOnAnIPhone =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0 Mobile/15E148 Safari/604.1";

    private static readonly BrowserVisit Visit = new(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3));
    private static readonly AudienceFunction Screen = new("screen", AudienceFunctionKind.Screen);
    private static readonly AudienceFunction Action = new("action", AudienceFunctionKind.Action);

    [Fact]
    public async Task FirstUsage_OpensTheVisit_WhichTheBrowserKeeps()
    {
        var api = new FakeAudienceApi();
        var request = RequestOf(Visit, userAgent: SafariOnAnIPhone);
        var record = new PresentedRecord(RecordType.Album, Guid.CreateVersion7());

        await RecorderOf(request, api).RecordScreenAsync(Screen, record);
        await request.StartResponseAsync();

        var (path, opened) = Assert.Single(api.Sent);
        Assert.Equal("/audience/visits", path);
        Assert.Equal(
            new OpenVisitRequest(false, DeviceType.Smartphone, RenderingEngine.WebKit, "26.0", new FunctionUse("screen", record, null)),
            opened.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web));
        Assert.Equal((Visit with { AudienceVisit = new(api.OpenedVisit, false) }).Format(), request.SetCookie(BrowserVisit.CookieName));
    }

    [Fact]
    public async Task Usage_OfAVisitAlreadyOpened_IsAddedToIt()
    {
        var api = new FakeAudienceApi();
        var audienceVisit = Guid.CreateVersion7();
        var request = RequestOf(Visit with { AudienceVisit = new(audienceVisit, false) });

        await RecorderOf(request, api).RecordActionAsync(Action, "page=2");
        await request.StartResponseAsync();

        var (path, use) = Assert.Single(api.Sent);
        Assert.Equal($"/audience/visits/{audienceVisit}/usages", path);
        Assert.Equal(new FunctionUse("action", null, "page=2"), use.Deserialize<FunctionUse>(JsonSerializerOptions.Web));
        Assert.Empty(request.SetCookies);
    }

    [Fact]
    public async Task Usage_OfAVisitNoLongerKnown_OpensAnother()
    {
        // Deleted at the end of its retention, or with the data of the deployment: the browser goes on in a new one.
        var api = new FakeAudienceApi { UsagesAnswer = HttpStatusCode.NotFound };
        var request = RequestOf(Visit with { AudienceVisit = new(Guid.CreateVersion7(), false) });

        await RecorderOf(request, api).RecordScreenAsync(Screen);
        await request.StartResponseAsync();

        Assert.Equal(2, api.Sent.Count);
        Assert.Equal("/audience/visits", api.Sent[1].Path);
        Assert.Equal((Visit with { AudienceVisit = new(api.OpenedVisit, false) }).Format(), request.SetCookie(BrowserVisit.CookieName));
    }

    [Fact]
    public async Task Visit_OfTheAdministrator_IsToldApart()
    {
        var api = new FakeAudienceApi();

        await RecorderOf(RequestOf(Visit, authenticated: true), api).RecordScreenAsync(Screen);

        Assert.True(api.Sent.Single().Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.IsAdministrator);
    }

    // A visit is that of the administrator or of a visitor from its opening: once the authentication changed (login,
    // logout, end of the session), the usage goes into another visit, of its kind, which the browser keeps.
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Usage_OnceTheAuthenticationChanged_OpensAVisitOfItsKind(bool visitOfAdministrator, bool authenticated)
    {
        var api = new FakeAudienceApi();
        var request = RequestOf(Visit with { AudienceVisit = new(Guid.CreateVersion7(), visitOfAdministrator) }, authenticated: authenticated);

        await RecorderOf(request, api).RecordScreenAsync(Screen);
        await request.StartResponseAsync();

        var (path, opened) = Assert.Single(api.Sent);
        Assert.Equal("/audience/visits", path);
        Assert.Equal(authenticated, opened.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.IsAdministrator);
        Assert.Equal((Visit with { AudienceVisit = new(api.OpenedVisit, authenticated) }).Format(), request.SetCookie(BrowserVisit.CookieName));
    }

    [Fact]
    public async Task Ipad_ThatAskedForTheWebsiteOfAComputer_IsATablet()
    {
        var api = new FakeAudienceApi();
        var request = new TestRequest(
            new Dictionary<string, string> { [BrowserVisit.CookieName] = Visit.Format(), [AudienceDevice.TouchScreenCookieName] = "1" },
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.1 Safari/605.1.15");

        await RecorderOf(request, api).RecordScreenAsync(Screen);

        Assert.Equal(DeviceType.Tablet, api.Sent.Single().Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.DeviceType);
    }

    [Fact]
    public async Task Visitor_WhoOpposes_IsNotRecorded()
    {
        var api = new FakeAudienceApi();
        var request = new TestRequest(new Dictionary<string, string>
        {
            [BrowserVisit.CookieName] = Visit.Format(),
            [AudienceOpposition.CookieName] = "1",
        });

        await RecorderOf(request, api).RecordScreenAsync(Screen);
        await request.StartResponseAsync();

        Assert.Empty(api.Sent);
        Assert.Empty(request.SetCookies);
    }

    [Fact]
    public async Task Usage_TheApiDoesNotRecord_LeavesThePageAlone()
    {
        // The measurement serves the administrator, never at the expense of the page the user asked for.
        var api = new FakeAudienceApi { VisitsAnswer = HttpStatusCode.InternalServerError };
        var request = RequestOf(Visit);

        await RecorderOf(request, api).RecordScreenAsync(Screen);
        await request.StartResponseAsync();

        Assert.Single(api.Sent);
        Assert.Empty(request.SetCookies);
    }

    [Fact]
    public async Task Function_IsRecordedAsWhatItIs()
    {
        var recorder = RecorderOf(RequestOf(Visit), new FakeAudienceApi());

        await Assert.ThrowsAsync<ArgumentException>(() => recorder.RecordScreenAsync(Action));
        await Assert.ThrowsAsync<ArgumentException>(() => recorder.RecordActionAsync(Screen));
    }

    [Fact]
    public async Task Usage_OnceTheResponseStarted_IsAnError()
    {
        // An interactive circuit, without a request of its own: its visit could not be kept.
        var request = RequestOf(Visit);
        request.MarkStarted();

        await Assert.ThrowsAsync<InvalidOperationException>(() => RecorderOf(request, new FakeAudienceApi()).RecordScreenAsync(Screen));
    }

    private static TestRequest RequestOf(BrowserVisit visit, string? userAgent = null, bool authenticated = false) =>
        new(new Dictionary<string, string> { [BrowserVisit.CookieName] = visit.Format() }, userAgent, authenticated);

    private static AudienceRecorder RecorderOf(TestRequest request, FakeAudienceApi api)
    {
        var visits = new VisitCookie(request.Accessor);
        return new AudienceRecorder(
            request.Accessor, visits, new AudienceOpposition(request.Accessor, visits),
            api.Client(), NullLogger<AudienceRecorder>.Instance);
    }
}
