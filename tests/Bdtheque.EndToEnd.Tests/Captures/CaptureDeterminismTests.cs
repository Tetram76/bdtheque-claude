using Bdtheque.EndToEnd.Tests.Controls;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// A capture depends on the page as it is, never on when it changed: a page that changes after its first paint is
/// captured as the same page painted once settled. The page of the controls changes after it: its choice of a record
/// renders again once interactive, and the script of the menu row shows its chevron. Whether the browser has already
/// painted it by then depends on the speed of the run; a tile redrawn in part (partial raster) has antialiased edges
/// rounded otherwise, hence captures that differed from one run to the next (#270). The browser of the captures redraws a
/// tile whole (<see cref="ApplicationHost"/>); this test paints the page before it changes, every time, to check it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class CaptureDeterminismTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [LinuxTheory]
    [InEachTheme("desktop")]
    [InEachTheme("smartphone")]
    public async Task APagePaintedBeforeItChanges_IsCapturedAsThePagePaintedOnceSettled(string theme, string viewport)
    {
        var settled = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(settled, ControlsPage.Path);
        await SettleAsync(settled);

        var painted = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await painted.GotoAsync(ControlsPage.Path, new PageGotoOptions { WaitUntil = WaitUntilState.Commit });
        await painted.WaitForSelectorAsync(".record-picker", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
        // A capture paints the page as it is: here, before the choice of a record is interactive.
        await painted.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        await PageReadiness.WaitAsync(painted);
        await SettleAsync(painted);

        var comparison = CaptureComparison.Compare(await ScreenCaptureTests.CaptureAsync(settled), await ScreenCaptureTests.CaptureAsync(painted));

        Assert.True(comparison.Identical, $"The page painted before it changed differs in {comparison.DifferentPixels} pixels.");
    }

    /// <summary>
    /// A capture of a whole page taller than the window enlarges the window while it is taken: the page no longer scrolls,
    /// and the browser composites a bar stuck to the bottom of the screen (an entry form, visuel/saisie-album/contenu.md)
    /// otherwise. Placed after the content above it, the bar lies at a fraction of a pixel, and which of the two
    /// compositions the capture shows depended on when it was taken: the dot of its state was a pixel higher in some of
    /// them (#343). Such a page is captured in a window already as high as itself, which the capture leaves unchanged.
    /// </summary>
    [Theory, InEachTheme]
    public async Task APageWithABarStuckToTheBottom_IsCapturedInAWindowAsHighAsItself(string theme)
    {
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports["smartphone"]);
        await PageReadiness.GotoAsync(page, ControlsPage.Path);
        var height = await page.EvaluateAsync<int>("() => document.documentElement.scrollHeight");
        Assert.True(height > page.ViewportSize!.Height, "The page of the controls is expected taller than the window of a smartphone.");

        await ScreenCaptureTests.CaptureAsync(page);

        Assert.Equal(height, page.ViewportSize!.Height);
    }

    /// <summary>
    /// A capture depends on the page, never on where the interactions of the test left the pointer: a click leaves it on
    /// the element clicked, which the page then moves (an error shown above it, the window enlarged by the capture), and
    /// whether the browser had already updated what lies under the pointer depended on when the capture was taken (#383).
    /// The capture is taken with the pointer off the page, as on a page that no interaction touched.
    /// </summary>
    [Fact]
    public async Task APageLeftUnderThePointer_IsCapturedWithNothingHovered()
    {
        var page = await NewCapturePageAsync("base", ScreenCaptureTests.Viewports["desktop"]);
        await PageReadiness.GotoAsync(page, ControlsPage.Path);
        var heading = page.GetByRole(AriaRole.Heading, new() { Name = "Contrôles" });
        await heading.HoverAsync();
        Assert.True(await heading.EvaluateAsync<bool>("element => element.matches(':hover')"), "The pointer is expected over the heading.");

        await ScreenCaptureTests.CaptureAsync(page);

        Assert.Equal(0, await page.EvaluateAsync<int>("() => document.querySelectorAll(':hover').length"));
    }

    // Both pages in the same state: the choice of a record rendered by its circuit, then whatever this rendering changed
    // in the page laid out (the chevron of the menu row).
    private static async Task SettleAsync(IPage page)
    {
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await PageReadiness.WaitAsync(page);
    }
}
