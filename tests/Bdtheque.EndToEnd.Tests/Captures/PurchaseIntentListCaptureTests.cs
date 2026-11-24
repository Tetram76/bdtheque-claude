extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The public list of the purchase intents, captured in each offered theme on the application of the records
/// (<see cref="RecordsApplication"/>), whose intents and dates are fixed: one on a whole album wished for, without cover, and
/// one on an edition of an album of the collection, whose publisher leads to its site.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class PurchaseIntentListCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
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
        await PageReadiness.GotoAsync(page, AppPaths.PurchaseIntents);

        CaptureStore.Verify(theme, $"purchase-intents.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
