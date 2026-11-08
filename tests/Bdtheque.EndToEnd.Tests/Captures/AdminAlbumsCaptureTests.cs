extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The list of the albums of the administration, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose albums and dates are fixed: the list, its filters unfolded, and the dialog
/// of the deletion of an album.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminAlbumsCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["list", "filters", "deletion"];

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
            "filters" => $"{AppPaths.AdminAlbums}?filters=open",
            "deletion" => $"{AppPaths.AdminAlbums}?delete={await AlbumIdAsync("Le Nid des Marsupilamis")}",
            _ => AppPaths.AdminAlbums,
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-albums-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> AlbumIdAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<AlbumAdminList>($"/admin/albums?q={Uri.EscapeDataString(title)}");
        return Assert.Single(list!.Albums.Items).Album.Id;
    }
}
