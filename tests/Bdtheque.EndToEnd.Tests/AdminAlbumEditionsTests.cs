extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The page of the editions and purchase intents of an album (visuel/editions-album/contenu.md), on the application shared by
/// the tests: each test creates its own album, named with a marker of its own. The paths of the user: from the list of the
/// albums to the page and back, an intent set on the whole album, an edition deleted, an intent removed.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminAlbumEditionsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task ListOfTheAlbums_LeadsToThePage_WhoseFilDAraneLeadsToTheListOfTheIntents()
    {
        await CreateAlbumAsync($"Le Château des étoiles {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminAlbums}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Le Château des étoiles {_marker}" }).ClickAsync();
        await page.Locator(".actions-menu-panel:popover-open").GetByRole(AriaRole.Link, new() { Name = "Éditions et intentions d'achat" }).ClickAsync();

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Éditions et intentions d'achat");
        await Assertions.Expect(page.Locator(".album-editions-subject")).ToHaveTextAsync($"Le Château des étoiles {_marker}");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Intentions d'achat" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents));
    }

    [Fact]
    // Nothing is entered for an intent on the whole album: choosing it sets it at once (visuel/editions-album/contenu.md).
    public async Task IntentOnTheAlbum_Chosen_IsSetAtOnce_AndNoOtherIntentIsOffered()
    {
        var album = await CreateAlbumAsync($"Blacksad {_marker}");
        var page = await OpenAsync(AppPaths.AdminAlbumEditions(album.Id));

        await page.GetByRole(AriaRole.Button, new() { Name = "Nouvelle intention d'achat" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Sur l'album…" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        await Assertions.Expect(page.Locator(".intent-row")).ToContainTextAsync("toute édition");
        await Assertions.Expect(page.Locator(".album-brief .tag")).ToHaveTextAsync("Intention d'achat");
        // An album itself targeted: neither a new edition nor another intent; its first edition comes with the purchase.
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Nouvelle édition" })).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Nouvelle intention d'achat" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Deletion_OfTheOnlyOwnedEdition_AnnouncesTheAlbumLeavingTheCollection_AndLeavesNoEdition()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Casterman {_marker}", null));
        var album = await CreateAlbumAsync($"Les Bijoux de la Castafiore {_marker}");
        await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, 1963, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
            album.Version));
        var page = await OpenAsync(AppPaths.AdminAlbumEditions(album.Id));

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Casterman {_marker} [1963]" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer l'édition…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cette édition ?" });
        await Assertions.Expect(dialog).ToContainTextAsync("Sortie de la collection de 1 album");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        await Assertions.Expect(page.Locator(".album-editions-none").First).ToHaveTextAsync("Aucune édition dans la collection.");
        await Assertions.Expect(page.Locator(".album-brief .tag")).ToHaveTextAsync("Hors collection");
    }

    [Fact]
    public async Task Removal_OfAnIntentOnAnEdition_AnnouncesTheEditionRemovedWithIt_AndOffersBothTargetsAgain()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Dupuis {_marker}", null));
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 1974, null, null, null, null, null, null, null, true), album.Version));
        var page = await OpenAsync(AppPaths.AdminAlbumEditions(album.Id));

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention : Dupuis {_marker} [1974]" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Retirer l'intention…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Retirer cette intention d'achat ?" });
        await Assertions.Expect(dialog).ToContainTextAsync("Suppression aussi de 1 édition");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        await Assertions.Expect(page.Locator(".album-editions-none").Last).ToHaveTextAsync("Aucune intention d'achat.");
        // Without any edition again, the album itself may be targeted: the choice of the two targets is back.
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Nouvelle intention d'achat" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Removal_OfAnIntent_AfterTheDeletionOfTheOwnedEdition_LeavesTheAlbumWithoutEither()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Lagune {_marker}", null));
        var album = await CreateAlbumAsync($"Brouillard sur Ombrelune {_marker}");
        var owned = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, 2008, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Gift, false, null, null, null, true, null, null, null, null),
            album.Version));
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 2023, null, null, null, null, null, null, null, true), owned.AlbumVersion));
        var page = await OpenAsync(AppPaths.AdminAlbumEditions(album.Id));

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Lagune {_marker} [2008]" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer l'édition…" }).ClickAsync();
        await page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();
        await Assertions.Expect(page.Locator(".edition-row")).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention : Lagune {_marker} [2023]" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Retirer l'intention…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Retirer cette intention d'achat ?" });
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        await Assertions.Expect(page.Locator(".album-editions-none")).ToHaveTextAsync(["Aucune édition dans la collection.", "Aucune intention d'achat."]);
    }

    [Theory]
    [InlineData(414)]
    [InlineData(768)]
    [InlineData(1024)]
    [InlineData(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheLabelItsRoom_WithoutThePageOverflowing(int width)
    {
        var publisher = await PostAsync<PublisherForm>(
            "/admin/publishers", new CreatePublisherRequest($"Les Éditions du Grand Large et des Archipels réunis {_marker}", null));
        var album = await CreateAlbumAsync($"Les Voyages extraordinaires du capitaine Archibald au-delà des mers {_marker}");
        var edition = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, 2014, "978-2-7234-4567-1", BindingType.Hardcover, null, null, null, 62,
                EditionCategory.FirstEdition, false, true, null, AcquisitionMode.Purchase, false, new DateOnly(2015, 1, 2), null, null, false,
                null, null, null, null),
            album.Version));
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 2020, "978-2-7234-4568-8", BindingType.Hardcover, null, null, null, null,
                EditionCategory.LimitedEdition, true),
            edition.AlbumVersion));
        foreach (var theme in ThemeCatalog.Offered.Select(theme => theme.Key))
        {
            var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
            await ApplicationHost.SignInAsync(page, AppPaths.AdminAlbumEditions(album.Id));
            await PageReadiness.WaitAsync(page);

            var label = await page.Locator(".edition-label").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
            var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

            Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
            Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        }
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
