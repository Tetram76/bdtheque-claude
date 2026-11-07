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
    [InlineData("desktop")]
    [InlineData("smartphone")]
    public async Task APagePaintedBeforeItChanges_IsCapturedAsThePagePaintedOnceSettled(string viewport)
    {
        var settled = await NewCapturePageAsync("base", ScreenCaptureTests.Viewports[viewport]);
        await PageReadiness.GotoAsync(settled, ControlsPage.Path);
        await SettleAsync(settled);

        var painted = await NewCapturePageAsync("base", ScreenCaptureTests.Viewports[viewport]);
        await painted.GotoAsync(ControlsPage.Path, new PageGotoOptions { WaitUntil = WaitUntilState.Commit });
        await painted.WaitForSelectorAsync(".record-picker", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
        // A capture paints the page as it is: here, before the choice of a record is interactive.
        await painted.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        await PageReadiness.WaitAsync(painted);
        await SettleAsync(painted);

        var comparison = CaptureComparison.Compare(await ScreenCaptureTests.CaptureAsync(settled), await ScreenCaptureTests.CaptureAsync(painted));

        Assert.True(comparison.Identical, $"The page painted before it changed differs in {comparison.DifferentPixels} pixels.");
    }

    // Both pages in the same state: the choice of a record rendered by its circuit, then whatever this rendering changed
    // in the page laid out (the chevron of the menu row).
    private static async Task SettleAsync(IPage page)
    {
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await PageReadiness.WaitAsync(page);
    }
}
