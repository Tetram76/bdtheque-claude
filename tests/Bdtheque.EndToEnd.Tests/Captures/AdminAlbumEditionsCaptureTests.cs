extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The page of the editions and purchase intents of an album, captured in each offered theme on the application of the
/// administration (<see cref="AdministrationApplication"/>), whose albums and dates are fixed: an album of the collection with
/// its edition and an intent on another one, the dialog of the deletion of that edition, which takes the album out of the
/// collection, an album targeted by an intent on itself, and an album without edition nor intent.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminAlbumEditionsCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["editions", "deletion", "intended", "empty"];

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
            "deletion" => await DeletionPathAsync(),
            "intended" => AppPaths.AdminAlbumEditions(await AlbumIdAsync("Le Prisonnier du Bouddha")),
            "empty" => AppPaths.AdminAlbumEditions(await AlbumIdAsync("Z comme Zorglub")),
            _ => AppPaths.AdminAlbumEditions(await AlbumIdAsync("Le Nid des Marsupilamis")),
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        CaptureStore.Verify(theme, $"admin-album-editions-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<string> DeletionPathAsync()
    {
        var album = await AlbumIdAsync("Le Nid des Marsupilamis");
        using var api = Application.CreateApiClient();
        var page = await api.GetFromJsonAsync<AlbumEditionsPage>($"/admin/albums/{album}/editions-and-purchase-intents");
        return AlbumEditionsQuery.ForEditionDeletion(Assert.Single(page!.Editions).Edition.Id).Href(album);
    }

    private async Task<Guid> AlbumIdAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<AlbumAdminList>($"/admin/albums?q={Uri.EscapeDataString(title)}");
        return Assert.Single(list!.Albums.Items).Album.Id;
    }
}
