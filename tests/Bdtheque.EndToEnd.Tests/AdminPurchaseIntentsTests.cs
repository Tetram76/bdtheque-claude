extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

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
            .GetByRole(AriaRole.Link, new() { Name = "Intentions d'achat" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Intentions d'achat");
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

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une intention d'achat" }).FillAsync($"Château des étoiles {_marker}");
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une intention d'achat" }).PressAsync("Enter");

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Le Château des étoiles {_marker} - T. 4"]);
    }

    [Theory, InEachTheme]
    public async Task Removal_OfAnIntentOnAnEdition_AnnouncesTheEditionRemovedWithIt_AndTakesTheIntentOutOfTheList(string theme)
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Dupuis {_marker}", null, []));
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(
            new PurchaseIntentEditionContent(publisher.Id, null, 1974, null, null, null, null, null, null, null, true), await AlbumVersionAsync(album.Id)));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPurchaseIntents}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur l'intention d'achat de Buddy Longway {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Retirer l'intention…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Retirer cette intention d'achat ?" });
        await Assertions.Expect(dialog).ToContainTextAsync("Suppression aussi de 1 édition");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPurchaseIntents, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucune intention d'achat ne correspond à « {_marker} ».");
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

        var label = await page.Locator(".intent-label").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        // At the width of the mockups' desktop, the table has all its columns, the membership of the collection included.
        if (width == 1440)
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Collection" })).ToBeVisibleAsync();
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Intentions d'achat" }).Locator(".intent-label");

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId = null, int? volume = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []));

    private async Task CreateAlbumIntentAsync(AlbumForm album) =>
        await PostAsync<PurchaseIntentsForm>($"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, album.Version));

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
