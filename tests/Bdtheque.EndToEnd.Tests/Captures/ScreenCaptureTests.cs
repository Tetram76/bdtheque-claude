extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// Each screen implemented, captured in each offered theme, the theme being fixed rather than drawn: any difference with
/// the reference capture fails (choix-implementation/socle/themes.md, captures de référence). A screen
/// implemented adds itself to <see cref="Screens"/>; a theme added to the catalog gets its captures of every screen.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class ScreenCaptureTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    /// <summary>The widths of the mockups: a desktop screen and a smartphone (visuel/charte-graphique/maquettes.md).</summary>
    internal static readonly Dictionary<string, ViewportSize> Viewports = new()
    {
        ["desktop"] = new ViewportSize { Width = 1440, Height = 900 },
        ["smartphone"] = new ViewportSize { Width = 414, Height = 896 },
    };

    private static readonly Dictionary<string, string> Screens = new()
    {
        ["not-yet-implemented"] = AppPaths.NotYetImplemented,
        ["not-found"] = "/no-such-page",
        ["error"] = "/Error",
        ["audience"] = AppPaths.AudienceInformation,
        ["admin-login"] = AppPaths.AdminLogin,
        ["admin-logout"] = AppPaths.AdminLogout,
        // Not a screen: the page of the tests that presents the controls shared by the screens, until they use them.
        ["controls"] = Controls.ControlsPage.Path,
        // Nor this one: blocks side by side and one above the other, on which a theme draws its cases, until a screen does.
        ["cases"] = Theming.CasesPage.Path,
    };

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var screen in Screens.Keys)
            foreach (var viewport in Viewports.Keys)
                captures.Add(theme.Key, screen, viewport);
            return captures;
        }
    }

    [LinuxTheory, EachTheme]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string screen, string viewport)
    {
        var page = await NewCapturePageAsync(theme, Viewports[viewport]);
        await PageReadiness.GotoAsync(page, Screens[screen]);

        CaptureStore.Verify(theme, $"{screen}.{viewport}", await CaptureAsync(page));
    }

    // An element stuck to the bottom of the screen, such as the bar of an entry form (visuel/saisie-album/contenu.md).
    private const string HasABarStuckToTheBottom = """
        () => [...document.querySelectorAll("*")].some(element => {
            const style = getComputedStyle(element);
            return style.position === "sticky" && style.bottom !== "auto";
        })
        """;

    /// <summary>
    /// The capture of the page once loaded, the way every screen is captured. A page with a bar stuck to the bottom of the
    /// screen is captured in a window as high as itself (choix-implementation/socle/themes.md, captures de référence):
    /// the bar lies at the bottom of the page, under the form, as at the end of its scrolling, and the capture does not
    /// change the window. A capture of the whole page beyond the window would leave the bar at the bottom of the window,
    /// over the form; and, enlarging the window while it is taken, it would change how the browser composites the bar, at
    /// a fraction of a pixel after the content above it: two renderings, depending on when it was taken (#343).
    /// </summary>
    internal static async Task<byte[]> CaptureAsync(IPage page)
    {
        if (await page.EvaluateAsync<bool>(HasABarStuckToTheBottom))
        {
            var height = await page.EvaluateAsync<int>("() => document.documentElement.scrollHeight");
            if (height != page.ViewportSize!.Height)
            {
                await page.SetViewportSizeAsync(page.ViewportSize.Width, height);
                await PageReadiness.WaitAsync(page);
            }
        }

        await page.EvaluateAsync("() => document.fonts.ready");
        return await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
        });
    }
}
