extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The whole change log, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose writes and their dates are fixed: the change log, its filters unfolded, and a
/// search, the entries of one album.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminChangeLogCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "filters", "search"];

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
            "filters" => $"{AppPaths.AdminChangeLog}?filters=open",
            "search" => $"{AppPaths.AdminChangeLog}?q=Marsupilamis",
            _ => AppPaths.AdminChangeLog,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-change-log-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
