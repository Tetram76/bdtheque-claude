extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The record of a universe, captured in each offered theme on the application of the records (<see cref="RecordsApplication"/>),
/// whose albums and dates are fixed: a universe with its description and a sub-universe, a series attached itself, a series
/// attached through one of its albums and an album without series, as it opens.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class UniverseRecordCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
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
        await PageReadiness.GotoAsync(page, AppPaths.UniverseRecord(Application.UniverseId));

        CaptureStore.Verify(theme, $"universe.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
