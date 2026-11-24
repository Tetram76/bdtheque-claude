extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the ISBN to check, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose edition with an inconsistent ISBN and dates are fixed: the list of the
/// errors to correct, its filters unfolded, and every error, each telling its state.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminInvalidIsbnsCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "filters", "all"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory, EachTheme]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var path = state switch
        {
            "filters" => $"{AppPaths.AdminInvalidIsbns}?filters=open",
            "all" => $"{AppPaths.AdminInvalidIsbns}?state=All",
            _ => AppPaths.AdminInvalidIsbns,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-invalid-isbns-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
