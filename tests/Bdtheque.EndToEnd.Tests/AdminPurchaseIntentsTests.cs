extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the purchase intents of the administration (visuel/listes/liste-par-liste.md), on the application shared by the
/// tests: each test names its albums with a marker of its own, and narrows the list to them by searching it. What the list
/// shares with that of the albums (the modal dialog, Escape) is pinned by AdminAlbumsTests.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminPurchaseIntentsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Intentions d'acquisition" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Intentions d'acquisition");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task Search_OnTheTitleOfTheSeries_NarrowsTheList(string theme)
    {
        var series = await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Le Château des étoiles {_marker}", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []));
        await CreateAlbumIntentAsync(await CreateAlbumAsync(null, series.Id, 4));
        await CreateAlbumIntentAsync(await CreateAlbumAsync($"Les Vieux Fourneaux {_marker}"));
        var page = await OpenAsync(theme, AppPaths.AdminPurchaseIntents);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une intention d'acquisition" }).FillAsync($"Château des étoiles {_marker}");
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une intention d'acquisition" }).PressAsync("Enter");

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Le Château des étoiles {_marker} - T. 4"]);
    }

    [Theory, InEachTheme]
    public async Task Search_ThenTheNavigationByInitialOfTheAlbums_NarrowTheList(string theme)
    {
        // An intent is filed under the entry of its album: « Le Lotus bleu » under L.
        await CreateAlbumIntentAsync(await CreateAlbumAsync($"Le Lotus bleu {_marker}"));
        await CreateAlbumIntentAsync(await CreateAlbumAsync($"Zorglub {_marker}"));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "Z", Exact = true }).ClickAsync();

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Zorglub {_marker}"]);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents, $"?q={_marker}&entry=Z"));
    }

    [Theory, InEachTheme]
    public async Task Removal_OfAnIntentOnAnEdition_DoesNotAnnounceTheEditionTargeted_AndTakesTheIntentOutOfTheList(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Dupuis {_marker}", null, []));
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 1974, null, null, null, null, null, null, null, true), await AlbumVersionAsync(album.Id)));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention d'acquisition de Buddy Longway {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Retirer l'intention…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Retirer cette intention d'acquisition ?" });
        // The edition targeted is the intent itself, which is not counted in what it takes along
        // (fonctionnel/regles-metier/suppression-des-entites.md); without visuals, nothing else is concerned.
        await Assertions.Expect(dialog).ToContainTextAsync("Aucune autre fiche n'est concernée.");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucune intention d'acquisition ne correspond à « {_marker} ».");
    }

    [Theory, InEachTheme]
    public async Task Confirmation_OfAnIntentOnAnEdition_RecordsTheAcquisition_AndLeadsBackToTheListWithoutTheIntent(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Dupuis {_marker}", null, []));
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 1974, null, null, null, null, null, null, null, true), await AlbumVersionAsync(album.Id)));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Confirmer l'acquisition : Buddy Longway {_marker}" }).ClickAsync();
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Confirmer l'acquisition");
        await page.GetByLabel("Mode d'acquisition").SelectOptionAsync(new SelectOptionValue { Label = "Achat" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmer l'acquisition" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents));
        var editions = await EditionsPageAsync(album.Id);
        Assert.Empty(editions.PurchaseIntents);
        Assert.Equal(AcquisitionMode.Purchase, Assert.Single(editions.Editions).AcquisitionMode);
    }

    [Theory, InEachTheme]
    public async Task Confirmation_OfAnIntentOnTheWholeAlbum_CreatesItsFirstEdition(string theme)
    {
        var album = await CreateAlbumAsync(null, (await CreateSeriesWithPublisherAsync()).Id, 3);
        await CreateAlbumIntentAsync(album);
        var intent = Assert.Single((await EditionsPageAsync(album.Id)).PurchaseIntents);
        var page = await OpenAsync(theme, AppPaths.AdminConfirmPurchase(album.Id, intent.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        await page.GetByLabel("Mode d'acquisition").SelectOptionAsync(new SelectOptionValue { Label = "Offerte" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmer l'acquisition" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents));
        var editions = await EditionsPageAsync(album.Id);
        Assert.Empty(editions.PurchaseIntents);
        Assert.Equal(AcquisitionMode.Gift, Assert.Single(editions.Editions).AcquisitionMode);
    }

    [Theory, InEachTheme]
    public async Task ConversionToAnEdition_FromTheList_EntersTheEditionTargeted_AndLeadsBackToTheList(string theme)
    {
        var album = await CreateAlbumAsync(null, (await CreateSeriesWithPublisherAsync()).Id, 5);
        await CreateAlbumIntentAsync(album);
        var page = await OpenAsync(theme, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention d'acquisition de La Mémoire de l'eau {_marker} - T. 5" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Viser plutôt une édition…" }).ClickAsync();
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await page.GetByLabel("Année d'édition").FillAsync("2012");
        await page.GetByRole(AriaRole.Button, new() { Name = "Viser cette édition" }).ClickAsync();

        // Back where the action was opened (decision of the user, #612), without its criteria, as after any record saved.
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents));
        var intent = Assert.Single((await EditionsPageAsync(album.Id)).PurchaseIntents);
        Assert.Equal(2012, intent.Edition!.PublicationYear);
    }

    [Theory, InEachTheme]
    public async Task ConversionToTheAlbum_FromTheList_ConfirmedInADialog_DeletesTheEditionTargeted(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Dupuis {_marker}", null, []));
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 1974, null, null, null, null, null, null, null, true), await AlbumVersionAsync(album.Id)));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention d'acquisition de Buddy Longway {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Viser plutôt l'album…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Viser plutôt l'album ?" });
        await Assertions.Expect(dialog).ToContainTextAsync("l'édition visée est supprimée");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Viser l'album" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Table, new() { Name = "Intentions d'acquisition" })).ToContainTextAsync("toute édition");
        var editions = await EditionsPageAsync(album.Id);
        Assert.Null(Assert.Single(editions.PurchaseIntents).Edition);
        Assert.Empty(editions.Editions);
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1024)]
    [InEachTheme(1180)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheLabelItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        var publisher = await PostAsync<PublisherForm>(
            "/admin/publishers", new PublisherContent($"Les Éditions du Grand Large et des Archipels réunis {_marker}", null, []));
        var album = await CreateAlbumAsync($"Les Voyages extraordinaires du capitaine Archibald au-delà des mers {_marker}");
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 2014, "978-2-7234-4567-1", null, null, null, null, null, null, true),
            await AlbumVersionAsync(album.Id)));
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");
        await PageReadiness.WaitAsync(page);

        var label = await page.Locator(".row-entry").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        // At the width of the mockups' desktop, the table has all its columns, the membership of the collection included.
        if (width == 1440)
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Possession" })).ToBeVisibleAsync();
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Intentions d'acquisition" }).Locator(".row-entry");

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId = null, int? volume = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []));

    private async Task CreateAlbumIntentAsync(AlbumForm album) =>
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, album.Version));

    // A series whose new editions are proposed its publisher, so that a new edition of its albums is saved without choosing one.
    private async Task<SeriesForm> CreateSeriesWithPublisherAsync()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Glénat {_marker}", null, []));
        return await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"La Mémoire de l'eau {_marker}", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(publisher.Id, null, null, null, null, null, null, null, null), [], [], []));
    }

    private async Task<AlbumEditionsPage> EditionsPageAsync(Guid albumId)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<AlbumEditionsPage>($"/admin/albums/{albumId}/editions-and-purchase-intents"))!;
    }

    private async Task<uint> AlbumVersionAsync(Guid albumId)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version;
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
