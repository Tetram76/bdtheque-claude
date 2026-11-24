extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the authors of the consultation, captured in each offered theme on the application of the records
/// (<see cref="RecordsApplication"/>), whose authors and albums are fixed: all the authors, whatever the possession of their
/// albums; those of the collection only; and the panel of the filters.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class AuthorListCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
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
            "owned" => $"{AppPaths.Authors}?state=InCollection",
            "filters" => $"{AppPaths.Authors}?filters=open",
            _ => AppPaths.Authors,
        });

        CaptureStore.Verify(theme, $"authors-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
