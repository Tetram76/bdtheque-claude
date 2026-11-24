extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The public list of the purchase intents, captured in each offered theme on the application of the records
/// (<see cref="RecordsApplication"/>), whose intents and dates are fixed: one on a whole album wished for, without cover, and
/// one on an edition of an album of the collection, whose publisher leads to its site; and the panel of the filters, those of
/// the albums without the possession.
/// </summary>
[Collection(RecordsApplication.Name)]
public sealed class PurchaseIntentListCaptureTests(RecordsApplication application) : BrowserTest<RecordsApplication>(application)
{
    private static readonly string[] States = ["all", "filters"];

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
        await PageReadiness.GotoAsync(page, state == "filters" ? $"{AppPaths.PurchaseIntents}?filters=open" : AppPaths.PurchaseIntents);

        CaptureStore.Verify(theme, $"purchase-intents-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
