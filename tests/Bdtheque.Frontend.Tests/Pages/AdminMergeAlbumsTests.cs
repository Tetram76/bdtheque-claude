using System.Globalization;
using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The merge of two albums (visuel/fusion/fusion-de-deux-fiches.md): a row per data, in the blocks of the entry form of an album,
/// and per genre, universe and contribution held by one album only; what is retained at once and what waits for the user; the
/// label and the sort key the data chosen give; the intent on the whole album the merge withdraws, said apart; the confirmation,
/// which sets apart what the merge will delete; what the merge sends. What every merge presents the same is fixed by MergeTests.
/// </summary>
public sealed class AdminMergeAlbumsTests : BunitContext, IDisposable
{
    private static readonly Guid First = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid Second = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly Guid Aventure = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly Guid Policier = Guid.Parse("01900000-0000-7000-8000-0000000000b2");
    private static readonly Guid Londres = Guid.Parse("01900000-0000-7000-8000-0000000000c1");
    private static readonly Guid IllustratorCredit = Guid.Parse("01900000-0000-7000-8000-0000000000d2");
    private static readonly Guid ColoristCredit = Guid.Parse("01900000-0000-7000-8000-0000000000d4");
    private static readonly AuthorListItem Jacobs = new(Guid.Parse("01900000-0000-7000-8000-0000000000e1"), "Jacobs", "Edgar P.", null);
    private static readonly AuthorListItem DeMoor = new(Guid.Parse("01900000-0000-7000-8000-0000000000e2"), "De Moor", "Bob", null);
    private static readonly SeriesListItem BlakeEtMortimer = new(Guid.Parse("01900000-0000-7000-8000-0000000000f1"), "Blake et Mortimer");

    // « La Marque jaune », in « Blake et Mortimer », tome 6, 1956, an edition and an intent on an edition; « La marque jaune »,
    // its sort key entered by hand, without series, an omnibus of the volumes 1 to 2, March 1987, targeted as a whole: a credit of
    // both, and one of each alone, a genre of both, one of the second alone, a universe of the first alone.
    private static readonly AlbumMergePreview Preview = new(
        new AlbumMergeRecord(
            First, "La Marque jaune", null, BlakeEtMortimer, "Blake et Mortimer", AlbumType.Regular, false, 6, null, null, 1956, null,
            "Le résumé du premier.", null, AlbumRating.VeryGood,
            [new GenreListItem(Aventure, "Aventure")],
            [new UniverseInHierarchy(new UniverseListItem(Londres, "Londres", null, null), [])],
            [
                new MergeContribution(Guid.Parse("01900000-0000-7000-8000-0000000000d1"), Jacobs, ContributionRole.Scenarist),
                new MergeContribution(IllustratorCredit, Jacobs, ContributionRole.Illustrator),
            ],
            new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero), 11),
        new AlbumMergeRecord(
            Second, "La marque jaune", "Marque jaune (manuelle)", null, null, AlbumType.Omnibus, false, 6, 1, 2, 1987, 3,
            null, "À relier.", null,
            [new GenreListItem(Aventure, "Aventure"), new GenreListItem(Policier, "Policier")],
            [],
            [
                new MergeContribution(Guid.Parse("01900000-0000-7000-8000-0000000000d3"), Jacobs, ContributionRole.Scenarist),
                new MergeContribution(ColoristCredit, DeMoor, ContributionRole.Colorist),
            ],
            new DateTimeOffset(2026, 11, 4, 8, 0, 0, TimeSpan.Zero), 12),
        [new MergeLinkCount(EntityKind.Edition, 2, 2, 0, 0), new MergeLinkCount(EntityKind.PurchaseIntent, 1, 1, 0, 0)],
        [MergeSide.Second]);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminMergeAlbumsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers[$"/admin/albums/merge?first={First}&second={Second}"] = Preview;
        _api.Answers["/admin/sort-keys/title"] = new TitleSortKey("Marque jaune (calculée)");
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Rows_EachDataInTheBlocksOfTheEntryForm_AndEachCreditGenreAndUniverseOfEitherAlbum()
    {
        var page = Open();

        Assert.Equal("Fusionner deux albums", page.Find("h1").TextContent.Trim());
        Assert.Equal(["L'album", "Tomaison", "Parution", "Les auteurs", "Genres et univers", "Résumé, notes et appréciation"],
            page.FindAll(".block-title").Where(title => title.Closest(".merge-data") is not null).Select(title => title.TextContent.Trim()));
        Assert.Equal(["Identification", "Dans la série", "Calendrier", "Contributions", "Classement", "Lecture"],
            page.FindAll(".merge-data .block-kicker").Select(kicker => kicker.TextContent.Trim()));
        Assert.Equal(
            [
                "Série", "Titre", "Clé de tri", "Tome", "Type", "Hors-série", "Première publication", "Scénario", "Dessin", "Couleurs",
                "Genre", "Genre", "Univers", "Résumé", "Notes personnelles", "Appréciation",
            ],
            page.FindAll(".merge-choice-label").Select(label => label.TextContent.Trim()));
        // The type and the volumes an omnibus covers, together; the year, or the month and the year, of the first publication.
        Assert.Equal(["Régulier", "Intégrale, tomes 1 à 2"], Values(page, "merge-type"));
        Assert.Equal(["1956", "mars 1987"], Values(page, "merge-publication"));
        Assert.Equal(["Auto", "Marque jaune (manuelle)"], Values(page, "merge-sort-key"));
        // Each album named by its label, its series included, and its editions and intents in the heads of the first block alone.
        Assert.Equal("La Marque jaune (Blake et Mortimer - T. 6) et La marque jaune (INT. - 6 [1 à 2])", page.Find(".entry-form-subject").TextContent.Trim());
        Assert.Contains("2 éditions · 1 intention", page.FindAll(".merge-data")[0].TextContent);
        Assert.Equal(2, page.FindAll(".merge-data-head-detail").Count);
    }

    [Fact]
    public void Choices_ASameValueOnce_AValueOfOneAlbumRetained_TwoValuesWaitingForTheUser()
    {
        var page = Open();

        Assert.Contains($"merge-contribution-{Jacobs.Id}-Scenarist-label", Same(page));
        Assert.Contains($"merge-genre-{Aventure}-label", Same(page));
        Assert.Contains("merge-volume-label", Same(page));
        Assert.Equal("First", Checked(page, "merge-series"));
        Assert.Equal("Second", Checked(page, "merge-sort-key"));
        Assert.Equal("Second", Checked(page, $"merge-genre-{Policier}"));
        Assert.Equal("First", Checked(page, $"merge-universe-{Londres}"));
        // The title, the type, the first publication differ; the summary, the notes and the rating are of one album each.
        Assert.Equal("3 données restent à choisir", page.Find(".save-bar-state-long").TextContent);
        Assert.All(["merge-title", "merge-type", "merge-publication"], id => Assert.Null(Checked(page, id)));
    }

    [Fact]
    public async Task Result_TheLabelAndTheSortKeyTheDataChosenGive()
    {
        var page = Open();
        Assert.DoesNotContain("Affiché", page.Find(".merge-data-result").TextContent);

        await page.ChangeAsync("input[name=merge-title][value=First]", true);
        await page.ChangeAsync("input[name=merge-type][value=First]", true);
        // The sort key entered by hand, of the second album, retained at once.
        Assert.Equal(["Affiché : La Marque jaune (Blake et Mortimer - T. 6)", "Classé à : Marque jaune (manuelle)"], Result(page));

        await page.ChangeAsync("input[name=merge-sort-key][value=First]", true);
        Assert.Contains($"/admin/sort-keys/title?title={Uri.EscapeDataString("La Marque jaune")}", _api.Requested);
        Assert.Equal("Classé à : Marque jaune (calculée)", Result(page)[^1]);

    }

    [Fact]
    public async Task Result_OfAnAlbumWithoutATitleOfItsOwn_IsFiledUnderTheSortKeyOfItsSeries()
    {
        _api.Answers[$"/admin/albums/merge?first={First}&second={Second}"] = Preview with { First = Preview.First with { Title = null } };
        var page = Open();

        // The title of the first album, none: the second one's, set on its side alone, is retained at once; discarding it files
        // the album under its series.
        Assert.Equal("Second", Checked(page, "merge-title"));
        await page.ChangeAsync("input[name=merge-title][value=First]", true);
        await page.ChangeAsync("input[name=merge-type][value=First]", true);

        Assert.Equal(["Affiché : Blake et Mortimer - T. 6", "Classé à : Blake et Mortimer"], Result(page));
    }

    [Fact]
    public void Links_TheEditionsAndIntentsThatPass_AndTheIntentWithdrawnApart()
    {
        var page = Open();

        var links = page.Find(".merge-links");
        Assert.Contains("2 éditions", links.TextContent);
        Assert.Contains("1 intention d'acquisition", links.TextContent);
        Assert.Equal(
            "L'intention d'acquisition de La marque jaune (INT. - 6 [1 à 2]) sur l'album entier sera retirée : l'album aura des éditions.",
            page.Find(".merge-links-withdrawn").TextContent.Trim());
    }

    [Fact]
    public async Task Merge_ConfirmsWhatItWillDeleteApart_SendsTheAlbumOfEachData_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new MergeResult(First));
        var page = Open();
        foreach (var (row, side) in new[] { ("title", "First"), ("type", "First"), ("publication", "First") })
            await page.ChangeAsync($"input[name=merge-{row}][value={side}]", true);
        // The drawing of Jacobs and the genre « Policier » discarded: the side without them is retained.
        await page.ChangeAsync($"input[name=merge-contribution-{Jacobs.Id}-Illustrator][value=Second]", true);
        await page.ChangeAsync($"input[name=merge-genre-{Policier}][value=First]", true);
        Assert.Equal("Tout est choisi", page.Find(".save-bar-state-long").TextContent);

        await page.SubmitAsync("form");
        var confirmation = page.Find("dialog");
        Assert.Contains("Les deux fiches n'en feront plus qu'une : La Marque jaune (Blake et Mortimer - T. 6).", confirmation.TextContent);
        Assert.Contains("La Marque jaune (Blake et Mortimer - T. 6) aura les 2 éditions et l'intention des deux fiches.", confirmation.TextContent);
        Assert.Equal("Sera supprimé :", page.Find(".merge-removals-title").TextContent.Trim());
        Assert.Equal(
            [
                "l'intention d'acquisition de La marque jaune (INT. - 6 [1 à 2]) sur l'album entier : l'album aura des éditions",
                "1 contribution : Edgar P. Jacobs (Dessin), de La Marque jaune (Blake et Mortimer - T. 6)",
                "1 genre : Policier, de La marque jaune (INT. - 6 [1 à 2])",
            ],
            page.FindAll(".merge-removals li").Select(line => line.TextContent.Trim()));
        await page.ClickAsync("dialog button[data-variant=delete]");

        var (_, path, body) = Assert.Single(_api.Written);
        Assert.Equal("/admin/albums/merge", path);
        var request = body.Deserialize<AlbumMergeRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal((First, 11u, Second, 12u), (request.First.Id, request.First.Version, request.Second.Id, request.Second.Version));
        Assert.Equal((MergeSide.First, MergeSide.First, MergeSide.Second, MergeSide.First), (request.Title, request.Series, request.SortKey, request.Type));
        Assert.Equal((MergeSide.First, MergeSide.Second, MergeSide.First), (request.Summary, request.PersonalNotes, request.Rating));
        Assert.Equal([new MergeElement(Policier, MergeSide.First)], request.Genres);
        Assert.Equal([new MergeElement(Londres, MergeSide.First)], request.Universes);
        Assert.Equal([new MergeElement(IllustratorCredit, MergeSide.Second), new MergeElement(ColoristCredit, MergeSide.Second)], request.Contributions);
        Assert.EndsWith(AppPaths.AdminAlbums, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.album-merge.merged", _audience.Functions);
    }

    [Fact]
    public async Task Merge_OfAnAlbumModifiedMeanwhile_SaysSoAndOffersToReload()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        var page = Open();
        foreach (var row in new[] { "title", "type", "publication" })
            await page.ChangeAsync($"input[name=merge-{row}][value=First]", true);

        await page.SubmitAsync("form");
        await page.ClickAsync("dialog button[data-variant=delete]");

        Assert.Contains("L'un des deux albums a été modifié depuis l'ouverture de la fusion.", page.Markup);
        Assert.Contains(page.FindAll("button"), button => button.TextContent.Trim() == "Recharger la fiche");
    }

    private IRenderedComponent<AdminMergeAlbums> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminMergeAlbums}?album={First}&other={Second}");
        return Render<AdminMergeAlbums>(parameters => parameters.AddCascadingValue(_request.Context));
    }

    // The label and the sort key the data chosen give, each a part of the result.
    private static string[] Result(IRenderedComponent<AdminMergeAlbums> page) =>
        [.. page.FindAll(".merge-album-result > span").Select(part => string.Join(' ', part.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))];

    private static string[] Values(IRenderedComponent<AdminMergeAlbums> page, string row) =>
        [.. page.FindAll($"input[name={row}]").Select(radio => radio.ParentElement!.QuerySelector(".merge-choice-value")!.TextContent.Trim())];

    private static string? Checked(IRenderedComponent<AdminMergeAlbums> page, string row) =>
        page.FindAll($"input[name={row}]").SingleOrDefault(radio => radio.HasAttribute("checked"))?.GetAttribute("value");

    // The rows presented once, the same in both albums, by their label.
    private static List<string> Same(IRenderedComponent<AdminMergeAlbums> page) =>
        [.. page.FindAll(".merge-choice-same").Select(same => same.PreviousElementSibling!.Id!)];
}
