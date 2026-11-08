extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the publishers of the administration, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose publishers and dates are fixed: the list, and the dialog of the deletion
/// of a publisher, which takes its collections with it.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminPublishersCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "deletion"];

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
        var path = state switch
        {
            "deletion" => $"{AppPaths.AdminPublishers}?delete={await PublisherIdAsync("Dargaud")}",
            _ => AppPaths.AdminPublishers,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-publishers-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> PublisherIdAsync(string name)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<Page<PublisherAdminListItem>>($"/admin/publishers?q={Uri.EscapeDataString(name)}");
        return Assert.Single(list!.Items).Id;
    }
}
