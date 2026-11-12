extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The entry form of an album, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose albums and dates are fixed, without writing anything: an album of a
/// series, a new album whose saving is to correct, and an omnibus with the volumes it covers. In a window as high as the
/// page, the bar of the form at its bottom (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminAlbumFormCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["edit", "new-error", "omnibus"];

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
        var path = state == "new-error" ? AppPaths.AdminNewAlbum : AppPaths.AdminEditAlbum(await AlbumIdAsync("Le Nid des Marsupilamis"));
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        if (state != "edit")
            await ControlsPage.WaitUntilInteractiveAsync(page);

        if (state == "new-error")
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'album" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
            // The title to correct, captured with its focus, gets it once the error is rendered (EntryForm.RevealProblemAsync).
            await Assertions.Expect(page.Locator("#album-title")).ToBeFocusedAsync();
        }
        else if (state == "omnibus")
        {
            await page.GetByLabel("Intégrale").CheckAsync();
            await page.GetByLabel("Du tome").FillAsync("1");
            await page.GetByLabel("Au tome").FillAsync("12");
            // The field left, its focus is not captured.
            await page.GetByLabel("Au tome").BlurAsync();
            await Assertions.Expect(page.Locator(".entry-form-subject")).ToContainTextAsync("[1 à 12]");
        }

        CaptureStore.Verify(theme, $"admin-album-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<Guid> AlbumIdAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<AlbumAdminList>($"/admin/albums?q={Uri.EscapeDataString(title)}");
        return Assert.Single(list!.Albums.Items).Album.Id;
    }
}
