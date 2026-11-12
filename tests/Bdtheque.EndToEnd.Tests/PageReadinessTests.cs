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

    // The sheet of the theme itself, or, for the base theme, which has none, that of its contracts, loaded for every theme.
    [Theory, InEachTheme]
    public async Task PageWhoseThemeStylesheetDidNotLoad_IsNeverReady_AndNamesTheStylesheet(string theme)
    {
        var stylesheet = "/" + (TestThemes.Of(theme).Stylesheet ?? "themes/base.css");
        var name = stylesheet[..stylesheet.LastIndexOf('.')] + ".";
        var page = await NewPageInThemeAsync(theme, Desktop);
        await page.RouteAsync(url => new Uri(url).AbsolutePath.StartsWith(name, StringComparison.Ordinal)
            && new Uri(url).AbsolutePath.EndsWith(".css", StringComparison.Ordinal), route => route.AbortAsync());

        var failure = await Assert.ThrowsAsync<PlaywrightException>(() => PageReadiness.GotoAsync(page, CasesPage.Path));

        Assert.Contains("Stylesheets not applied", failure.Message, StringComparison.Ordinal);
        Assert.Contains(name, failure.Message, StringComparison.Ordinal);
    }

    [Theory, InEachTheme]
    public async Task Page_IsReady_OnceBlazorHasMovedTheFocusToItsTitle(string theme)
    {
        var page = await NewPageInThemeAsync(theme, Desktop);

        await PageReadiness.GotoAsync(page, CasesPage.Path);

        Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement?.localName === 'h1'"));
    }

    // What the script of « La planche encrée », the one theme that has one, draws: a test of that theme alone.
    [Fact]
    public async Task PageWithAllItsStylesheets_IsReady_OnceTheScriptOfItsThemeHasDrawnIt()
    {
        var page = await NewPageInThemeAsync(ThemeCatalog.Planche.Key, Desktop);

        await PageReadiness.GotoAsync(page, CasesPage.Path);

        Assert.True(await page.EvaluateAsync<bool>(
            "() => [...document.querySelectorAll('.block')].every(block => block.dataset.drawn !== undefined)"));
    }
}
