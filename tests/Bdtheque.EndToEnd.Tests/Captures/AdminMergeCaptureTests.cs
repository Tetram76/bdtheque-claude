extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The merge of two authors, captured in each offered theme on the application of the merges (<see cref="MergeApplication"/>),
/// whose dates are fixed, without merging anything (visuel/fusion/fusion-de-deux-fiches.md): as it opens, two data left to choose,
/// then every data chosen, with the confirmation open. In a window as high as the page, the bar of the merge at its bottom
/// (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(MergeApplication.Name)]
public sealed class AdminMergeCaptureTests(MergeApplication application) : BrowserTest<MergeApplication>(application)
{
    private static readonly string[] States = ["authors", "authors-confirmation"];

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
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminMergeAuthors}?author={Application.Gotlib.Id}&other={Application.MarcelGotlib.Id}");
        await PageReadiness.WaitAsync(page);
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        if (state == "authors-confirmation")
        {
            await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).GetByRole(AriaRole.Radio, new() { Name = "Gotlieb", Exact = true }).CheckAsync();
            await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Biographie" }).GetByRole(AriaRole.Radio, new() { Name = "Né à Paris" }).CheckAsync();
            await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync("Affiché : Gotlib");
            await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alertdialog)).ToBeVisibleAsync();
        }
        else
        {
            // Attached rather than visible: the long state is hidden on a smartphone, which shows the short one.
            await Assertions.Expect(page.GetByText("2 données restent à choisir")).ToBeAttachedAsync();
        }

        CaptureStore.Verify(theme, $"admin-merge-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
