extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the series of the administration, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose series and dates are fixed: the list, its filters unfolded, and the dialog
/// of the deletion of a series.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminSeriesCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "filters", "deletion"];

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
        var path = state switch
        {
            "filters" => $"{AppPaths.AdminSeries}?filters=open",
            "deletion" => $"{AppPaths.AdminSeries}?delete={await SeriesIdAsync("Bone")}",
            _ => AppPaths.AdminSeries,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-series-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> SeriesIdAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<SeriesAdminList>($"/admin/series?q={Uri.EscapeDataString(title)}");
        return Assert.Single(list!.Series.Items).Id;
    }
}
