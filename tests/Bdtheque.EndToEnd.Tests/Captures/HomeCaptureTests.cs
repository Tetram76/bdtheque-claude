extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The two versions of the home page (fonctionnel/page-accueil.md), each captured in each offered theme on the application
/// of its own data (<see cref="EmptyCollectionApplication"/>, <see cref="CollectionApplication"/>): its rendering depends on
/// the whole collection, which the other tests change.
/// </summary>
[Collection(EmptyCollectionApplication.Name)]
public sealed class EmptyCollectionHomeCaptureTests(EmptyCollectionApplication application) : BrowserTest<EmptyCollectionApplication>(application)
{
    public static TheoryData<string, string> Captures => HomeCaptures.All;

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string viewport) =>
        await HomeCaptures.VerifyAsync(await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]), "home-empty", theme, viewport);
}

[Collection(CollectionApplication.Name)]
public sealed class DashboardHomeCaptureTests(CollectionApplication application) : BrowserTest<CollectionApplication>(application)
{
    public static TheoryData<string, string> Captures => HomeCaptures.All;

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string viewport) =>
        await HomeCaptures.VerifyAsync(await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]), "home", theme, viewport);
}

internal static class HomeCaptures
{
    public static TheoryData<string, string> All
    {
        get
        {
            var captures = new TheoryData<string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, viewport);
            return captures;
        }
    }

    public static async Task VerifyAsync(IPage page, string screen, string theme, string viewport)
    {
        await PageReadiness.GotoAsync(page, AppPaths.Home);

        CaptureStore.Verify(theme, $"{screen}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
