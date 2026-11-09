extern alias frontend;

using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The entry form of an album (visuel/saisie-album/contenu.md), on the application shared by the tests: each test names its
/// records with a marker of its own. Each journey of the administrator, through the browser: creating an album and its
/// authors, modifying it, and the errors of its saving.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminAlbumFormTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task List_LeadsToTheFormOfAnAlbum_AndToANewOne()
    {
        var album = await CreateAlbumAsync(Content($"Le Lotus bleu {_marker}"));
        var page = await OpenAsync($"{AppPaths.AdminAlbums}?q={_marker}");

        await page.GetByRole(AriaRole.Link, new() { Name = $"Modifier Le Lotus bleu {_marker}" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditAlbum(album.Id)));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier l'album");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Nouvel album" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewAlbum));
    }

    [Fact]
    public async Task Create_WithItsSeriesAndAnAuthorFoundByTypingItsName_LeadsToItsForm()
    {
        var series = await PostAsync<SeriesForm>("/admin/series", Series($"Série {_marker}", []));
        var author = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur{_marker}", "Jean", null, null, null));
        var page = await OpenAsync(AppPaths.AdminNewAlbum);
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByRole(AriaRole.Combobox, new() { Name = "Série" }).PressSequentiallyAsync($"Série {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Série {_marker}" }).ClickAsync();
        await page.GetByLabel("Titre").FillAsync($"Le Lotus bleu {_marker}");
        await Assertions.Expect(page.GetByLabel("Clé de tri")).ToHaveValueAsync($"Lotus bleu {_marker} [Le]");
        await page.GetByLabel("Tome", new() { Exact = true }).FillAsync("5");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ajouter un auteur au scénario" }).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Ajouter un auteur au scénario" }).PressSequentiallyAsync($"Auteur{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Jean Auteur{_marker}" }).ClickAsync();
        await Assertions.Expect(page.Locator(".entry-form-subject")).ToHaveTextAsync($"Le Lotus bleu {_marker} (Série {_marker} - T. 5)");
        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'album" }).ClickAsync();

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Modifier l'album");
        await Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Aucune modification" })).ToBeVisibleAsync();
        var id = Guid.Parse(new Uri(page.Url).Segments[^1]);
        var saved = await GetAsync(id);
        Assert.Equal(($"Le Lotus bleu {_marker}", series.Id, 5), (saved.Content.Title, saved.Content.SeriesId, saved.Content.VolumeNumber));
        Assert.Equal([new ContributionContent(author.Id, ContributionRole.Scenarist)], saved.Content.Contributions);
    }

    [Fact]
    public async Task Author_CreatedOnTheSpot_IsCreditedOnTheAlbum()
    {
        var album = await CreateAlbumAsync(Content($"Album {_marker}"));
        var page = await OpenAsync(AppPaths.AdminEditAlbum(album.Id));
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Ajouter un auteur au dessin" }).ClickAsync();
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Ajouter un auteur au dessin" }).PressSequentiallyAsync($"Jean Nouveau{_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Créer l'auteur « Jean Nouveau{_marker} »" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Nouvel auteur" });
        await Assertions.Expect(dialog.GetByLabel("Nom", new() { Exact = true })).ToHaveValueAsync($"Nouveau{_marker}");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Créer l'auteur" }).ClickAsync();

        await Assertions.Expect(dialog).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Aucune modification" })).ToBeVisibleAsync();
        var credited = Assert.Single((await GetAsync(album.Id)).Content.Contributions);
        Assert.Equal(ContributionRole.Illustrator, credited.Role);
    }

    [Fact]
    public async Task Save_WithoutTitleNorSeries_IsToCorrect_OnTheTitle()
    {
        var page = await OpenAsync(AppPaths.AdminNewAlbum);
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Créer l'album" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToContainTextAsync("À corriger");
        await Assertions.Expect(page.GetByLabel("Titre")).ToHaveAttributeAsync("aria-invalid", "true");
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminNewAlbum));
    }

    [Fact]
    public async Task Save_OfAnAlbumModifiedMeanwhile_OverwritesNothing_ShowsTheChanges_AndReloads()
    {
        var album = await CreateAlbumAsync(Content($"Album {_marker}"));
        var page = await OpenAsync(AppPaths.AdminEditAlbum(album.Id));
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await UpdateAsync(album, album.Content with { Summary = "Modifié ailleurs" });

        await page.GetByLabel("Titre").FillAsync($"Autre titre {_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToContainTextAsync("Modifiée entre-temps");
        await error.GetByRole(AriaRole.Button, new() { Name = "Voir mes changements" }).ClickAsync();
        var changes = page.GetByRole(AriaRole.Dialog, new() { Name = "Mes changements" });
        await Assertions.Expect(changes).ToContainTextAsync($"Autre titre {_marker}");
        Assert.True(await changes.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await changes.GetByRole(AriaRole.Button, new() { Name = "Fermer" }).ClickAsync();
        Assert.Equal($"Album {_marker}", (await GetAsync(album.Id)).Content.Title);

        await error.GetByRole(AriaRole.Button, new() { Name = "Recharger la fiche" }).ClickAsync();
        await Assertions.Expect(page.GetByLabel("Résumé")).ToHaveValueAsync("Modifié ailleurs");
        await Assertions.Expect(page.GetByLabel("Titre")).ToHaveValueAsync($"Album {_marker}");
    }

    [Fact]
    public async Task Series_WithAuthors_ChosenForAnAlbumWithout_GivesThemAtOnce_AndSaysSo()
    {
        var author = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Auteur{_marker}", null, null, null, null));
        await PostAsync<SeriesForm>("/admin/series", Series($"Série {_marker}", [new ContributionContent(author.Id, ContributionRole.Scenarist)]));
        var album = await CreateAlbumAsync(Content($"Album {_marker}"));
        var page = await OpenAsync(AppPaths.AdminEditAlbum(album.Id));
        await ControlsPage.WaitUntilInteractiveAsync(page);

        await page.GetByRole(AriaRole.Combobox, new() { Name = "Série" }).PressSequentiallyAsync($"Série {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Série {_marker}" }).ClickAsync();

        // As soon as the series is chosen, before any saving.
        await Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Repris de la série" })).ToContainTextAsync($"Série {_marker}");
        await Assertions.Expect(page.Locator(".album-role").First).ToContainTextAsync($"Auteur{_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enregistrer" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Modifications non enregistrées")).ToHaveCountAsync(0);
        Assert.Equal([new ContributionContent(author.Id, ContributionRole.Scenarist)], (await GetAsync(album.Id)).Content.Contributions);
    }

    private async Task<IPage> OpenAsync(string path)
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static AlbumContent Content(string title) =>
        new(title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []);

    private static SeriesContent Series(string title, IReadOnlyList<ContributionContent> contributions) =>
        new(title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], contributions);

    private Task<AlbumForm> CreateAlbumAsync(AlbumContent content) => PostAsync<AlbumForm>("/admin/albums", content);

    private async Task<AlbumForm> GetAsync(Guid id)
    {
        using var api = Application.CreateApiClient();
        return (await api.GetFromJsonAsync<AlbumForm>($"/admin/albums/{id}"))!;
    }

    private async Task UpdateAsync(AlbumForm album, AlbumContent content)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PutAsJsonAsync($"/admin/albums/{album.Id}", new UpdateAlbumRequest(content, album.Version));
        response.EnsureSuccessStatusCode();
    }

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
