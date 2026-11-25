extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The merge of two authors and of two series, captured in each offered theme on the application of the merges
/// (<see cref="MergeApplication"/>), whose dates are fixed, without merging anything (visuel/fusion/fusion-de-deux-fiches.md): that
/// of two authors as it opens, two data left to choose, then every data chosen, with the confirmation open; that of two series as
/// it opens, with the credits, the genres and the publisher of either series. In a window as high as the page, the bar of the merge at its bottom
/// (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(MergeApplication.Name)]
public sealed class AdminMergeCaptureTests(MergeApplication application) : BrowserTest<MergeApplication>(application)
{
    private static readonly string[] States = ["authors", "authors-confirmation", "series"];

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
        await ApplicationHost.SignInAsync(page, state == "series"
            ? $"{AppPaths.AdminMergeSeries}?series={Application.Asterix.Id}&other={Application.AsterixAgain.Id}"
            : $"{AppPaths.AdminMergeAuthors}?author={Application.Gotlib.Id}&other={Application.MarcelGotlib.Id}");
        await PageReadiness.WaitAsync(page);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        // A value is named after its record too on a smartphone: « Gotlieb » is only that of Gotlib.
        if (state == "authors-confirmation")
        {
            await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).GetByRole(AriaRole.Radio, new() { Name = "Gotlieb" }).CheckAsync();
            await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Biographie" }).GetByRole(AriaRole.Radio, new() { Name = "Né à Paris" }).CheckAsync();
            await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync("Affiché : Gotlib");
            await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alertdialog)).ToBeVisibleAsync();
        }
        else if (state == "series")
        {
            // The title, the status, the completeness and the format differ; the publisher, of the first series alone, and the
            // sort key entered by hand, against the one following the title, are retained.
            await Assertions.Expect(page.GetByText("4 données restent à choisir")).ToBeAttachedAsync();
        }
        else
        {
            // Attached rather than visible: the long state is hidden on a smartphone, which shows the short one.
            await Assertions.Expect(page.GetByText("2 données restent à choisir")).ToBeAttachedAsync();
        }

        CaptureStore.Verify(theme, $"admin-merge-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
