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
public sealed class CaptureDeterminismTests(ApplicationHost application)
{
    [LinuxTheory]
    [InlineData("desktop")]
    [InlineData("smartphone")]
    public async Task APagePaintedBeforeItChanges_IsCapturedAsThePagePaintedOnceSettled(string viewport)
    {
        var settled = await application.NewCapturePageAsync("base", ScreenCaptureTests.Viewports[viewport]);
        await settled.GotoAsync(ControlsPage.Path, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await SettleAsync(settled);

        var painted = await application.NewCapturePageAsync("base", ScreenCaptureTests.Viewports[viewport]);
        await painted.GotoAsync(ControlsPage.Path, new PageGotoOptions { WaitUntil = WaitUntilState.Commit });
        await painted.WaitForSelectorAsync(".record-picker", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
        // A capture paints the page as it is: here, before the choice of a record is interactive.
        await painted.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        await painted.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await SettleAsync(painted);

        var comparison = CaptureComparison.Compare(await ScreenCaptureTests.CaptureAsync(settled), await ScreenCaptureTests.CaptureAsync(painted));

        Assert.True(comparison.Identical, $"The page painted before it changed differs in {comparison.DifferentPixels} pixels.");
    }

    // Nothing in the page tells that its interactive rendering is done: a delay, well beyond it, lets both pages reach the
    // same state. Too short, it would only let the test pass without checking anything, never fail it.
    private static Task SettleAsync(IPage page) => page.WaitForTimeoutAsync(1500);
}
