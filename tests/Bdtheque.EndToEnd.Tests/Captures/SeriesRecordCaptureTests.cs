extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The record of a series, captured in each offered theme on the application of the records (<see cref="RecordsApplication"/>),
/// whose albums and dates are fixed: a series with its publisher, collection, authors and summary, whose albums are one of the
/// collection with its cover, one wished for and one catalogued only; and a series just created, without album, author, genre
/// nor status.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class SeriesRecordCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["series", "empty"];

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
        await PageReadiness.GotoAsync(page, AppPaths.SeriesRecord(state == "empty" ? Application.EmptySeriesId : Application.SeriesId));

        CaptureStore.Verify(theme, $"series-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
