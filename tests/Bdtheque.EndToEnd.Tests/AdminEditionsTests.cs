extern alias frontend;

using System.Globalization;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the editions of the administration (visuel/listes/liste-par-liste.md), on the application shared by the tests:
/// each test names its albums with a marker of its own, and narrows the list to them. What the list shares with that of the
/// albums (the modal dialog, Escape, the merge) is pinned by AdminAlbumsTests.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminEditionsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Éditions" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditions));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Éditions");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Fact]
    public async Task Search_OnTheIsbnTypedWithoutItsHyphens_NarrowsTheList()
    {
        var digits = Random.Shared.NextInt64(1_000_000_000, 10_000_000_000).ToString(CultureInfo.InvariantCulture);
        var publisher = await CreatePublisherAsync($"Glénat {_marker}");
        var album = await CreateAlbumAsync($"Les Lunes de Saturne {_marker}");
        await CreateEditionAsync(album, publisher, $"978-{digits[..1]}-{digits[1..5]}-{digits[5..]}");
        await CreateEditionAsync(album, publisher, null);
        var page = await OpenAsync(AppPaths.AdminEditions);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une édition" }).FillAsync($"978{digits}");
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher une édition" }).PressAsync("Enter");

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Glénat {_marker} - ISBN 978-{digits[..1]}-{digits[1..5]}-{digits[5..]}"]);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditions, $"?q=978{digits}"));
    }

    [Fact]
    public async Task Filter_OnAnAlbumChosenByTypingItsTitle_NarrowsTheList_AndItsTagRemovesIt()
    {
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var kept = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await CreateEditionAsync(kept, publisher, null);
        await CreateEditionAsync(await CreateAlbumAsync($"Bouncer {_marker}"), publisher, null);
        var page = await OpenAsync($"{AppPaths.AdminEditions}?publisherId={publisher.Id}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Album" }).PressSequentiallyAsync($"Buddy Longway {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Buddy Longway {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Albums(page)).ToHaveTextAsync([$"Buddy Longway {_marker}"]);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Retirer « Album : Buddy Longway {_marker} »" }).ClickAsync();
        await Assertions.Expect(Albums(page)).ToHaveTextAsync([$"Bouncer {_marker}", $"Buddy Longway {_marker}"]);
    }

    [Fact]
    public async Task Filter_OnACollection_SetsItsPublisher_ThePublisherChosenOfferingOnlyItsCollections()
    {
        var publisher = await CreatePublisherAsync($"Delcourt {_marker}");
        await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Encrages {_marker}", publisher.Version));
        var other = await CreatePublisherAsync($"Soleil {_marker}");
        await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{other.Id}/collections", new CreatePublisherCollectionRequest($"Encrages bis {_marker}", other.Version));
        var page = await OpenAsync($"{AppPaths.AdminEditions}?filters=open");
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByRole(AriaRole.Combobox, new() { Name = "Collection éditeur" }).PressSequentiallyAsync($"Encrages {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Encrages {_marker}" }).ClickAsync();

        // The publisher of the collection, set by its choice.
        await Assertions.Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Éditeur", Exact = true })).ToHaveValueAsync($"Delcourt {_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Retirer « Encrages {_marker} »" }).ClickAsync();
        // The field emptied by the circuit before typing again, as the user sees it.
        await Assertions.Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Collection éditeur" })).ToHaveValueAsync("");
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Collection éditeur" }).PressSequentiallyAsync("Encrages");
        // Only the collections of the publisher chosen.
        await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToContainTextAsync($"Encrages {_marker}");
    }

    [Fact]
    public async Task Deletion_OfTheLastEditionOfAnAlbum_AnnouncesThatTheAlbumLeavesTheCollection_AndTakesTheEditionOutOfTheList()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        await CreateEditionAsync(await CreateAlbumAsync($"Tintin {_marker}"), publisher, null);
        var page = await OpenAsync($"{AppPaths.AdminEditions}?publisherId={publisher.Id}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Casterman {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cette édition ?" });
        await Assertions.Expect(dialog).ToContainTextAsync("Sortie de la collection de 1 album");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditions, $"?publisherId={publisher.Id}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Aucune édition ne correspond à ces critères.");
    }

    [Theory]
    [InlineData(414)]
    [InlineData(768)]
    [InlineData(1024)]
    [InlineData(1180)]
    [InlineData(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheLabelItsRoom_WithoutTheListOverflowing(int width)
    {
        var publisher = await CreatePublisherAsync($"Les Éditions du Grand Large et des Archipels réunis {_marker}");
        var album = await CreateAlbumAsync($"Les Voyages extraordinaires du capitaine Archibald au-delà des mers {_marker}");
        await CreateEditionAsync(album, publisher, "978-2-7234-4567-1", EditionCategory.LimitedEdition, new DateOnly(2014, 5, 2));
        foreach (var theme in ThemeCatalog.Offered.Select(theme => theme.Key))
        {
            var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
            await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminEditions}?publisherId={publisher.Id}");
            await PageReadiness.WaitAsync(page);

            var label = await page.Locator(".row-entry").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
            var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

            Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
            Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
            // At the width of the mockups' desktop, the table has all its columns, the category and the last modification
            // included.
            if (width == 1440)
            {
                await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Catégorie" })).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Modifiée" })).ToBeVisibleAsync();
            }
        }
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Éditions" }).Locator(".row-entry");

    private static ILocator Albums(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Éditions" }).Locator(".edition-album");

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest(name, null));

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task CreateEditionAsync(
        AlbumForm album, PublisherForm publisher, string? isbn, EditionCategory? category = null, DateOnly? acquisitionDate = null)
    {
        using var api = Application.CreateApiClient();
        var version = (await api.GetFromJsonAsync<AlbumForm>($"/admin/albums/{album.Id}"))!.Version;
        await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, null, isbn, null, null, null, null, null, category, false, true, null,
                AcquisitionMode.Purchase, false, acquisitionDate, null, null, false, null, null, null, null),
            version));
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
