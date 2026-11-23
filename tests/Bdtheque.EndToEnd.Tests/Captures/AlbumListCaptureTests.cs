extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the albums of the consultation, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose albums are fixed, one in each state of the possession: the whole list, the
/// list limited to the collection, and the list that a filter empties under an entry.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AlbumListCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["all", "owned", "empty"];

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
            "owned" => $"{AppPaths.Albums}?state=InCollection",
            "empty" => $"{AppPaths.Albums}?entry=Z&state=InCollection",
            _ => AppPaths.Albums,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(page, path);

        CaptureStore.Verify(theme, $"albums-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
