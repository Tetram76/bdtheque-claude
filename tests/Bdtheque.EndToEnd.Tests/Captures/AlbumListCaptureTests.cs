extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the albums of the consultation, captured in each offered theme on the application of the records
/// (<see cref="RecordsApplication"/>), whose albums and dates are fixed: all the albums, owned, targeted by an intent or
/// catalogued; those of the collection only, their covers taken among their owned editions; and the panel of the filters.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class AlbumListCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["all", "owned", "filters"];

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
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(page, state switch
        {
            "owned" => $"{AppPaths.Albums}?state=InCollection",
            "filters" => $"{AppPaths.Albums}?filters=open",
            _ => AppPaths.Albums,
        });

        CaptureStore.Verify(theme, $"albums-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
