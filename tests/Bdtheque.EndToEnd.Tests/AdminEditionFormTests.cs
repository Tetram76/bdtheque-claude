extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The entry form of an edition and of an edition targeted by a purchase intent (visuel/saisie-edition/contenu.md), on the
/// application shared by the tests: each test creates its own album and publisher, named with a marker of its own. Each journey
/// of the administrator, through the browser: a new edition from the page of the editions of its album, its modification, a new
/// purchase intent on a new edition and its modification, the check of an ISBN, and the error of a saving.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminEditionFormTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task NewEdition_FromThePageOfTheEditions_IsCreated_AndLeadsBackToIt()
    {
        var publisher = await CreatePublisherAsync();
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection{_marker}", publisher.Version));
        var album = await CreateAlbumAsync();
        var page = await OpenAsync(AppPaths.AdminAlbumEditions(album.Id));

        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvelle édition" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewAlbumEdition(album.Id)));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Nouvelle édition");
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur(obligatoire)", Exact = true }).PressSequentiallyAsync($"Éditeur{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Éditeur{_marker}" }).ClickAsync();
        var collectionField = page.GetByRole(AriaRole.Combobox, new() { Name = "Collection" });
        await Assertions.Expect(collectionField).ToBeEnabledAsync();
        await collectionField.SelectOptionAsync(new SelectOptionValue { Label = $"Collection{_marker}" });
        await page.GetByLabel("Année d'édition").FillAsync("1963");
        await page.GetByLabel("Mode d'acquisition").SelectOptionAsync(new SelectOptionValue { Label = "Achat" });
        await page.GetByLabel("Montant").First.FillAsync("10,95");
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'édition" }).ClickAsync();

        // A record created leads back to its list: for an edition, the page of the editions of its album.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        var edition = Assert.Single((await GetEditionsPageAsync(album.Id)).Editions).Edition;
        var saved = await GetEditionAsync(album.Id, edition.Id);
        Assert.Equal((collection.Id, 1963, AcquisitionMode.Purchase, 10.95m, "EUR"),
            (saved.Content.PublisherCollectionId, saved.Content.PublicationYear, saved.Content.AcquisitionMode, saved.Content.AcquisitionAmount,
                saved.Content.AcquisitionCurrency));
    }

    [Fact]
    public async Task Edition_Modified_FreeOnceGiven_IsSaved()
    {
        var publisher = await CreatePublisherAsync();
        var album = await CreateAlbumAsync();
        var edition = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, 1963, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, null, 10m, "EUR", false, null, null, null, null),
            album.Version));
        var page = await OpenAsync(AppPaths.AdminEditEdition(album.Id, edition.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await Assertions.Expect(page.GetByText("Prix d'achat", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByLabel("Mode d'acquisition").SelectOptionAsync(new SelectOptionValue { Label = "Offerte" });
        // The labels follow the mode (fonctionnel/presentation/libelles-contextuels-sur-l-edition.md).
        await Assertions.Expect(page.GetByText("Valeur d'acquisition", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByLabel("Gratuite").CheckAsync();
        await Assertions.Expect(page.GetByLabel("Montant").First).ToBeDisabledAsync();
        await Assertions.Expect(page.GetByLabel("Montant").First).ToHaveValueAsync("");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        var saved = await GetEditionAsync(album.Id, edition.Id);
        Assert.Equal((AcquisitionMode.Gift, true, (decimal?)null), (saved.Content.AcquisitionMode, saved.Content.IsFree, saved.Content.AcquisitionAmount));
    }

    [Fact]
    public async Task NewPurchaseIntent_OnANewEdition_IsCreated_ThenModifiedFromItsMenu()
    {
        var publisher = await CreatePublisherAsync();
        var album = await CreateAlbumAsync();
        var page = await OpenAsync(AppPaths.AdminAlbumEditions(album.Id));

        await page.GetByRole(AriaRole.Button, new() { Name = "Nouvelle intention d'achat" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Sur une nouvelle édition…" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewPurchaseIntent(album.Id)));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        // The minimum of fields: neither copy nor value (fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention).
        await Assertions.Expect(page.GetByLabel("Mode d'acquisition")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur(obligatoire)", Exact = true }).PressSequentiallyAsync($"Éditeur{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Éditeur{_marker}" }).ClickAsync();
        await page.GetByLabel("Année d'édition").FillAsync("2013");
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'intention d'achat" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        await Assertions.Expect(page.Locator(".intent-row")).ToContainTextAsync($"Éditeur{_marker} [2013]");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention : Éditeur{_marker} [2013]" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Modifier l'intention" }).ClickAsync();
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier l'intention d'achat");
        await page.GetByLabel("Année d'édition").FillAsync("2014");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        await Assertions.Expect(page.Locator(".intent-row")).ToContainTextAsync($"Éditeur{_marker} [2014]");
    }

    [Fact]
    public async Task Isbn_Inconsistent_IsAWarning_AndTheEditionIsSavedWithIt()
    {
        var publisher = await CreatePublisherAsync();
        var album = await CreateAlbumAsync();
        var page = await OpenAsync(AppPaths.AdminNewAlbumEdition(album.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur(obligatoire)", Exact = true }).PressSequentiallyAsync($"Éditeur{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Éditeur{_marker}" }).ClickAsync();
        var isbn = page.GetByLabel("ISBN");
        await isbn.FillAsync("978-2-203-00120-6");
        await Assertions.Expect(page.Locator("#edition-isbn-help")).ToHaveTextAsync("Chiffre de contrôle correct.");
        await isbn.FillAsync("978-2-203-00120-7");
        await Assertions.Expect(page.Locator("#edition-isbn-help")).ToContainTextAsync("Chiffre de contrôle incohérent");
        await page.GetByLabel("Mode d'acquisition").SelectOptionAsync(new SelectOptionValue { Label = "Échange" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'édition" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        var edition = Assert.Single((await GetEditionsPageAsync(album.Id)).Editions).Edition;
        Assert.Equal("978-2-203-00120-7", edition.Isbn);
    }

    [Fact]
    public async Task Save_WithoutModeOfAcquisition_IsToCorrect_OnTheMode_WhichGetsTheFocus()
    {
        await CreatePublisherAsync();
        var album = await CreateAlbumAsync();
        var page = await OpenAsync(AppPaths.AdminNewAlbumEdition(album.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur(obligatoire)", Exact = true }).PressSequentiallyAsync($"Éditeur{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Éditeur{_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'édition" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("À corriger");
        var mode = page.GetByLabel("Mode d'acquisition");
        await Assertions.Expect(mode).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(mode).ToBeFocusedAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewAlbumEdition(album.Id)));
    }

    [Fact]
    public async Task Amount_ATextThatIsNoAmount_ShowsAgainTheAmountThatWillBeSaved()
    {
        // The field shows what will be saved: a text that is no amount is not kept on screen while the previous amount is sent.
        await CreatePublisherAsync();
        var album = await CreateAlbumAsync();
        var page = await OpenAsync(AppPaths.AdminNewAlbumEdition(album.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        var price = page.Locator("#edition-price");
        await price.FillAsync("douze");
        await price.PressAsync("Tab");
        await Assertions.Expect(price).ToHaveValueAsync("");

        await price.FillAsync("12,5");
        await price.PressAsync("Tab");
        await Assertions.Expect(price).ToHaveValueAsync("12,5");
        await price.FillAsync("douze");
        await price.PressAsync("Tab");

        await Assertions.Expect(price).ToHaveValueAsync("12,5");
    }

    [Fact]
    public async Task Visuals_Added_Refused_Retyped_Reordered_AndDeleted_EachWrittenAtOnce()
    {
        // visuel/saisie-edition/contenu.md, Les visuels: every write on its own, at once, the form left as it was.
        var publisher = await CreatePublisherAsync();
        var album = await CreateAlbumAsync();
        var edition = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, 1963, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Gift, false, null, null, null, false, null, null, null, null),
            album.Version));
        var page = await OpenAsync(AppPaths.AdminEditEdition(album.Id, edition.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        var visuals = page.Locator(".edition-visuals");
        var marks = visuals.Locator(".edition-visual-mark");

        // Three plates, sent one at a time; a file that is no image, refused with its reason.
        await page.GetByLabel("Ajoutés comme").SelectOptionAsync(new SelectOptionValue { Label = "Planche" });
        await visuals.GetByLabel("Choisir des images à ajouter").SetInputFilesAsync(
        [
            Png("planche-01.png", SKColors.SteelBlue), Png("planche-02.png", SKColors.IndianRed), Png("planche-03.png", SKColors.Goldenrod),
            new FilePayload { Name = "notes.txt", MimeType = "text/plain", Buffer = "pas une image"u8.ToArray() },
        ]);
        await Assertions.Expect(visuals.Locator(".edition-visuals-count")).ToHaveTextAsync("3 visuels");
        await Assertions.Expect(visuals.Locator(".edition-visuals-upload[data-refused] .edition-visuals-upload-reason"))
            .ToContainTextAsync("n'est pas une image lisible");
        await Assertions.Expect(marks).ToHaveTextAsync(["Pl. 1", "Pl. 2", "Pl. 3"]);
        var plates = (await GetVisualsAsync(album.Id, edition.Id)).Visuals.Select(v => v.Id).ToList();

        // The first plate becomes the cover, which comes first.
        await visuals.GetByRole(AriaRole.Button, new() { Name = "Actions sur Planche 1" }).ClickAsync();
        await page.Locator(".actions-menu-panel:popover-open").GetByRole(AriaRole.Button, new() { Name = "Couverture", Exact = true }).ClickAsync();
        await Assertions.Expect(marks).ToHaveTextAsync(["Couv.", "Pl. 1", "Pl. 2"]);

        // The last plate dragged onto the first takes its place.
        await visuals.Locator(".edition-visual").Nth(2).DragToAsync(visuals.Locator(".edition-visual").Nth(1));
        await Assertions.Expect(visuals.Locator(".edition-visual img").Nth(1)).ToHaveAttributeAsync("src", VisualMediaRoutes.Display(plates[2]));
        Assert.Equal([plates[0], plates[2], plates[1]], (await GetVisualsAsync(album.Id, edition.Id)).Visuals.Select(v => v.Id));

        // A plate deleted once confirmed.
        await visuals.GetByRole(AriaRole.Button, new() { Name = "Actions sur Planche 2" }).ClickAsync();
        await page.Locator(".actions-menu-panel:popover-open").GetByRole(AriaRole.Button, new() { Name = "Supprimer le visuel…" }).ClickAsync();
        await page.Locator("dialog[open]").GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();
        await Assertions.Expect(visuals.Locator(".edition-visuals-count")).ToHaveTextAsync("2 visuels");
        Assert.Equal([plates[0], plates[2]], (await GetVisualsAsync(album.Id, edition.Id)).Visuals.Select(v => v.Id));

        // The form, untouched, saves under the version the visuals left.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("Aucune modification");
        await page.GetByLabel("Année d'édition").FillAsync("1964");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbumEditions(album.Id)));
        Assert.Equal(1964, (await GetEditionAsync(album.Id, edition.Id)).Content.PublicationYear);
    }

    // A small image of one color, a scan as far as api is concerned.
    private static FilePayload Png(string name, SKColor color)
    {
        using var bitmap = new SKBitmap(60, 80);
        bitmap.Erase(color);
        using var data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        return new FilePayload { Name = name, MimeType = "image/png", Buffer = data.ToArray() };
    }

    private async Task<EditionVisualsForm> GetVisualsAsync(Guid albumId, Guid editionId)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<EditionVisualsForm>($"/admin/albums/{albumId}/editions/{editionId}/visuals"))!;
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private Task<PublisherForm> CreatePublisherAsync() =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur{_marker}", null, []));

    private Task<AlbumForm> CreateAlbumAsync() =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Les Bijoux de la Castafiore {_marker}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [],
            [], []));

    private async Task<AlbumEditionsPage> GetEditionsPageAsync(Guid albumId)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<AlbumEditionsPage>($"/admin/albums/{albumId}/editions-and-purchase-intents"))!;
    }

    private async Task<EditionForm> GetEditionAsync(Guid albumId, Guid id)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<EditionForm>($"/admin/albums/{albumId}/editions/{id}"))!;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
