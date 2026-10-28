extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// Each screen implemented, captured in each offered theme, the theme being fixed rather than drawn: any difference with
/// the reference capture fails (choix-implementation/socle.md § Thèmes : mise en œuvre, captures de référence). A screen
/// implemented adds itself to <see cref="Screens"/>; a theme added to the catalog gets its captures of every screen.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class ScreenCaptureTests(ApplicationHost application)
{
    /// <summary>The widths of the mockups: a desktop screen and a smartphone (visuel/charte-graphique.md § Maquettes).</summary>
    private static readonly Dictionary<string, ViewportSize> Viewports = new()
    {
        ["desktop"] = new ViewportSize { Width = 1440, Height = 900 },
        ["smartphone"] = new ViewportSize { Width = 414, Height = 896 },
    };

    private static readonly Dictionary<string, string> Screens = new()
    {
        ["home"] = AppPaths.Home,
        ["not-yet-implemented"] = AppPaths.NotYetImplemented,
        ["not-found"] = "/no-such-page",
        ["error"] = "/Error",
    };

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var screen in Screens.Keys)
            foreach (var viewport in Viewports.Keys)
                captures.Add(theme.Key, screen, viewport);
            return captures;
        }
    }

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string screen, string viewport)
    {
        var page = await application.NewCapturePageAsync(theme, Viewports[viewport]);
        await page.GotoAsync(Screens[screen], new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.EvaluateAsync("() => document.fonts.ready");

        var capture = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
        });

        CaptureStore.Verify(theme, $"{screen}.{viewport}", capture);
    }
}
