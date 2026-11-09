extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The record of an album, captured in each offered theme on the application of the records (<see cref="RecordsApplication"/>),
/// whose albums and dates are fixed: an album with its editions, its visuals and a collective drawing, as it opens, then with the
/// viewer open on a plate, then, on a smartphone, whose line does not hold every drawer, the authors of a role unfolded; an album targeted
/// by a purchase intent, and one out of the collection, each marked as such, without edition.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class AlbumRecordCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["album", "viewer", "unfolded", "intent", "out"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys.Where(viewport => state != "unfolded" || viewport == "smartphone"))
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(page, state switch
        {
            "intent" => AppPaths.Album(Application.IntendedAlbumId),
            "out" => AppPaths.Album(Application.CataloguedAlbumId),
            // The viewer open on a plate, above the record.
            "viewer" => AlbumRecordQuery.ForVisual(Application.FirstEditionId, 2).Href(Application.AlbumId),
            _ => AppPaths.Album(Application.AlbumId),
        });
        if (state == "unfolded")
        {
            await page.Locator(".contributions-toggle:visible summary").ClickAsync();
            // Off the names, which the pointer would leave hovered.
            await page.Mouse.MoveAsync(0, 0);
            // At the top, where the header that stays on screen is in each capture.
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }

        CaptureStore.Verify(theme, $"album-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
