extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The home page of the administration, captured in each offered theme on the application of its own data
/// (<see cref="AdministrationApplication"/>): it presents the whole base, which the other tests change.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminHomeCaptureTests(AdministrationApplication application)
{
    public static TheoryData<string, string> Captures => HomeCaptures.All;

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string viewport)
    {
        var page = await application.NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        CaptureStore.Verify(theme, $"admin-home.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
