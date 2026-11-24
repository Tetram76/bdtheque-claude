extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the series of the consultation, captured in each offered theme on the application of the records
/// (<see cref="RecordsApplication"/>), whose series and dates are fixed: all the series, with and without albums; those of the
/// collection only, the covers of their piles taken among their owned editions; and the panel of the filters.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class SeriesListCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
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
            "owned" => $"{AppPaths.Series}?state=InCollection",
            "filters" => $"{AppPaths.Series}?filters=open",
            _ => AppPaths.Series,
        });

        CaptureStore.Verify(theme, $"series-list-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
