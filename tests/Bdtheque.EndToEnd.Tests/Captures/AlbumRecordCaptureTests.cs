extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The record of an album, captured in each offered theme on the application of the records (<see cref="RecordsApplication"/>),
/// whose albums and dates are fixed: an album with a collective drawing, as it opens, then the authors of a role unfolded.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class AlbumRecordCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["album", "unfolded"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(page, AppPaths.Album(Application.AlbumId));
        if (state == "unfolded")
        {
            await page.GetByText("et 3 autres").ClickAsync();
            // Off the names, which the pointer would leave hovered.
            await page.Mouse.MoveAsync(0, 0);
            // At the top, where the header that stays on screen is in each capture.
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }

        CaptureStore.Verify(theme, $"album-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
