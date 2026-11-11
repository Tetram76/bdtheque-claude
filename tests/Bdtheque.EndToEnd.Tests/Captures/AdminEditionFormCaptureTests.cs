extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The entry form of an edition, captured in each offered theme on the application of the administration
/// (<see cref="AdministrationApplication"/>), whose editions and dates are fixed, without writing anything: the owned edition of
/// « Le Nid des Marsupilamis », as read; the same edition given rather than bought, free, its ISBN mistyped, as the user left it
/// before saving; the edition targeted by its purchase intent; and a new edition of « Z comme Zorglub », whose series proposes
/// nothing, whose saving is to correct. In a window as high as the page, the bar of the form at its bottom
/// (<see cref="ScreenCaptureTests.CaptureAsync"/>).
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminEditionFormCaptureTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    private static readonly string[] States = ["edit", "edit-changed", "intent", "new-error"];

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
        var album = await AlbumIdAsync("Le Nid des Marsupilamis");
        var path = state switch
        {
            "new-error" => AppPaths.AdminNewAlbumEdition(await AlbumIdAsync("Z comme Zorglub")),
            "intent" => AppPaths.AdminEditEdition(album, Assert.Single((await EditionsPageAsync(album)).PurchaseIntents).Edition!.Id),
            _ => AppPaths.AdminEditEdition(album, Assert.Single((await EditionsPageAsync(album)).Editions).Edition.Id),
        };
        var page = await NewCapturePageAsync(theme, ScreenCaptureTests.Viewports[viewport]);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);

        if (state == "edit-changed")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            await page.GetByLabel("Mode d'acquisition").SelectOptionAsync(new SelectOptionValue { Label = "Offerte" });
            await page.GetByLabel("Gratuite").CheckAsync();
            await Assertions.Expect(page.GetByLabel("Montant").First).ToBeDisabledAsync();
            var isbn = page.GetByLabel("ISBN");
            await isbn.FillAsync("978-2-8001-0012-7");
            await Assertions.Expect(page.Locator("#edition-isbn-help")).ToContainTextAsync("Chiffre de contrôle incohérent");
            await isbn.BlurAsync();
            // Attached rather than visible: the long state is hidden on a smartphone, which shows the short one.
            await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToBeAttachedAsync();
        }
        else if (state == "new-error")
        {
            await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
            await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'édition" }).ClickAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
            // The field to correct gets the focus once the error is rendered (EntryForm.RevealProblemAsync): left only once it
            // has it, it would otherwise get it after the field is left, and be captured.
            var publisher = page.Locator("#edition-publisher");
            await Assertions.Expect(publisher).ToBeFocusedAsync();
            await publisher.BlurAsync();
        }

        CaptureStore.Verify(theme, $"admin-edition-{state}.{viewport}", await ScreenCaptureTests.CaptureAsync(page));
    }

    private async Task<AlbumEditionsPage> EditionsPageAsync(Guid album)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<AlbumEditionsPage>($"/admin/albums/{album}/editions-and-purchase-intents"))!;
    }

    private async Task<Guid> AlbumIdAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var list = await api.GetFromJsonAsync<AlbumAdminList>($"/admin/albums?q={Uri.EscapeDataString(title)}");
        return Assert.Single(list!.Albums.Items).Album.Id;
    }
}
