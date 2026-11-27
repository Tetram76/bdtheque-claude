extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The login of an administrator account with a second factor (fonctionnel/structure-de-l-application.md), as a
/// deployment exposed in HTTPS presents it: the code under the password, in each offered theme
/// (choix-implementation/socle/themes.md, captures de référence). Without second factor, the login is captured by
/// <see cref="ScreenCaptureTests"/>.
/// </summary>
[Collection(SecondFactorApplication.Name)]
public sealed class AdminSecondFactorLoginCaptureTests(SecondFactorApplication application) : BrowserTest<SecondFactorApplication>(application)
{
    public static TheoryData<string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, viewport);
            return captures;
        }
    }

    [LinuxTheory, EachTheme]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string viewport)
    {
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(page, AppPaths.AdminLogin);

        CaptureStore.Verify(theme, $"admin-login-code.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
