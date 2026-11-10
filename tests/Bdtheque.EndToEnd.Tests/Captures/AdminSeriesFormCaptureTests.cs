extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The entry form of a series, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose series and dates are fixed, without writing anything: a series with its
/// publisher, collection, authors and new editions, and a new series whose saving is to correct, its new editions pre-filled and its collection unavailable
/// until a publisher is chosen. In a window as high as the page, the bar of the form at its bottom
/// (<see cref="ScreenCaptureTests.CaptureAtFullHeightAsync"/>).
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminSeriesFormCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["edit", "new-error"];

    public static TheoryData<string, string, string> Captures
    {
        get
        {
            var captures = new TheoryData<string, string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var state in States)
            foreach (var viewport in ScreenCaptureTests.Viewports.Keys)
                captures.Add(theme.Key, state, viewport);
            return captures;
        }
    }

    [LinuxTheory]
    [MemberData(nameof(Captures))]
    public async Task Screen_LooksLikeItsReference(string theme, string state, string viewport)
    {
        var path = state == "new-error" ? AppPaths.AdminNewSeries : AppPaths.AdminEditSeries(await SeriesIdAsync("Barracuda"));
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        if (state == "new-error")
        {
            await ControlsPage.WaitUntilInteractiveAsync(page);
            await page.GetByRole(AriaRole.Button, new() { Name = "Créer la série" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
            // The title to correct has the focus, which is not captured.
            await page.GetByLabel("Titre").BlurAsync();
        }

        CaptureStore.Verify(theme, $"admin-series-{state}.{viewport}", await ScreenCaptureTests.CaptureAtFullHeightAsync(page));
    }

    private async Task<Guid> SeriesIdAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<SeriesAdminList>($"/admin/series?q={Uri.EscapeDataString(title)}");
        return Assert.Single(list!.Series.Items).Id;
    }
}
