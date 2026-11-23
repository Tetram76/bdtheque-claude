extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The search of a record to modify, from the header of the administration, captured in each offered theme on the application
/// of the administration (<see cref="AdministrationApplication"/>), whose records are fixed: the records found of several types
/// (a series, its albums, their intents and a universe), and a search that finds nothing.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminSearchCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly Dictionary<string, string> States = new()
    {
        ["found"] = "spirou",
        ["none"] = "zorro",
    };

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in TestThemes.Covered)
            foreach (var state in States.Keys)
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
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminSearch}?q={States[state]}");
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-search-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
