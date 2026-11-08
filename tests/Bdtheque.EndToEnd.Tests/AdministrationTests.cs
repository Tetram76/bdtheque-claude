extern alias frontend;

using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The access to the administration (contraintes-techniques/architecture-deploiement/authentification.md): the login, back to
/// the page asked for, the refusal of wrong credentials, and the logout from the menu.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed partial class AdministrationAccessTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    [Fact]
    public async Task Administrator_SignsIn_AndIsLedBackToThePageAskedFor()
    {
        var page = await NewPageAsync();

        await page.GotoAsync(AppPaths.AdminHome);

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminLogin, $"?{AppPaths.ReturnUrlParameter}=%2Fadmin"));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Connexion à l'administration");
        await page.GetByLabel("Identifiant").FillAsync(ApplicationHost.AdminLogin);
        await page.GetByLabel("Mot de passe").FillAsync(ApplicationHost.AdminPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Administration");
    }

    [Fact]
    public async Task WrongPassword_IsRefused_AsAnErrorToCorrect()
    {
        var page = await NewPageAsync();
        await page.GotoAsync(AppPaths.AdminLogin);

        await page.GetByLabel("Identifiant").FillAsync(ApplicationHost.AdminLogin);
        await page.GetByLabel("Mot de passe").FillAsync("not-the-password");
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToHaveAttributeAsync("data-error-category", "business");
        await Assertions.Expect(error).ToContainTextAsync("Identifiant ou mot de passe incorrect");
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminLogin));
        // Still refused at the address of the administration: no cookie was given.
        await page.GotoAsync(AppPaths.AdminHome);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($@"{Regex.Escape(AppPaths.AdminLogin)}\?"));
    }

    [Fact]
    public async Task ReturnPage_OfAnotherSite_IsNeverFollowed()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{AppPaths.AdminLogin}?{AppPaths.ReturnUrlParameter}=%2F%2Fother.example%2F");

        await page.GetByLabel("Identifiant").FillAsync(ApplicationHost.AdminLogin);
        await page.GetByLabel("Mot de passe").FillAsync(ApplicationHost.AdminPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Se connecter" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Fact]
    public async Task Administrator_SignsOutFromTheMenu_AndTheAdministrationAsksForTheLoginAgain()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Se déconnecter" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.Home));
        await page.GotoAsync(AppPaths.AdminHome);
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($@"{Regex.Escape(AppPaths.AdminLogin)}\?"));
    }

    [Fact]
    public async Task LoginPage_OnceSignedIn_LeadsStraightToTheAdministration()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GotoAsync(AppPaths.AdminLogin);

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }
}

/// <summary>
/// The home page of the administration (visuel/administration/contenu.md), on an application of its own: it presents the
/// whole base, which the other tests change. Its captures are in <see cref="Captures.AdminHomeCaptureTests"/>.
/// </summary>
[Collection(AdministrationApplication.Name)]
public sealed class AdminHomeTests(AdministrationApplication application) : BrowserTest<AdministrationApplication>(application)
{
    [Fact]
    public async Task Administrator_SeesTheRecordsTheChecksAndTheLatestChanges()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        var albums = page.Locator(".admin-record", new() { Has = page.GetByRole(AriaRole.Link, new() { Name = "Albums", Exact = true }) });
        await Assertions.Expect(albums.Locator(".admin-record-count")).ToHaveTextAsync("4");
        await Assertions.Expect(albums.Locator(".admin-record-detail")).ToHaveTextAsync("dont 2 hors de la collection");
        var check = page.GetByRole(AriaRole.Link, new() { Name = "ISBN au chiffre de contrôle incohérent" });
        await Assertions.Expect(check.Locator(".admin-check-count")).ToHaveTextAsync("1");
        // The genre deleted is no record any more; the album renamed, under its new title.
        var changes = page.Locator(".admin-change-label");
        await Assertions.Expect(changes.First).ToHaveTextAsync("Le Nid des Marsupilamis (Spirou et Fantasio - T. 12)");
        await Assertions.Expect(changes).Not.ToContainTextAsync(["Polar"]);
    }

    [Fact]
    public async Task Menu_GivesTheNumberOfRecords_AndLeadsToTheLists()
    {
        var page = await NewPageAsync();
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        var albums = page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" }).GetByRole(AriaRole.Link, new() { Name = "Albums" });
        await Assertions.Expect(albums.Locator(".admin-menu-count")).ToHaveTextAsync("4");

        await albums.ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbums));
    }
}

/// <summary>
/// An application whose base holds a record of every type, an ISBN whose check digit disagrees, a record renamed and one
/// deleted, written at fixed dates: the captures show the dates of the latest changes.
/// </summary>
public sealed class AdministrationApplication : ApplicationHost
{
    public const string Name = "Administration";

    protected override bool PaintedByItsSpotlight => true;

    private static readonly DateTimeOffset Start = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    protected override async Task SeedAsync()
    {
        using var api = CreateApiClient();
        ApiClock.UtcNow = Start;
        var publisher = await PostAsync<CreatePublisherRequest, PublisherForm>(api, "/admin/publishers", new("Dupuis", null));
        await PostAsync<CreatePublisherCollectionRequest, PublisherCollectionForm>(api,
            $"/admin/publishers/{publisher.Id}/collections", new("Repérages", publisher.Version));
        var series = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Spirou et Fantasio", SeriesStatus.InProgress, false));
        // The cases of the list of the authors: a pseudonym over a name, credited on a series only; one credited with several
        // roles on an album; one credited on nothing, without nationality.
        var franquin = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Franquin", "André", null, null, "Belge"));
        var gotlib = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Gotlieb", "Marcel", "Gotlib", null, "Française"));
        await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Gomez", "Alain", null, null, null));
        // The cases of the list of the series: a complete one, and one without status nor album.
        await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Barracuda", SeriesStatus.Completed, true) with
        {
            Contributions = [new(gotlib.Id, ContributionRole.Scenarist), new(gotlib.Id, ContributionRole.Illustrator)],
        });
        await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Bone", null, false));
        var album = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album("Le Nid des Marsupilamis", series.Id, 12) with
        {
            Contributions = [new(franquin.Id, ContributionRole.Scenarist), new(franquin.Id, ContributionRole.Illustrator)],
        });
        await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{album.Id}/editions", new(Edition(publisher.Id, "978-2-8001-0012-8"), album.Version));
        var wanted = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album("Le Prisonnier du Bouddha", series.Id, 14));
        await PostAsync<CreatePurchaseIntentRequest, PurchaseIntentsForm>(api, $"/admin/albums/{wanted.Id}/purchase-intents", new(null, wanted.Version));
        await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album("Z comme Zorglub", series.Id, 15));
        await PostAsync<CreateGenreRequest, GenreForm>(api, "/admin/genres", new("Humour"));
        var deleted = await PostAsync<CreateGenreRequest, GenreForm>(api, "/admin/genres", new("Polar"));
        var universe = await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Spirouverse", null, null));
        await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Champignac", null, universe.Id));

        // An edition whose ISBN has a check digit that disagrees, then the latest changes: a deletion and a modification.
        var mistyped = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(null, series.Id, 13));
        await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{mistyped.Id}/editions", new(Edition(publisher.Id, "978-2-8001-0013-1"), mistyped.Version));
        Tick();
        var impact = await api.GetFromJsonAsync<DeletionImpact>($"/admin/genres/{deleted.Id}/deletion-impact");
        (await api.DeleteAsync($"/admin/genres/{deleted.Id}?version={deleted.Version}&fingerprint={Uri.EscapeDataString(impact!.Fingerprint)}")).EnsureSuccessStatusCode();
        Tick();
        var current = await api.GetFromJsonAsync<AlbumForm>($"/admin/albums/{album.Id}");
        (await api.PutAsJsonAsync($"/admin/albums/{album.Id}", new UpdateAlbumRequest(
            current!.Content with { Rating = AlbumRating.VeryGood }, current.Version))).EnsureSuccessStatusCode();
    }

    // Each write a quarter of an hour after the one before: the latest changes in a known order and at known dates.
    private async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient api, string path, TRequest request)
    {
        Tick();
        var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private void Tick() => ApiClock.UtcNow += TimeSpan.FromMinutes(15);

    private static SeriesContent Series(string title, SeriesStatus? status, bool isComplete) =>
        new(title, null, status, null, isComplete, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []);

    private static AlbumContent Album(string? title, Guid series, int volume) =>
        new(title, null, series, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []);

    private static EditionContent Edition(Guid publisher, string isbn) =>
        new(publisher, null, 1960, isbn, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);
}

[CollectionDefinition(AdministrationApplication.Name)]
public sealed class AdministrationCollection : ICollectionFixture<AdministrationApplication>;
