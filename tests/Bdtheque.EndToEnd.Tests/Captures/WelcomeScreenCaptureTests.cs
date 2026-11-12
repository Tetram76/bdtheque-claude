extern alias frontend;

using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The welcome screen of the first request of a browser (visuel/ecran-accueil/), captured once at each width: it has the
/// common identity of the application, the same whatever the theme the visit will have, which is not drawn yet. Captured
/// without its script, which would give way to the page at once, under reduced motion, as every capture: the pile fixed.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed partial class WelcomeScreenCaptureTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    /// <summary>Where the references of what no theme changes are kept, beside those of each theme.</summary>
    public const string Common = "common";

    public static TheoryData<string> Captures => [.. ScreenCaptureTests.Viewports.Keys];

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string viewport)
    {
        // Any theme: the screen comes before it.
        var page = await NewCapturePageAsync(ThemeCatalog.Base.Key, ScreenCaptureTests.Viewports[viewport]);
        await page.RouteAsync(url => new Uri(url).AbsolutePath == AppPaths.Home, async route =>
        {
            var response = await route.FetchAsSentAsync();
            var screen = WhatGivesWayToThePage().Replace(await response.TextAsync(), "");
            await route.FulfillAsync(new RouteFulfillOptions { Response = response, Body = screen });
        });

        await page.GotoAsync(AppPaths.Home);
        // The screen alone, laid out: it has neither a stylesheet nor a font of its own to wait for.
        await (await page.WaitForFunctionAsync("() => document.readyState === 'complete' && document.documentElement.hasAttribute('data-welcome')")).DisposeAsync();
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

        CaptureStore.Verify(Common, $"welcome.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    // The script of the screen, and its way to the page for a browser without scripts.
    [GeneratedRegex("<script>.*?</script>|<noscript>.*?</noscript>", RegexOptions.Singleline)]
    private static partial Regex WhatGivesWayToThePage();
}
