extern alias frontend;

using Bdtheque.EndToEnd.Tests.Theming;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// A page is judged ready on its own state (<see cref="PageReadiness"/>), never on the quiet of the network: a page whose
/// stylesheet did not load is quiet all the same, and was captured as it was, without the sheet that paints its theme (#283).
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class PageReadinessTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 900 };

    [Fact]
    public async Task PageWhoseThemeStylesheetDidNotLoad_IsNeverReady_AndNamesTheStylesheet()
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Planche.Key, Desktop);
        await page.RouteAsync(url => new Uri(url).AbsolutePath.StartsWith("/themes/planche.", StringComparison.Ordinal)
            && new Uri(url).AbsolutePath.EndsWith(".css", StringComparison.Ordinal), route => route.AbortAsync());

        var failure = await Assert.ThrowsAsync<PlaywrightException>(() => PageReadiness.GotoAsync(page, CasesPage.Path));

        Assert.Contains("Stylesheets not applied", failure.Message, StringComparison.Ordinal);
        Assert.Contains("/themes/planche.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PageWithAllItsStylesheets_IsReady_OnceTheScriptOfItsThemeHasDrawnIt()
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Planche.Key, Desktop);

        await PageReadiness.GotoAsync(page, CasesPage.Path);

        Assert.True(await page.EvaluateAsync<bool>(
            "() => [...document.querySelectorAll('.block')].every(block => block.dataset.drawn !== undefined)"));
    }
}
