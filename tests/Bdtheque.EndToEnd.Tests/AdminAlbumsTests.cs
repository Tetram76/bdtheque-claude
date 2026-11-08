extern alias frontend;

using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the albums of the administration (visuel/liste-albums/contenu.md), on the application shared by the tests:
/// each test names its albums with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminAlbumsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(AtPath(AppPaths.AdminAlbums));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Albums");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(AtPath(AppPaths.AdminHome));
    }

    [Fact]
    public async Task Search_ThenTheNavigationByInitial_NarrowTheList()
    {
        await CreateAlbumAsync($"Astérix {_marker}");
        await CreateAlbumAsync($"Le Lotus bleu {_marker}");
        var page = await OpenAsync(AppPaths.AdminAlbums);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un album" }).FillAsync(_marker);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un album" }).PressAsync("Enter");

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Astérix {_marker}", $"Le Lotus bleu {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "L", Exact = true }).ClickAsync();
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Le Lotus bleu {_marker}"]);
        await Assertions.Expect(page).ToHaveURLAsync(AtPath(AppPaths.AdminAlbums, $"?q={_marker}&entry=L"));
    }

    [Fact]
    public async Task Filter_ChosenByTypingItsName_NarrowsTheList_AndItsTagRemovesIt()
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        await CreateAlbumAsync($"Avec le genre {_marker}", [genre.Id]);
        await CreateAlbumAsync($"Sans le genre {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminAlbums}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" }).PressSequentiallyAsync($"Genre {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Genre {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Avec le genre {_marker}"]);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Retirer « Genre : Genre {_marker} »" }).ClickAsync();
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Avec le genre {_marker}", $"Sans le genre {_marker}"]);
    }

    [Fact]
    public async Task Deletion_ConfirmedInAModalDialog_TakesTheAlbumOutOfTheList()
    {
        await CreateAlbumAsync($"À supprimer {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminAlbums}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À supprimer {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cet album ?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(AtPath(AppPaths.AdminAlbums, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucun album ne correspond à « {_marker} ».");
    }

    [Fact]
    public async Task Deletion_Escape_ClosesTheDialogWithoutDeleting()
    {
        await CreateAlbumAsync($"À garder {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminAlbums}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À garder {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Alertdialog)).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(page).ToHaveURLAsync(AtPath(AppPaths.AdminAlbums, $"?q={_marker}"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"À garder {_marker}"]);
    }

    [Fact]
    public async Task Merge_SearchesTheOtherAlbum_AndContinuesWithTheOneChosen()
    {
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var other = await CreateAlbumAsync($"Barracuda bis {_marker}");
        var page = await OpenAsync($"{AppPaths.AdminAlbums}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Barracuda {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Fusionner avec un autre album…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux albums" });

        await dialog.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher l'autre album" }).FillAsync(_marker);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Rechercher" }).ClickAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio)).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(AtPath(AppPaths.AdminMergeAlbums, $"?other={other.Id}&album={album.Id}"));
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Albums" }).Locator(".album-label");

    private static Regex AtPath(string path, string query = "") => new($@"^[a-z]+://[^/]+{Regex.Escape(path + query)}$");

    private Task<AlbumForm> CreateAlbumAsync(string title, Guid[]? genreIds = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, genreIds ?? [], [], []));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
