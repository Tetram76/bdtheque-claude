extern alias frontend;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using frontend::Bdtheque.Frontend.Navigation;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The merge of two genres, of two authors, of two publishers, of two series, of two albums and of two editions (visuel/fusion/fusion-de-deux-fiches.md), on an
/// application of its own: a merge deletes one of its records, which no other test may count on. Each journey of the
/// administrator, through the browser: from the dialog of the list to the merge, the data chosen one by one, the confirmation and
/// the list again; and two records modified meanwhile. Its captures are in <see cref="Captures.AdminMergeCaptureTests"/>.
/// </summary>
[Collection(MergeApplication.Name)]
public sealed partial class AdminMergeTests(MergeApplication application) : BrowserTest<MergeApplication>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];
    private PublisherForm? _publisher;

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
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : «\u00A0SF {_marker}\u00A0».");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminGenres));
        using var api = Application.CreateApiClient();
        Assert.Equal($"SF {_marker}", (await api.GetFromJsonAsync<GenreForm>($"/admin/genres/{second.Id}"))!.Label);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/genres/{first.Id}")).StatusCode);
    }

    // fonctionnel/regles-metier/fusion-des-fiches.md, fiches pressenties: from its opening, the dialog proposes the genres that
    // resemble the genre, the first chosen; « Continuer » leads to the merge with it, the address saying it was proposed.
    [Theory, InEachTheme]
    public async Task GenreMerge_TheDialogProposesTheGenresThatResembleIt_AndContinuesWithTheFirst(string theme)
    {
        var first = await CreateGenreAsync($"Science-fiction {_marker}");
        var alike = await CreateGenreAsync($"SCIENCE FICTION {_marker}");
        await CreateGenreAsync($"Western {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminGenres}?q={_marker}&merge={first.Id}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux genres" });
        var proposed = dialog.GetByRole(AriaRole.Radiogroup, new() { Name = "Genres pressentis" });
        await Assertions.Expect(proposed.GetByRole(AriaRole.Radio)).ToHaveCountAsync(1);
        await Assertions.Expect(proposed.GetByRole(AriaRole.Radio, new() { Name = $"SCIENCE FICTION {_marker}" })).ToBeCheckedAsync();
        await Assertions.Expect(dialog).ToContainTextAsync("Ou recherchez l'autre genre par son libellé.");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeGenres, $"?other={alike.Id}&genre={first.Id}&suggested=true"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux genres");
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
        // Left out of the reduced display, the default; presented by « Tout afficher ».
        await Assertions.Expect(page.GetByText("Identique dans les deux fiches")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Tout afficher" }).CheckAsync();
        await Assertions.Expect(page.GetByText("Identique dans les deux fiches")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Nom", Exact = true }).GetByRole(AriaRole.Radio, new() { Name = $"Gotlib {_marker}", Exact = true }).CheckAsync();
        // The display name the names chosen give, as the domain derives it: the pseudonym.
        await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync($"Affiché : Gotlib {_marker}");
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Biographie" }).GetByRole(AriaRole.Radio, new() { Name = "Courte." }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();
        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux auteurs ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : «\u00A0Gotlib {_marker}\u00A0».");
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
        // Left out of the reduced display, the default; presented by « Tout afficher ».
        await Assertions.Expect(homonyms.GetByText("Identique dans les deux fiches")).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Tout afficher" }).CheckAsync();
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
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : «\u00A0Dupuis {_marker}\u00A0».");
        await Assertions.Expect(confirmation).ToContainTextAsync($"«\u00A0Dupuis {_marker}\u00A0» réunira les 3 collections des deux fiches.");
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
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : «\u00A0Spirou et Fantasio {_marker}\u00A0».");
        await Assertions.Expect(confirmation).ToContainTextAsync($"«\u00A0Spirou et Fantasio {_marker}\u00A0» regroupera les 2 albums des deux fiches.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminSeries));
        using var api = Application.CreateApiClient();
        // As many data of each series kept, the first one remains.
        var kept = (await api.GetFromJsonAsync<SeriesDetail>($"/catalog/series/{first.Id}"))!;
        Assert.Equal(($"Spirou et Fantasio {_marker}", 2), (kept.Title, kept.Albums.Count));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/series/{second.Id}")).StatusCode);
    }

    [Theory, InEachTheme]
    public async Task AlbumMerge_FromTheDialogOfTheList_SaysTheIntentWithdrawn_AndKeepsTheEditionOfTheOther(string theme)
    {
        using var api = Application.CreateApiClient();
        var first = await CreateAlbumAsync(api, $"Le Lotus bleu {_marker}");
        var publisher = Application.Dupuis;
        (await api.PostAsJsonAsync($"/admin/albums/{first.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, 1936, null, null, null, null, null, null, null, false, true, null, AcquisitionMode.Purchase, false,
                null, null, null, false, null, null, null, null), first.Version))).EnsureSuccessStatusCode();
        var second = await CreateAlbumAsync(api, $"Le lotus bleu {_marker}");
        (await api.PostAsJsonAsync($"/admin/albums/{second.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, second.Version))).EnsureSuccessStatusCode();
        var page = await OpenAsync(theme,
            $"{AppPaths.AdminAlbums}?q={_marker}&merge={first.Id}&mergeQ={Uri.EscapeDataString($"Le lotus bleu {_marker}")}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux albums" });
        await dialog.GetByRole(AriaRole.Radio, new() { Name = $"Le lotus bleu {_marker}" }).CheckAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeAlbums, $"?other={second.Id}&album={first.Id}"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux albums");
        // The intent on the whole second album does not pass: the album will have the edition of the first one.
        await Assertions.Expect(page.Locator(".merge-links-withdrawn")).ToHaveTextAsync(
            $"L'intention d'acquisition de l'album «\u00A0Le lotus bleu {_marker}\u00A0» sera retirée : l'album aura des éditions.");
        // Two titles, neither favoured; every other data the same in both albums.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("1 donnée reste à choisir");
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Titre" })
            .GetByRole(AriaRole.Radio, new() { Name = $"Le lotus bleu {_marker}", Exact = true }).CheckAsync();
        // The label and the sort key the title chosen gives, as the domain computes it.
        await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync($"Affiché : Le lotus bleu {_marker}");
        await Assertions.Expect(page.Locator(".merge-data-result")).ToContainTextAsync($"Classé à : lotus bleu {_marker} [Le]");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();

        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux albums ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync($"Les deux fiches n'en feront plus qu'une : «\u00A0Le lotus bleu {_marker}\u00A0».");
        await Assertions.Expect(confirmation.Locator(".merge-removals")).ToContainTextAsync("Sera supprimé :");
        await Assertions.Expect(confirmation.Locator(".merge-removals li")).ToHaveTextAsync(
            [$"l'intention d'acquisition de l'album «\u00A0Le lotus bleu {_marker}\u00A0» : l'album aura des éditions"]);
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbums));
        // As many data of each album kept, the first one remains, with its edition and without the intent.
        var kept = (await api.GetFromJsonAsync<AlbumDetail>($"/catalog/albums/{first.Id}"))!;
        Assert.Equal(($"Le lotus bleu {_marker}", 1, null), (kept.Album.Title, kept.Editions.Count, kept.PurchaseIntent));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/albums/{second.Id}")).StatusCode);
    }

    [Theory, InEachTheme]
    public async Task EditionMerge_FromTheDialogOfTheList_KeepsEachDataChosen_AndLeadsBackToTheList(string theme)
    {
        using var api = Application.CreateApiClient();
        var album = await CreateAlbumAsync(api, $"Spirou à New York {_marker}");
        var digits = string.Concat(_marker.Select(c => (char)('0' + c % 10)));
        var (firstIsbn, secondIsbn) = ($"9782{digits}1", $"9782{digits}2");
        var first = await CreateEditionAsync(api, album.Id, firstIsbn, AcquisitionMode.Purchase);
        var second = await CreateEditionAsync(api, album.Id, secondIsbn, AcquisitionMode.Gift);
        // The other edition searched by the title of its album, as an edition without ISBN is.
        var page = await OpenAsync(theme,
            $"{AppPaths.AdminEditions}?q={firstIsbn}&merge={first.Id}&mergeQ={Uri.EscapeDataString($"spirou a new york {_marker}")}");

        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux éditions" });
        await dialog.GetByRole(AriaRole.Radio).CheckAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeEditions, $"?other={second.Id}&edition={first.Id}"));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync("Fusionner deux éditions");
        // The ISBN and the acquisition differ, neither favoured; every other data the same in both editions.
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("2 données restent à choisir");
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "ISBN" }).GetByRole(AriaRole.Radio).Last.CheckAsync();
        await page.GetByRole(AriaRole.Radiogroup, new() { Name = "Acquisition" }).GetByRole(AriaRole.Radio).First.CheckAsync();
        await Assertions.Expect(page.Locator(".save-bar-state-long")).ToHaveTextAsync("Tout est choisi");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fusionner…" }).ClickAsync();

        var confirmation = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Fusionner les deux éditions ?" });
        await Assertions.Expect(confirmation).ToContainTextAsync("La fusion est définitive.");
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Fusionner définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminEditions));
        // As many data of each edition kept, the first one remains, with the ISBN of the other and its own acquisition.
        var kept = (await api.GetFromJsonAsync<EditionForm>($"/admin/albums/{album.Id}/editions/{first.Id}"))!;
        Assert.Equal((secondIsbn, AcquisitionMode.Purchase), (kept.Content.Isbn, kept.Content.AcquisitionMode));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await api.GetAsync($"/admin/albums/{album.Id}/editions/{second.Id}")).StatusCode);
    }

    [Theory, InEachTheme]
    public async Task EditionMerge_TheThumbnailOfAVisualOpensTheViewer_WithoutChoosingIt_AndEscapeClosesIt(string theme)
    {
        // The editions of the captures, read only: their covers, retained by the edition that holds each.
        var page = await OpenAsync(theme, $"{AppPaths.AdminMergeEditions}?edition={Application.BelgiansFirst.Id}&other={Application.BelgiansSecond.Id}");
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        var cover = page.GetByRole(AriaRole.Radiogroup, new() { Name = "Visuel", Exact = true }).Nth(1);

        await cover.GetByRole(AriaRole.Button, new() { Name = "Voir en grand : Couverture" }).ClickAsync();

        var viewer = page.Locator("dialog.visual-dialog");
        await Assertions.Expect(viewer).ToBeVisibleAsync();
        await Assertions.Expect(viewer.Locator(".visual-dialog-title")).ToHaveTextAsync("Couverture");
        await Assertions.Expect(viewer.Locator(".visual-dialog-image img")).ToHaveJSPropertyAsync("complete", true);
        await Assertions.Expect(cover.Locator("input[value=Second]")).ToBeCheckedAsync();

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(viewer).ToHaveCountAsync(0);
    }

    [Theory, InEachTheme]
    public async Task Merge_TheBlocksOfItsData_AreSpacedAsTheBlocksOfAnEntryForm(string theme)
    {
        // The editions of the captures, read only: the blocks of the entry form of the first one, then those of their merge, all
        // presented.
        var edition = Application.BelgiansFirst;
        var page = await OpenAsync(theme, AppPaths.AdminEditEdition(edition.AlbumId, edition.Id));
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        var form = await SpacesAsync(page, ".entry-form-blocks > .block");

        page = await OpenAsync(theme, $"{AppPaths.AdminMergeEditions}?edition={edition.Id}&other={Application.BelgiansSecond.Id}");
        await PageReadiness.WaitUntilEntryFormInteractiveAsync(page);
        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Tout afficher" }).CheckAsync();
        await Assertions.Expect(page.Locator(".merge-view[data-reduced]")).ToHaveCountAsync(0);
        var merge = await SpacesAsync(page, ".merge-group");

        // Each block below the previous one by the space between two blocks of a form, never stuck to it.
        Assert.True(form[0] > 0);
        Assert.Equal(3, merge.Length);
        Assert.All(merge, space => Assert.Equal(form[0], space, 0.5));
    }

    // The space between each block and the next one, of those the selector designates that are presented.
    private static Task<double[]> SpacesAsync(IPage page, string blocks) => page.EvaluateAsync<double[]>("""
        selector => {
            const rects = [...document.querySelectorAll(selector)].map(b => b.getBoundingClientRect()).filter(r => r.height > 0);
            return rects.slice(1).map((r, i) => r.top - rects[i].bottom);
        }
        """, blocks);

    // An edition of the album, owned, of a publisher of its own: the publishers of the captures count their editions.
    private async Task<EditionForm> CreateEditionAsync(HttpClient api, Guid albumId, string isbn, AcquisitionMode mode)
    {
        _publisher ??= (await (await api.PostAsJsonAsync("/admin/publishers", new PublisherContent($"Dupuis {_marker}", null, []))).Content
            .ReadFromJsonAsync<PublisherForm>())!;
        var album = (await api.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!;
        var response = await api.PostAsJsonAsync($"/admin/albums/{albumId}/editions", new CreateEditionRequest(
            new EditionContent(_publisher.Id, null, 1987, isbn, null, null, null, null, null, null, false, true, null, mode, false,
                null, null, null, false, null, null, null, null), album.Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionForm>())!;
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
    private static async Task<AlbumForm> CreateAlbumAsync(HttpClient api, string title)
    {
        var response = await api.PostAsJsonAsync("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlbumForm>())!;
    }

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

        // The two albums of the captures: « Le Tour de Gaule d'Astérix », in « Les Aventures d'Astérix », apart from the series
        // of the merge of two series, whose captures count the albums, tome 5, an owned edition; « Le Tour de Gaule », saved again
        // later without series, wished for as a whole, the same scenarist and a colourist of its own, a genre of its own, published
        // in another month.
        var adventures = await PostAsync<SeriesContent, SeriesForm>(api, "/admin/series", Series("Les Aventures d'Astérix"));
        var tour = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(adventures.Id, 5) with
        {
            Title = "Le Tour de Gaule d'Astérix", FirstPublicationYear = 1965, FirstPublicationMonth = 3, Rating = AlbumRating.VeryGood,
            Summary = "Astérix et Obélix font le tour de la Gaule pour en rapporter les spécialités.",
            GenreIds = [aventure.Id], Contributions = [new(goscinny.Id, ContributionRole.Scenarist), new(uderzo.Id, ContributionRole.Illustrator)],
        });
        await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{tour.Id}/editions", new(Owned(dargaud.Id, dargaud.Collections[0].Id), tour.Version));
        Tour = await GetAsync<AlbumForm>(api, $"/admin/albums/{tour.Id}");
        var tourAgain = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(null, 5) with
        {
            Title = "Le Tour de Gaule", FirstPublicationYear = 1965, FirstPublicationMonth = 5,
            PersonalNotes = "Saisi de nouveau, à fusionner.", GenreIds = [aventure.Id, humour.Id],
            Contributions = [new(goscinny.Id, ContributionRole.Scenarist), new(mebarki.Id, ContributionRole.Colorist)],
        });
        await PostAsync<CreatePurchaseIntentRequest, PurchaseIntentsForm>(api, $"/admin/albums/{tourAgain.Id}/purchase-intents", new(null, tourAgain.Version));
        TourAgain = await GetAsync<AlbumForm>(api, $"/admin/albums/{tourAgain.Id}");

        // The two editions of the captures, of « Astérix chez les Belges », apart from the other albums: the first one in the
        // collection « Pilote », bought, its cover; the second one saved again without collection, offered, free, in good
        // condition, its own cover and a plate.
        var belgians = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums", Album(null, null) with { Title = "Astérix chez les Belges" });
        BelgiansFirst = await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{belgians.Id}/editions", new(Owned(dargaud.Id, dargaud.Collections[0].Id) with
        {
            PublicationYear = 1979, Isbn = "9782012101333", AcquisitionDate = new DateOnly(2004, 3, 12), AcquisitionAmount = 7.5m, AcquisitionCurrency = "EUR",
        }, belgians.Version));
        var version = await UploadAsync(api, belgians.Id, BelgiansFirst.Id, VisualType.Cover, new SKColor(0x2F, 0x5B, 0xA8), BelgiansFirst.AlbumVersion);
        BelgiansSecond = await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{belgians.Id}/editions", new(Owned(dargaud.Id, dargaud.Collections[0].Id) with
        {
            PublisherCollectionId = null, PublicationYear = 1979, Isbn = "9782012101333", AcquisitionMode = AcquisitionMode.Gift, IsFree = true, Condition = EditionCondition.Good,
            PersonalReference = "B-12",
        }, version));
        version = await UploadAsync(api, belgians.Id, BelgiansSecond.Id, VisualType.Cover, new SKColor(0xD8, 0x34, 0x2B), BelgiansSecond.AlbumVersion);
        await UploadAsync(api, belgians.Id, BelgiansSecond.Id, VisualType.Plate, new SKColor(0x3C, 0x9A, 0x5F), version);
    }

    /// <summary>The two editions of the captures, of the same album, the second saved again later.</summary>
    public EditionForm BelgiansFirst { get; private set; } = null!;

    public EditionForm BelgiansSecond { get; private set; } = null!;

    // A visual of an edition, a plain image of a colour, uploaded as the entry of a visual is.
    private async Task<uint> UploadAsync(HttpClient api, Guid album, Guid edition, VisualType type, SKColor color, uint albumVersion)
    {
        Clock.UtcNow += TimeSpan.FromMinutes(15);
        using var bitmap = new SKBitmap(300, 400);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var file = new ByteArrayContent(png.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent
        {
            { file, UploadVisualFields.File, "visual.png" },
            { new StringContent(type.ToString()), UploadVisualFields.Type },
            { new StringContent(albumVersion.ToString(CultureInfo.InvariantCulture)), UploadVisualFields.AlbumVersion },
        };
        using var response = await api.PostAsync($"/admin/albums/{album}/editions/{edition}/visuals", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.AlbumVersion;
    }

    /// <summary>The two albums of the captures: one in its series, with an edition, the other saved again later, wished for as a whole.</summary>
    public AlbumForm Tour { get; private set; } = null!;

    public AlbumForm TourAgain { get; private set; } = null!;

    private static async Task<TResponse> GetAsync<TResponse>(HttpClient api, string path) => (await api.GetFromJsonAsync<TResponse>(path))!;

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

    private static AlbumContent Album(Guid? series, int? volume) =>
        new(null, null, series, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []);
}

[CollectionDefinition(MergeApplication.Name)]
public sealed class MergeCollection : ICollectionFixture<MergeApplication>;
