extern alias frontend;

using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The merge of two genres, of two authors, of two publishers and of two series (visuel/fusion/fusion-de-deux-fiches.md), on an
/// application of its own: a merge deletes one of its records, which no other test may count on. Each journey of the
/// administrator, through the browser: from the dialog of the list to the merge, the data chosen one by one, the confirmation and
/// the list again; and two records modified meanwhile. Its captures are in <see cref="Captures.AdminMergeCaptureTests"/>.
/// </summary>
[Collection(MergeApplication.Name)]
public sealed partial class AdminMergeTests(MergeApplication application) : BrowserTest<MergeApplication>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task GenreMerge_FromTheDialogOfTheList_KeepsTheLabelChosen_AndLeadsBackToTheList(string theme)
    {
        var first = await CreateGenreAsync($"Science-fiction {_marker}");
        var second = await CreateGenreAsync($"SF {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminGenres}?q={_marker}&merge={first.Id}&mergeQ={Uri.EscapeDataString($"SF {_marker}")}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux genres" });
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio, new() { Name = $"SF {_marker}" })).ToBeCheckedAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeGenres, $"?other={second.Id}&genre={first.Id}"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux genres");
        var merge = page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" });
        // Two labels, neither favoured: nothing is chosen for the user.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("1 donnée reste à choisir");
        await Assertions.Expect(merge).ToBeDisabledAsync();

        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Libellé" }).GetByRole(AriaRole.Radio, new() { Name = $"SF {_marker}" }).CheckAsync();
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("Tout est choisi");
        await merge.ClickAsync();

        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux genres ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync("La fusion est définitive.");
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : SF {_marker}.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres));
        using var api = Application.CreateApiClient();
        Assert.Equal($"SF {_marker}", (await api.GetFromJsonAsync<GenreForm>($"/admin/genres/{second.Id}"))!.Label);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/genres/{first.Id}")).StatusCode);
    }

    [Theory, InEachTheme]
    public async Task AuthorMerge_PresentsWhatTheNamesChosenGive_AndKeepsEachDataChosen(string theme)
    {
        var first = await CreateAuthorAsync(new($"Gotlieb {_marker}", "Marcel", $"Gotlib {_marker}", "Une longue biographie.", "Française"));
        var second = await CreateAuthorAsync(new($"Gotlib {_marker}", "Marcel", null, "Courte.", null));
        var page = await OpenAsync(theme, $"{AppPaths.AdminMergeAuthors}?author={first.Id}&other={second.Id}");
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);

        // The last name and the biography differ on both sides: the user chooses them; the pseudonym and the nationality, set on
        // one side only, are retained at once, the first name, the same, has nothing to choose.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("2 données restent à choisir");
        await Assertions.Expect(page.GetByText("Identique dans les deux fiches")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).GetByRole(AriaRole.Radio, new() { Name = $"Gotlib {_marker}", Exact = true }).CheckAsync();
        // The display name the names chosen give, as the domain derives it: the pseudonym.
        await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync($"Affiché : Gotlib {_marker}");
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Biographie" }).GetByRole(AriaRole.Radio, new() { Name = "Courte." }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();
        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux auteurs ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : Gotlib {_marker}.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAuthors));
        using var api = Application.CreateApiClient();
        // Three data of the first author kept, two of the second: the first one remains.
        var kept = (await api.GetFromJsonAsync<AuthorForm>($"/admin/authors/{first.Id}"))!;
        Assert.Equal(
            ($"Gotlib {_marker}", "Marcel", $"Gotlib {_marker}", "Courte.", "Française"),
            (kept.LastName, kept.FirstName, kept.Pseudonym, kept.Biography, kept.Nationality));
    }

    [Theory, InEachTheme]
    public async Task PublisherMerge_FromTheDialogOfTheList_ChoosesTheNameOfTheHomonymousCollections_AndKeepsEachDataChosen(string theme)
    {
        var first = await CreatePublisherAsync(new($"Dupuis {_marker}", "https://www.example.org/dupuis", [new(null, "Aire Libre"), new(null, "Expresso")]));
        var second = await CreatePublisherAsync(new($"Éditions Dupuis {_marker}", null, [new(null, "aire libre"), new(null, "Expresso"), new(null, "Repérages")]));
        var page = await OpenAsync(theme, $"{AppPaths.AdminPublishers}?q={_marker}&merge={first.Id}&mergeQ={Uri.EscapeDataString($"Éditions Dupuis {_marker}")}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux éditeurs" });
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio, new() { Name = $"Éditions Dupuis {_marker}" })).ToBeCheckedAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergePublishers, $"?other={second.Id}&publisher={first.Id}"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux éditeurs");
        // The names differ, and so do those of a pair of homonymous collections; the website, set on one side only, is retained
        // at once, and the other pair of collections, spelled the same, has nothing to choose.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("2 données restent à choisir");
        var homonyms = page.Locator(".merge-data", new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "Collections homonymes" }) });
        await Assertions.Expect(homonyms.GetByText("Identique dans les deux fiches")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".merge-links")).ToContainTextAsync("3 collections");

        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true })
            .GetByRole(AriaRole.Radio, new() { Name = $"Dupuis {_marker}", Exact = true }).CheckAsync();
        // Its case alone tells it from its homonym.
        await homonyms.GetByRole(AriaRole.Radiogroup, new() { Name = "Aire Libre" })
            .GetByRole(AriaRole.Radio, new() { NameRegex = LowerCaseAireLibre() }).CheckAsync();
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("Tout est choisi");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();

        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux éditeurs ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : Dupuis {_marker}.");
        await Assertions.Expect(confirmation).ToContainTextAsync($"Dupuis {_marker} réunira les 3 collections des deux fiches.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminPublishers));
        using var api = Application.CreateApiClient();
        // The name and the website of the first publisher: it remains, with the collections of both, the homonyms once.
        var kept = (await api.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{first.Id}"))!;
        Assert.Equal(($"Dupuis {_marker}", "https://www.example.org/dupuis"), (kept.Name, kept.Website));
        Assert.Equal(["aire libre", "Expresso", "Repérages"], kept.Collections.Select(c => c.Name));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/publishers/{second.Id}")).StatusCode);
    }

    [Theory, InEachTheme]
    public async Task SeriesMerge_FromTheDialogOfTheList_KeepsTheTitleChosen_AndTheAlbumsOfBoth(string theme)
    {
        var first = await CreateSeriesAsync($"Spirou {_marker}");
        var second = await CreateSeriesAsync($"Spirou et Fantasio {_marker}");
        var page = await OpenAsync(theme,
            $"{AppPaths.AdminSeries}?q={_marker}&merge={first.Id}&mergeQ={Uri.EscapeDataString($"Spirou et Fantasio {_marker}")}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux séries" });
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio, new() { Name = $"Spirou et Fantasio {_marker}" })).ToBeCheckedAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeSeries, $"?other={second.Id}&series={first.Id}"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux séries");
        // Two titles, neither favoured; every other data the same in both series.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("1 donnée reste à choisir");
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Titre" })
            .GetByRole(AriaRole.Radio, new() { Name = $"Spirou et Fantasio {_marker}", Exact = true }).CheckAsync();
        // The sort key the title chosen gives, as the domain computes it.
        await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync($"Classé à : Spirou et Fantasio {_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();

        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux séries ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : Spirou et Fantasio {_marker}.");
        await Assertions.Expect(confirmation).ToContainTextAsync($"Spirou et Fantasio {_marker} regroupera les 2 albums des deux fiches.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries));
        using var api = Application.CreateApiClient();
        // As many data of each series kept, the first one remains.
        var kept = (await api.GetFromJsonAsync<SeriesDetail>($"/catalog/series/{first.Id}"))!;
        Assert.Equal(($"Spirou et Fantasio {_marker}", 2), (kept.Title, kept.Albums.Count));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/series/{second.Id}")).StatusCode);
    }

    [Theory, InEachTheme]
    public async Task Merge_OfAGenreModifiedMeanwhile_MergesNothing_AndOffersToReload(string theme)
    {
        var first = await CreateGenreAsync($"Western {_marker}");
        var second = await CreateGenreAsync($"Westerns {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminMergeGenres}?genre={first.Id}&other={second.Id}");
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        // Renamed from another tab once the merge is read.
        using (var api = Application.CreateApiClient())
            (await api.PutAsJsonAsync($"/admin/genres/{second.Id}", new UpdateGenreRequest($"Western spaghetti {_marker}", second.Version)))
                .EnsureSuccessStatusCode();

        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Libellé" }).GetByRole(AriaRole.Radio, new() { Name = $"Western {_marker}", Exact = true }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();
        await page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        var error = page.GetByRole(AriaRole.Alert);
        await Assertions.Expect(error).ToHaveAttributeAsync("data-error-category", "functional");
        await Assertions.Expect(error).ToContainTextAsync("L'un des deux genres a été modifié depuis l'ouverture de la fusion.");
        await error.GetByRole(AriaRole.Button, new() { Name = "Recharger la fiche" }).ClickAsync();
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Radio, new() { Name = $"Western spaghetti {_marker}" })).ToBeAttachedAsync();
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private async Task<GenreForm> CreateGenreAsync(string label)
    {
        using var api = Application.CreateApiClient();
        var response = await api.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenreForm>())!;
    }

    // Case sensitive: its case alone tells the collection from its homonym.
    [GeneratedRegex("aire libre$")]
    private static partial Regex LowerCaseAireLibre();

    private async Task<PublisherForm> CreatePublisherAsync(PublisherContent content)
    {
        using var api = Application.CreateApiClient();
        var response = await api.PostAsJsonAsync("/admin/publishers", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
    }

    // A series of one album.
    private async Task<SeriesForm> CreateSeriesAsync(string title)
    {
        using var api = Application.CreateApiClient();
        var response = await api.PostAsJsonAsync("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []));
        response.EnsureSuccessStatusCode();
        var series = (await response.Content.ReadFromJsonAsync<SeriesForm>())!;
        (await api.PostAsJsonAsync("/admin/albums", new AlbumContent(
            null, null, series.Id, AlbumType.Regular, false, 1, null, null, null, null, null, null, null, [], [], []))).EnsureSuccessStatusCode();
        return series;
    }

    private async Task<AuthorForm> CreateAuthorAsync(CreateAuthorRequest request)
    {
        using var api = Application.CreateApiClient();
        var response = await api.PostAsJsonAsync("/admin/authors", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorForm>())!;
    }
}

/// <summary>
/// An application whose records the merges delete, which no other test may count on, with two authors saved twice, two genres,
/// two publishers, two series and two universes saved twice,
/// at fixed dates, for the captures of the merge (<see cref="Captures.AdminMergeCaptureTests"/>), which write nothing.
/// </summary>
public sealed class MergeApplication : ApplicationHost
{
    public const string Name = "Merge";

    private static readonly DateTimeOffset Start = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    /// <summary>The two authors of the captures: one known by his pseudonym, the other saved later under his name.</summary>
    public AuthorForm Gotlib { get; private set; } = null!;

    public AuthorForm MarcelGotlib { get; private set; } = null!;

    /// <summary>The two series of the captures: one with its sort key entered by hand, the other saved again later.</summary>
    public SeriesForm Asterix { get; private set; } = null!;

    public SeriesForm AsterixAgain { get; private set; } = null!;

    /// <summary>The two universes of the captures: under two parents, with homonymous sub-universes at two levels.</summary>
    public UniverseForm StarWars { get; private set; } = null!;

    public UniverseForm StarWarsTwin { get; private set; } = null!;

    protected override async Task SeedAsync()
    {
        using var api = CreateApiClient();
        Clock.UtcNow = Start;
        Gotlib = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Gotlieb", "Marcel", "Gotlib",
            "Né à Paris en 1934, il entre à Pilote en 1965, où naissent Gai-Luron et la Rubrique-à-brac, puis cofonde L'Écho des savanes en 1972 et Fluide glacial en 1975.",
            "Française"));
        MarcelGotlib = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Gotlib", "Marcel", null,
            "Auteur de la Rubrique-à-brac et de Gai-Luron.", null));
        var series = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Rubrique-à-brac") with
        {
            Contributions = [new(Gotlib.Id, ContributionRole.Scenarist)],
        });
        await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Gai-Luron") with
        {
            Contributions = [new(Gotlib.Id, ContributionRole.Illustrator), new(MarcelGotlib.Id, ContributionRole.Illustrator)],
        });
        foreach (var volume in Enumerable.Range(1, 3))
            await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(series.Id, volume) with
            {
                Contributions = volume == 3
                    ? [new(Gotlib.Id, ContributionRole.Scenarist), new(MarcelGotlib.Id, ContributionRole.Illustrator)]
                    : [new(Gotlib.Id, ContributionRole.Scenarist)],
            });
        await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(series.Id, 4) with
        {
            Contributions = [new(MarcelGotlib.Id, ContributionRole.Scenarist)],
        });
        // The genres keep the other captures' data apart: none of them is merged by them.
        var humour = await PostAsync<CreateGenreRequest, GenreForm>(api, "/admin/genres", new("Humour"));

        // Two publishers, the second saved later under a longer name, each collection of the first homonymous to one of the
        // second, spelled otherwise or the same; a series of each, and editions of their albums.
        Dupuis = await PostAsync<PublisherContent, PublisherForm>(api, "/admin/publishers", new("Dupuis", "https://www.example.org/dupuis",
            [new(null, "Aire Libre"), new(null, "Expresso")]));
        EditionsDupuis = await PostAsync<PublisherContent, PublisherForm>(api, "/admin/publishers", new("Éditions Dupuis", null,
            [new(null, "aire libre"), new(null, "Expresso"), new(null, "Repérages")]));
        foreach (var (publisher, title, volumes) in new[] { (Dupuis, "Le Pouvoir des innocents", 3), (EditionsDupuis, "Spirou et Fantasio", 2) })
        {
            var collection = publisher.Collections[0].Id;
            var publishedSeries = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series(title) with
            {
                EditionTemplate = new SeriesEditionTemplate(publisher.Id, collection, null, null, null, null, null, null, null),
            });
            foreach (var volume in Enumerable.Range(1, volumes))
            {
                var album = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(publishedSeries.Id, volume));
                await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{album.Id}/editions", new(Owned(publisher.Id, collection), album.Version));
            }
        }

        // The two series of the captures: « Astérix », its sort key entered by hand, with its publisher and collection; « Asterix »,
        // saved again later, a credit of both, one of each alone, a genre of both and one of the first alone.
        var goscinny = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new(null, null, "Goscinny", null, null));
        var uderzo = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Uderzo", "Albert", null, null, null));
        var mebarki = await PostAsync<CreateAuthorRequest, AuthorForm>(api, "/admin/authors", new("Mébarki", "Thierry", null, null, null));
        var aventure = await PostAsync<CreateGenreRequest, GenreForm>(api, "/admin/genres", new("Aventure"));
        var dargaud = await PostAsync<PublisherContent, PublisherForm>(api, "/admin/publishers", new("Dargaud", null, [new(null, "Pilote")]));
        Asterix = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Astérix") with
        {
            ManualSortKey = "Asterix", Status = SeriesStatus.Completed, TheoreticalVolumeCount = 38, IsComplete = true,
            Summary = "Les aventures d'un petit village gaulois qui résiste encore et toujours à l'envahisseur.",
            EditionTemplate = new SeriesEditionTemplate(dargaud.Id, dargaud.Collections[0].Id, null, EditionCondition.Excellent, BindingType.Hardcover,
                BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Standard, true),
            GenreIds = [aventure.Id, humour.Id],
            Contributions = [new(goscinny.Id, ContributionRole.Scenarist), new(uderzo.Id, ContributionRole.Illustrator)],
        });
        AsterixAgain = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Asterix") with
        {
            Status = SeriesStatus.InProgress, PersonalNotes = "Saisie de nouveau, à fusionner.",
            EditionTemplate = new SeriesEditionTemplate(null, null, null, EditionCondition.Excellent, BindingType.Hardcover,
                BookOrientation.Portrait, ReadingDirection.LeftToRight, EditionFormat.Large, true),
            GenreIds = [humour.Id],
            Contributions = [new(goscinny.Id, ContributionRole.Scenarist), new(mebarki.Id, ContributionRole.Colorist)],
        });
        foreach (var volume in Enumerable.Range(1, 2))
            await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(Asterix.Id, volume));
        await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(AsterixAgain.Id, 3));

        var lucasfilm = await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Lucasfilm", null, null));
        StarWars = await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Star Wars",
            "La saga galactique créée par George Lucas en 1977 : ses films, ses séries animées, ses romans et ses bandes dessinées.", null));
        StarWarsTwin = await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Star wars", null, lucasfilm.Id));
        var cloneWars = await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Clone Wars",
            "La guerre des clones, entre les épisodes II et III.", StarWars.Id));
        var cloneWarsTwin = await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Clone wars", null, StarWarsTwin.Id));
        await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Les Jedi", null, cloneWars.Id));
        await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Les Jédi", null, cloneWarsTwin.Id));
        await PostAsync<CreateUniverseRequest, UniverseForm>(api, "/admin/universes", new("Rebels", null, StarWarsTwin.Id));
    }

    /// <summary>The two publishers of the captures: the first one, and the same saved again under a longer name.</summary>
    public PublisherForm Dupuis { get; private set; } = null!;

    public PublisherForm EditionsDupuis { get; private set; } = null!;

    private static EditionContent Owned(Guid publisher, Guid collection) =>
        new(publisher, collection, 2020, null, null, null, null, null, null, null, false, true, null, AcquisitionMode.Purchase, false,
            null, null, null, false, null, null, null, null);

    // Each write a quarter of an hour after the one before, at known dates.
    private async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient api, string path, TRequest request)
    {
        Clock.UtcNow += TimeSpan.FromMinutes(15);
        var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private static SeriesContent Series(string title) =>
        new(title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []);

    private static AlbumContent Album(Guid series, int volume) =>
        new(null, null, series, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []);
}

[CollectionDefinition(MergeApplication.Name)]
public sealed class MergeCollection : ICollectionFixture<MergeApplication>;
