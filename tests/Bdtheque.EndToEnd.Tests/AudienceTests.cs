extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Audience;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Visits;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The audience measurement of the web interface (fonctionnel/mesure-audience.md), as a browser goes through it: its
/// visit, the screens it displays and the actions it triggers, recorded by <c>api</c>, and the opposition of the visitor.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AudienceTests(ApplicationHost application)
{
    private const string SafariOnAMac =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.1 Safari/605.1.15";

    [Fact]
    public async Task FirstPage_OpensTheVisit_WithTheScreenItDisplays()
    {
        var page = await application.NewPageAsync();

        await page.GotoAsync(AppPaths.Home);

        var visit = await AudienceVisitAsync(page);
        Assert.Equal(
            (false, DeviceType.Computer, RenderingEngine.Blink),
            (visit.Visit.IsAdministrator, visit.Visit.DeviceType, visit.Visit.RenderingEngine));
        // The major version of Chromium, that of its Blink.
        Assert.Equal(application.Browser.Version.Split('.')[0], visit.Visit.EngineVersion);
        Assert.Equal([AudienceFunctions.Home.Key], visit.Usages.Select(u => u.Function));
    }

    [Fact]
    public async Task Visit_GathersThePagesOfEveryTabOfTheBrowser_InTheirOrder()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(AppPaths.Home);
        await page.GotoAsync(AppPaths.AudienceInformation);
        var tab = await page.Context.NewPageAsync();

        await tab.GotoAsync(AppPaths.AudienceInformation);

        var visit = await AudienceVisitAsync(page);
        Assert.Equal(
            [(AudienceFunctions.Home.Key, false), (AudienceFunctions.AudienceInformation.Key, false), (AudienceFunctions.AudienceInformation.Key, true)],
            visit.Usages.Select(u => (u.Function, u.IsRepeat)));
    }

    [Fact]
    public async Task EveryPage_LeadsToTheInformation_FromItsFoot()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(AppPaths.Home);

        await page.GetByRole(AriaRole.Contentinfo).GetByRole(AriaRole.Link, new() { Name = "Mesure d'audience" }).ClickAsync();

        // An enhanced navigation of Blazor: the address changes before the page.
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Mesure d'audience");
        Assert.EndsWith(AppPaths.AudienceInformation, page.Url);
    }

    [Fact]
    public async Task Visitor_WhoOpposes_IsNoLongerRecorded_UntilTheyComeBackOnIt()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(AppPaths.AudienceInformation);
        var before = (await AudienceVisitAsync(page)).Visit.Id;

        await page.GetByRole(AriaRole.Button, new() { Name = "Refuser la mesure d'audience" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Accepter de nouveau la mesure d'audience" }).WaitForAsync();
        await page.GotoAsync(AppPaths.Home);

        // Nothing recorded since: neither the opposition, nor the pages that follow; nothing of the measurement kept by the browser.
        Assert.Equal([AudienceFunctions.AudienceInformation.Key], (await AudienceVisitByIdAsync(before)).Usages.Select(u => u.Function));
        Assert.Null((await BrowserVisitAsync(page)).AudienceVisitId);
        Assert.Equal("opposed", await page.Locator("html").GetAttributeAsync("data-audience"));
        await Assertions.Expect(page.GetByRole(AriaRole.Contentinfo)).ToContainTextAsync("refusée");

        await page.GotoAsync(AppPaths.AudienceInformation);
        await page.GetByRole(AriaRole.Button, new() { Name = "Accepter de nouveau la mesure d'audience" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Refuser la mesure d'audience" }).WaitForAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Contentinfo)).ToContainTextAsync("active");
        var after = await AudienceVisitAsync(page);
        Assert.NotEqual(before, after.Visit.Id);
        Assert.Equal(
            [AudienceFunctions.AudienceOppositionWithdrawn.Key, AudienceFunctions.AudienceInformation.Key],
            after.Usages.Select(u => u.Function));
    }

    [Fact]
    public async Task Opposition_IsKeptByTheBrowser_BeyondItsSession()
    {
        var page = await application.NewPageAsync();
        await page.GotoAsync(AppPaths.AudienceInformation);
        await page.GetByRole(AriaRole.Button, new() { Name = "Refuser la mesure d'audience" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Accepter de nouveau la mesure d'audience" }).WaitForAsync();

        var opposition = (await page.Context.CookiesAsync()).Single(c => c.Name == AudienceOpposition.CookieName);

        Assert.True(opposition.Expires > DateTimeOffset.UtcNow.AddDays(364).ToUnixTimeSeconds());
        Assert.True(opposition.Expires <= DateTimeOffset.UtcNow.AddDays(366).ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Ipad_ThatAsksForTheWebsiteOfAComputer_IsATablet_FromItsNextVisit()
    {
        // The touch screen, which only the browser tells, is known from the first page on: the visits that follow are a tablet's.
        var page = await TouchScreenMacAsync();
        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Contains(await page.Context.CookiesAsync(), c => c.Name == AudienceDevice.TouchScreenCookieName);

        await page.Context.ClearCookiesAsync(new BrowserContextClearCookiesOptions { Name = BrowserVisit.CookieName });
        await page.GotoAsync(AppPaths.Home);

        var visit = await AudienceVisitAsync(page);
        Assert.Equal((DeviceType.Tablet, RenderingEngine.WebKit, "26.1"), (visit.Visit.DeviceType, visit.Visit.RenderingEngine, visit.Visit.EngineVersion));
    }

    [Fact]
    public async Task TouchScreen_IsNotTold_ByTheBrowserOfAVisitorWhoOpposes()
    {
        var page = await TouchScreenMacAsync();
        await page.Context.AddCookiesAsync([new Cookie
        {
            Name = AudienceOpposition.CookieName, Value = "1", Url = application.BaseAddress.ToString(),
        }]);

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.DoesNotContain(await page.Context.CookiesAsync(), c => c.Name == AudienceDevice.TouchScreenCookieName);
    }

    private async Task<IPage> TouchScreenMacAsync()
    {
        var context = await application.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = application.BaseAddress.ToString(),
            UserAgent = SafariOnAMac,
        });
        await context.AddInitScriptAsync("Object.defineProperty(Navigator.prototype, 'maxTouchPoints', { get: () => 5 })");
        return await context.NewPageAsync();
    }

    private static async Task<BrowserVisit> BrowserVisitAsync(IPage page) =>
        BrowserVisit.Parse(Uri.UnescapeDataString((await page.Context.CookiesAsync()).Single(c => c.Name == BrowserVisit.CookieName).Value))
        ?? throw new InvalidOperationException("The browser keeps no visit.");

    private async Task<AudienceVisitDetail> AudienceVisitAsync(IPage page) =>
        await AudienceVisitByIdAsync((await BrowserVisitAsync(page)).AudienceVisitId
            ?? throw new InvalidOperationException("The visit of the browser has no visit of the audience measurement."));

    private async Task<AudienceVisitDetail> AudienceVisitByIdAsync(Guid id)
    {
        using var api = application.CreateApiClient();
        return (await api.GetFromJsonAsync<AudienceVisitDetail>($"/admin/audience/visits/{id}"))!;
    }
}
