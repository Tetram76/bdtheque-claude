extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the publishers of the administration (visuel/listes/liste-par-liste.md), on the application shared by the
/// tests: each test names its publishers with a marker of its own, and searches it. What the list shares with that of the
/// albums (the modal dialog, Escape) is pinned by AdminAlbumsTests.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminPublishersTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Éditeurs" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPublishers));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Éditeurs");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task Search_OnTheName_NarrowsTheList(string theme)
    {
        await CreatePublisherAsync($"Glénat {_marker}");
        await CreatePublisherAsync($"Delcourt {_marker}");
        var page = await OpenAsync(theme, AppPaths.AdminPublishers);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un éditeur" }).FillAsync(_marker);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un éditeur" }).PressAsync("Enter");

        await Assertions.Expect(Names(page)).ToHaveTextAsync([$"Delcourt {_marker}", $"Glénat {_marker}"]);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPublishers, $"?q={_marker}"));
    }

    [Theory, InEachTheme]
    public async Task Website_OfAPublisher_OpensBesideTheList(string theme)
    {
        await CreatePublisherAsync($"Dargaud {_marker}", "https://www.dargaud.example/");
        var page = await OpenAsync(theme, $"{AppPaths.AdminPublishers}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Dargaud {_marker}" }).ClickAsync();
        var website = page.GetByRole(AriaRole.Link, new() { Name = "Ouvrir le site web" });

        await Assertions.Expect(website).ToHaveAttributeAsync("href", "https://www.dargaud.example/");
        await Assertions.Expect(website).ToHaveAttributeAsync("target", "_blank");
    }

    [Theory, InEachTheme]
    public async Task Deletion_ConfirmedInAModalDialog_TakesThePublisherAndItsCollectionsOutOfTheList(string theme)
    {
        var publisher = await CreatePublisherAsync($"À supprimer {_marker}");
        await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest("Signé", publisher.Version));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPublishers}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À supprimer {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cet éditeur ?" });
        await Assertions.Expect(dialog).ToContainTextAsync("1 collection éditeur");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPublishers, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucun éditeur ne correspond à « {_marker} ».");
    }

    [Theory, InEachTheme]
    public async Task Deletion_OfAPublisherOfAnEdition_IsRefused_AndSaysWhatToDo(string theme)
    {
        var publisher = await CreatePublisherAsync($"Avec une édition {_marker}");
        var album = await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            $"Album {_marker}", null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));
        await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, null, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
            album.Version));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPublishers}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Avec une édition {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Suppression impossible" });
        await Assertions.Expect(dialog).ToContainTextAsync("1 édition");
        await Assertions.Expect(dialog).ToContainTextAsync("Rattachez d'abord ses éditions à un autre éditeur");
        await Assertions.Expect(dialog.GetByRole(AriaRole.Link, new() { Name = "Fermer" })).ToBeVisibleAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" })).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task Merge_SearchesTheOtherPublisher_AndContinuesWithTheOneChosen(string theme)
    {
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var other = await CreatePublisherAsync($"Dupuis bis {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminPublishers}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Dupuis {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Fusionner avec un autre éditeur…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux éditeurs" });

        await dialog.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher l'autre éditeur" }).FillAsync(_marker);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Rechercher" }).ClickAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio)).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergePublishers, $"?other={other.Id}&publisher={publisher.Id}"));
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1024)]
    [InEachTheme(1180)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheNameItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        var publisher = await CreatePublisherAsync(
            $"Les Éditions du Grand Large et des Archipels réunis {_marker}", "https://www.editions-du-grand-large-et-des-archipels.example/catalogue");
        var version = publisher.Version;
        foreach (var collection in new[] { "Carnets de voyage", "Escapades", "Horizons lointains", "Lumières du Nord" })
            version = (await PostAsync<PublisherCollectionForm>(
                $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(collection, version))).PublisherVersion;
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminPublishers}?q={_marker}");
        await PageReadiness.WaitAsync(page);

        var name = await page.Locator(".row-entry").EvaluateAsync<double>("name => name.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(name >= 120, $"{theme} at {width}px: the name is {name}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        // At the width of the mockups' desktop, the table has all its columns, the website and the last modification
        // included.
        if (width == 1440)
        {
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Site web" })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Modifié" })).ToBeVisibleAsync();
        }
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Names(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Éditeurs" }).Locator(".row-entry");

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website = null) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, website, []));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
