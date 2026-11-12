extern alias frontend;

using frontend::Bdtheque.Frontend.Formatting;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Visits;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The welcome screen of the first request of a browser (choix-implementation/socle/transverse/internationalisation.md,
/// écran d'accueil; visuel/ecran-accueil/): what only the browser knows is written by its script, then the page comes,
/// already rendered with it, at the address asked for.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class WelcomeScreenTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    // A page of the consultation that depends on no data, at an address with a query and a fragment, both kept.
    private const string Address = AppPaths.NotYetImplemented + "?from=welcome#top";

    [Theory, InEachTheme]
    public async Task FirstRequestOfABrowser_GetsTheWelcomeScreen_ThenThePage_AtTheAddressAskedFor(string theme)
    {
        var page = await NewPageAsync(theme, new BrowserNewContextOptions { TimezoneId = "Asia/Tokyo" });
        var documents = await DocumentsOfAsync(page);

        await PageReadiness.GotoAsync(page, Address);

        var (screen, content) = documents[0];
        Assert.Contains("data-welcome", content, StringComparison.Ordinal);
        Assert.Contains("no-store", screen.Headers["cache-control"], StringComparison.Ordinal);
        Assert.Equal(2, documents.Count);
        Assert.EndsWith(Address, page.Url, StringComparison.Ordinal);
        Assert.Equal("Asia/Tokyo", await page.Locator("html").GetAttributeAsync("data-time-zone"));
        Assert.Equal(theme, await page.Locator("html").GetAttributeAsync("data-theme"));
    }

    [Theory, InEachTheme]
    public async Task BrowserThatGaveItsTimeZone_GetsThePageAtOnce(string theme)
    {
        var page = await NewPageAsync(theme);
        await page.Context.AddCookiesAsync([new Cookie
        {
            Name = BrowserTimeZone.CookieName, Value = ApplicationHost.BrowserTimeZoneId, Url = Application.BaseAddress.ToString(),
        }]);
        var documents = await DocumentsOfAsync(page);

        await PageReadiness.GotoAsync(page, Address);

        Assert.DoesNotContain("data-welcome", Assert.Single(documents).Content, StringComparison.Ordinal);
    }

    [Theory, InEachTheme]
    public async Task BrowserWithoutScripts_GetsThePage_Marked(string theme)
    {
        // Scripts disabled, as for a robot that renders pages: sent to the page by the screen itself, never to the screen again.
        var page = await NewPageAsync(theme, new BrowserNewContextOptions { JavaScriptEnabled = false });

        await page.GotoAsync(Address);

        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("data-theme", theme);
        Assert.Contains($"{BrowserWelcome.SkipParameter}=", page.Url, StringComparison.Ordinal);
        Assert.Null(await page.Locator("html").GetAttributeAsync("data-time-zone"));
    }

    [Theory, InEachTheme]
    public async Task ClientThatIsNotABrowser_GetsThePage(string theme)
    {
        // A robot or the preview of a link, which neither runs scripts nor sends the metadata of a navigation.
        var context = await NewContextAsync(theme);

        var response = await context.APIRequest.GetAsync(Address);

        Assert.Contains($"data-theme=\"{theme}\"", await response.TextAsync(), StringComparison.Ordinal);
    }

    // The documents of the page loads at its address, with their content, as the server answers them: the welcome screen,
    // then the page. Each is noted before the browser gets it, so in their order, and complete once the page is ready.
    private static async Task<List<(IAPIResponse Response, string Content)>> DocumentsOfAsync(IPage page)
    {
        var documents = new List<(IAPIResponse, string)>();
        await page.RouteAsync(url => new Uri(url).AbsolutePath == AppPaths.NotYetImplemented, async route =>
        {
            var response = await route.FetchAsSentAsync();
            var content = await response.TextAsync();
            lock (documents)
                documents.Add((response, content));
            await route.FulfillAsync(new RouteFulfillOptions { Response = response });
        });
        return documents;
    }
}
