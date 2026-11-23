extern alias frontend;

using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The indicators of the audience measurement in the administration, captured in each offered theme on the application of its
/// own usages (<see cref="AudienceApplication"/>), by a browser that opposes the measurement: the indicators are those of the
/// base, whatever the captures taken before.
/// </summary>
[Collection(AudienceApplication.Name)]
public sealed class AdminStatisticsCaptureTests(AudienceApplication application) : BrowserTest<AudienceApplication>(application)
{
    public static TheoryData<string, string> Captures => HomeCaptures.All;

    [LinuxTheory, EachTheme]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string viewport)
    {
        var page = await AudienceApplication.OpenAsync(
            await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]), Application, AppPaths.AdminStatistics);

        CaptureStore.Verify(theme, $"admin-statistics.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }
}
