extern alias frontend;

using frontend::Bdtheque.Frontend.Formatting;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The server knows the time zone of the browser, in which it presents the dates and times, even in the pages it
/// renders statically (fonctionnel/presentation.md § Langue et culture d'affichage).
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class BrowserTimeZoneTests(ApplicationHost application)
{
    [Fact]
    public async Task Server_LearnsTheTimeZoneOfTheBrowser_FromItsFirstPage()
    {
        var page = await PageInAsync("Asia/Tokyo");

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(AppPaths.Home);

        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));
    }

    [Fact]
    public async Task Browser_ThatChangesTimeZone_IsFollowed()
    {
        var context = await application.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = application.BaseAddress.ToString(),
            TimezoneId = "Europe/Paris",
        });
        await context.AddCookiesAsync([new Cookie
        {
            Name = BrowserTimeZone.CookieName, Value = "Asia/Tokyo", Url = application.BaseAddress.ToString(),
        }]);
        var page = await context.NewPageAsync();

        await page.GotoAsync(AppPaths.Home, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));
        await page.GotoAsync(AppPaths.Home);

        Assert.Equal("Europe/Paris", await page.Locator("html").GetAttributeAsync("data-time-zone"));
    }

    [Fact]
    public async Task FirstPageWithDatesAndTimes_IsRenderedAgainInTheTimeZoneOfTheBrowser_Once()
    {
        var page = await PageInAsync("Asia/Tokyo");
        var renders = await PresentingADateAndTimeAsync(page);

        await page.GotoAsync(AppPaths.Home);
        await WaitForRendersAsync(renders, 2);

        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));
        Assert.Equal(2, renders.Count);
    }

    [Fact]
    public async Task PageWithDatesAndTimes_WhenTheBrowserRefusesTheCookie_IsNotRenderedAgain()
    {
        var page = await PageInAsync("Asia/Tokyo");
        // A browser that refuses the cookies of the application: whatever the script writes, none is kept.
        await page.AddInitScriptAsync(
            "Object.defineProperty(Document.prototype, 'cookie', { get: () => '', set: () => {}, configurable: true });");
        var renders = await PresentingADateAndTimeAsync(page);

        await page.GotoAsync(AppPaths.Home);
        await WaitForRendersAsync(renders, 1);

        Assert.Null(await page.Locator("html").GetAttributeAsync("data-time-zone"));
        Assert.Single(renders);
    }

    [Fact]
    public async Task TabLeftInTheFormerTimeZone_IsRenderedAgain_WhenTheCookieIsAlreadyCurrent()
    {
        // Two tabs open when the browser changes time zone: the first one the user comes back to updates the cookie,
        // shared with the other one, which must still be rendered again in the new time zone.
        var page = await PageInAsync("Asia/Tokyo");
        await page.AddInitScriptAsync(ZoneOfTheBrowser("Asia/Tokyo"));
        var renders = await PresentingADateAndTimeAsync(page);
        await page.GotoAsync(AppPaths.Home);
        await WaitForRendersAsync(renders, 2);
        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));

        await page.Context.AddCookiesAsync([TimeZoneCookie("Europe/Paris")]);
        await page.EvaluateAsync("""
            () => {
                sessionStorage.setItem("zoneOfTheBrowser", "Europe/Paris");
                document.dispatchEvent(new Event("visibilitychange"));
            }
            """);
        await WaitForRendersAsync(renders, 3);

        Assert.Equal(3, renders.Count);
        Assert.Equal("Europe/Paris", await page.Locator("html").GetAttributeAsync("data-time-zone"));
    }

    [Fact]
    public async Task TimeZoneTheServerDoesNotKnow_IsNotRenderedAgainAndAgain()
    {
        // The server presents in universal time a time zone it does not know: the page is current all the same, the
        // request having brought the time zone of the browser.
        var page = await PageInAsync("Asia/Tokyo");
        await page.AddInitScriptAsync(ZoneOfTheBrowser("Nowhere/Atlantis"));
        var renders = await PresentingADateAndTimeAsync(page);

        await page.GotoAsync(AppPaths.Home);
        await WaitForRendersAsync(renders, 3);

        Assert.Equal(2, renders.Count);
        Assert.Equal("Nowhere/Atlantis", await page.Locator("html").GetAttributeAsync("data-time-zone"));
    }

    [Fact]
    public async Task PageWithoutDatesAndTimes_IsNotRenderedAgain()
    {
        var page = await PageInAsync("Asia/Tokyo");
        var renders = RendersOf(page);

        await page.GotoAsync(AppPaths.Home);
        await WaitForRendersAsync(renders, 1);

        Assert.Single(renders);
    }

    private async Task<IPage> PageInAsync(string timeZone)
    {
        var context = await application.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = application.BaseAddress.ToString(),
            TimezoneId = timeZone,
        });
        return await context.NewPageAsync();
    }

    // The time zone the browser reports, which the test changes where the context's is fixed. Kept for the tab, as the
    // real one is, across the reloads that the init script runs again on.
    private static string ZoneOfTheBrowser(string zone) => $$"""
        if (sessionStorage.getItem("zoneOfTheBrowser") === null) {
            sessionStorage.setItem("zoneOfTheBrowser", "{{zone}}");
        }
        const resolvedOptions = Intl.DateTimeFormat.prototype.resolvedOptions;
        Intl.DateTimeFormat.prototype.resolvedOptions = function () {
            return { ...resolvedOptions.call(this), timeZone: sessionStorage.getItem("zoneOfTheBrowser") };
        };
        """;

    private Cookie TimeZoneCookie(string zone) =>
        new() { Name = BrowserTimeZone.CookieName, Value = zone, Url = application.BaseAddress.ToString() };

    // No page presents a date and time yet: the home page served gets one, as a page that does would render it.
    private static async Task<List<string>> PresentingADateAndTimeAsync(IPage page)
    {
        await page.RouteAsync(url => new Uri(url).AbsolutePath == AppPaths.Home, async route =>
        {
            var response = await route.FetchAsync();
            var html = (await response.TextAsync()).Replace(
                "</main>", "<time datetime=\"2025-12-31T23:30:00Z\" data-local>31/12/2025 23:30</time></main>", StringComparison.Ordinal);
            await route.FulfillAsync(new RouteFulfillOptions { Response = response, Body = html });
        });
        return RendersOf(page);
    }

    // The documents the page loads: each one a rendering of the page by the server.
    private static List<string> RendersOf(IPage page)
    {
        var renders = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest && request.ResourceType == "document")
                lock (renders)
                    renders.Add(request.Url);
        };
        return renders;
    }

    // Lets the page settle, then a while longer: a rendering beyond the expected one would show up meanwhile.
    private static async Task WaitForRendersAsync(List<string> renders, int expected)
    {
        for (var waited = 0; waited < 50 && renders.Count < expected; waited++)
            await Task.Delay(100);
        await Task.Delay(1000);
    }
}
