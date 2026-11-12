extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The record of an author, captured in each offered theme on the application of the records (<see cref="RecordsApplication"/>),
/// whose albums and dates are fixed: an author without biography nor nationality and its whole bibliography as it opens,
/// then, on a smartphone, every series unfolded; an author known under a pseudonym, credited on nothing yet.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class AuthorRecordCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["author", "unfolded", "uncredited"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys.Where(viewport => state != "unfolded" || viewport == "smartphone"))
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory, EachTheme]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(page, AppPaths.AuthorRecord(state == "uncredited" ? Application.UncreditedAuthorId : Application.AuthorId));
        if (state == "unfolded")
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Tout déplier" }).ClickAsync();
            // At the top, where the header that stays on screen is in each capture.
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }

        CaptureStore.Verify(theme, $"author-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
