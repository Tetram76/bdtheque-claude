extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The simple search of the consultation, captured in each offered theme on the application of the records
/// (<see cref="RecordsApplication"/>), whose records are fixed: the records found of several types (a series, its albums, their
/// intents and a universe), and a search that finds nothing.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class ConsultationSearchCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly Dictionary<string, string> States = new()
    {
        ["found"] = "corto",
        ["none"] = "zorro",
    };

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var state in States.Keys)
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
        await PageReadiness.GotoAsync(page, $"{AppPaths.Search}?q={States[state]}");

        CaptureStore.Verify(theme, $"search-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
