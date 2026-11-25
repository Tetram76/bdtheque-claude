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
/// The merge of two series (visuel/fusion/fusion-de-deux-fiches.md): a row per data, in the order of the entry form of a series,
/// and per genre, universe and contribution held by one series only; what is retained at once and what waits for the user; the
/// sort key the data chosen give; what the merge sends. What every merge presents the same is fixed by MergeTests.
/// </summary>
public sealed class AdminMergeSeriesTests : BunitContext, IDisposable
{
    private static readonly Guid First = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid Second = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly Guid Aventure = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly Guid Humour = Guid.Parse("01900000-0000-7000-8000-0000000000b2");
    private static readonly Guid Gaule = Guid.Parse("01900000-0000-7000-8000-0000000000c1");
    private static readonly Guid UderzoCredit = Guid.Parse("01900000-0000-7000-8000-0000000000d2");
    private static readonly Guid MebarkiCredit = Guid.Parse("01900000-0000-7000-8000-0000000000d4");
    private static readonly AuthorListItem Goscinny = new(Guid.Parse("01900000-0000-7000-8000-0000000000e1"), null, null, "Goscinny");
    private static readonly AuthorListItem Uderzo = new(Guid.Parse("01900000-0000-7000-8000-0000000000e2"), "Uderzo", "Albert", null);
    private static readonly AuthorListItem Mebarki = new(Guid.Parse("01900000-0000-7000-8000-0000000000e3"), "Mébarki", "Thierry", null);
    private static readonly PublisherListItem Dargaud = new(Guid.Parse("01900000-0000-7000-8000-0000000000f1"), "Dargaud", null);
    private static readonly PublisherListItem Hachette = new(Guid.Parse("01900000-0000-7000-8000-0000000000f2"), "Hachette", null);

    // « Astérix », its sort key entered by hand, with Dargaud and its collection; « Asterix », its sort key following its title:
    // a credit of both, and one of each alone, a genre of both, and one of the first alone.
    private static readonly SeriesMergePreview Preview = new(
        new SeriesMergeRecord(
            First, "Astérix", "Asterix", SeriesStatus.Completed, 38, true, false, false, "Le petit Gaulois.", null,
            new SeriesEditionTemplate(Dargaud.Id, null, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait,
                ReadingDirection.LeftToRight, EditionFormat.Standard, true),
            Dargaud, new PublisherCollectionListItem(Guid.Parse("01900000-0000-7000-8000-0000000000f3"), "Pilote", Dargaud.Id, "Dargaud"),
            [new GenreListItem(Aventure, "Aventure"), new GenreListItem(Humour, "Humour")],
            [new UniverseInHierarchy(new UniverseListItem(Gaule, "Gaule", null, null), [])],
            [
                new MergeContribution(Guid.Parse("01900000-0000-7000-8000-0000000000d1"), Goscinny, ContributionRole.Scenarist),
                new MergeContribution(UderzoCredit, Uderzo, ContributionRole.Illustrator),
            ],
            new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero), 11),
        new SeriesMergeRecord(
            Second, "Asterix", null, SeriesStatus.InProgress, null, false, false, false, null, "À relier.",
            new SeriesEditionTemplate(Hachette.Id, null, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait,
                ReadingDirection.LeftToRight, EditionFormat.Large, true),
            Hachette, null,
            [new GenreListItem(Humour, "Humour")],
            [],
            [
                new MergeContribution(Guid.Parse("01900000-0000-7000-8000-0000000000d3"), Goscinny, ContributionRole.Scenarist),
                new MergeContribution(MebarkiCredit, Mebarki, ContributionRole.Colorist),
            ],
            new DateTimeOffset(2026, 11, 4, 8, 0, 0, TimeSpan.Zero), 12),
        [new MergeLinkCount(EntityKind.Album, 40, 38, 2, 0)]);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminMergeSeriesTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _api.Answers[$"/admin/series/merge?first={First}&second={Second}"] = Preview;
        _api.Answers["/admin/sort-keys/title"] = new TitleSortKey("Asterix (calculée)");
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public async Task Rows_EachDataInTheOrderOfTheEntryForm_AndEachCreditGenreAndUniverseOfEitherSeries()
    {
        var page = Open();
        // Every data presented (« Tout afficher »).
        await page.ChangeAsync(".merge-show-all", true);

        Assert.Equal("Fusionner deux séries", page.Find("h1").TextContent.Trim());
        Assert.Equal(
            [
                "Titre", "Clé de tri", "Statut", "Tomes numérotés prévus", "Complète dans la collection", "Exclure des albums manquants",
                "Exclure des estimations de sortie", "Scénario", "Dessin", "Couleurs", "Éditeur et collection", "Genre", "Genre", "Univers",
                "Résumé", "Notes personnelles", "Catégorie", "État", "Reliure", "Format", "Orientation", "Sens de lecture", "Couleur",
            ],
            page.FindAll(".merge-choice-label").Select(label => label.TextContent.Trim()));
        Assert.Equal(["Dargaud · Pilote", "Hachette"], Values(page, "merge-publisher"));
        Assert.Equal(["Asterix", "Auto, calculée à partir du titre"], Values(page, "merge-sort-key"));
        Assert.Equal(["Oui", "Non"], Values(page, "merge-complete"));
    }

    [Fact]
    public void Blocks_ThoseOfTheEntryFormOfASeries_TheirTitlesAndMentions_TheRecordsDetailedInTheFirstOnly()
    {
        var page = Open();

        Assert.Equal(["La série", "Avancement", "Calculs", "Les auteurs", "Éditeur et collection", "Genres et univers", "Résumé et notes",
                "Nouvelles éditions"],
            page.FindAll(".block-title").Where(title => title.Closest(".merge-data") is not null).Select(title => title.TextContent.Trim()));
        Assert.Equal(["Identification", "Suivi", "Réglages", "Contributions", "Publication", "Classement", "Lecture", "Pré-remplissage"],
            page.FindAll(".merge-data .block-kicker").Select(kicker => kicker.TextContent.Trim()));
        Assert.Equal(["Titre", "Clé de tri", "Statut"], page.FindAll(".merge-data")[0].QuerySelectorAll(".merge-choice-label").Select(label => label.TextContent.Trim()));
        // The albums and the last modification of each series in the heads of the first block alone.
        Assert.Equal(2, page.FindAll(".merge-data-head-detail").Count);
        Assert.Contains("38 albums", page.FindAll(".merge-data")[0].TextContent);
    }

    [Fact]
    public async Task Choices_ASameValueOnce_AValueOfOneSeriesRetained_TwoValuesWaitingForTheUser()
    {
        var page = Open();
        // Every data presented (« Tout afficher »).
        await page.ChangeAsync(".merge-show-all", true);

        // A credit and a genre of both, the same on both sides, as the exclusions, the category and the template values alike.
        Assert.Contains($"merge-contribution-{Goscinny.Id}-Scenarist-label", Same(page));
        Assert.Contains($"merge-genre-{Humour}-label", Same(page));
        // Uderzo, the genre « Aventure » and the universe of the first series alone, Mébarki of the second alone, retained at once;
        // the sort key entered by hand, retained against the one following the title.
        Assert.Equal("First", Checked(page, $"merge-contribution-{Uderzo.Id}-Illustrator"));
        Assert.Equal("Second", Checked(page, $"merge-contribution-{Mebarki.Id}-Colorist"));
        Assert.Equal("First", Checked(page, $"merge-genre-{Aventure}"));
        Assert.Equal("First", Checked(page, $"merge-universe-{Gaule}"));
        Assert.Equal("First", Checked(page, "merge-sort-key"));
        // The title, the status, the completeness, the publisher and the format differ: nothing is chosen for the user.
        Assert.Equal("5 données restent à choisir", page.Find(".save-bar-state-long").TextContent);
        Assert.All(["merge-title", "merge-status", "merge-complete", "merge-publisher", "merge-format"], id => Assert.Null(Checked(page, id)));
        Assert.True(page.Find(".save-bar button[type=submit]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task SortKey_EnteredByHand_IsTheResult_AndFollowingTheTitle_IsTheOneTheDomainComputesFromTheTitleChosen()
    {
        var page = Open();
        Assert.Empty(page.FindAll(".merge-data-result"));

        await page.ChangeAsync("input[name=merge-title][value=Second]", true);
        Assert.Equal("Classé à : Asterix", page.Find(".merge-data-result").TextContent.Trim());

        await page.ChangeAsync("input[name=merge-sort-key][value=Second]", true);
        Assert.Contains($"/admin/sort-keys/title?title={Uri.EscapeDataString("Asterix")}", _api.Requested);
        Assert.Equal("Classé à : Asterix (calculée)", page.Find(".merge-data-result").TextContent.Trim());
    }

    [Fact]
    public async Task Merge_SendsTheSeriesOfEachDataAndOfEachElementOfOneSeries_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(new MergeResult(First));
        var page = Open();
        foreach (var (row, side) in new[] { ("title", "Second"), ("status", "First"), ("complete", "First"), ("publisher", "First"), ("format", "Second") })
            await page.ChangeAsync($"input[name=merge-{row}][value={side}]", true);
        // Uderzo discarded: the side without him is retained.
        await page.ChangeAsync($"input[name=merge-contribution-{Uderzo.Id}-Illustrator][value=Second]", true);
        Assert.Equal("Tout est choisi", page.Find(".save-bar-state-long").TextContent);

        await page.SubmitAsync("form");
        var confirmation = page.Find("dialog");
        Assert.Contains("Les deux fiches n'en feront plus qu'une : «\u00A0Asterix\u00A0».", confirmation.TextContent);
        Assert.Contains("«\u00A0Asterix\u00A0» regroupera les 40 albums des deux fiches.", confirmation.TextContent);
        // Uderzo, discarded, the confirmation sets apart as deleted.
        Assert.Equal(["1 contribution : «\u00A0Albert Uderzo\u00A0» (Dessin), de «\u00A0Astérix\u00A0»"], page.FindAll(".merge-removals li").Select(line => line.TextContent.Trim()));
        await page.ClickAsync("dialog button[data-variant=delete]");

        var (_, path, body) = Assert.Single(_api.Written);
        Assert.Equal("/admin/series/merge", path);
        var request = body.Deserialize<SeriesMergeRequest>(JsonSerializerOptions.Web)!;
        Assert.Equal((First, 11u, Second, 12u), (request.First.Id, request.First.Version, request.Second.Id, request.Second.Version));
        Assert.Equal((MergeSide.Second, MergeSide.First, MergeSide.First, MergeSide.Second), (request.Title, request.SortKey, request.Publisher, request.Format));
        Assert.Equal([new MergeElement(Aventure, MergeSide.First)], request.Genres);
        Assert.Equal([new MergeElement(Gaule, MergeSide.First)], request.Universes);
        Assert.Equal([new MergeElement(UderzoCredit, MergeSide.Second), new MergeElement(MebarkiCredit, MergeSide.Second)], request.Contributions);
        Assert.EndsWith(AppPaths.AdminSeries, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.series-merge.merged", _audience.Functions);
    }

    [Fact]
    public async Task Merge_OfASeriesModifiedMeanwhile_SaysSoAndOffersToReload()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        var page = Open();
        foreach (var row in new[] { "title", "status", "complete", "publisher", "format" })
            await page.ChangeAsync($"input[name=merge-{row}][value=First]", true);

        await page.SubmitAsync("form");
        await page.ClickAsync("dialog button[data-variant=delete]");

        Assert.Contains("L'une des deux séries a été modifiée depuis l'ouverture de la fusion.", page.Markup);
        Assert.Contains(page.FindAll("button"), button => button.TextContent.Trim() == "Recharger la fiche");
    }

    private IRenderedComponent<AdminMergeSeries> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminMergeSeries}?series={First}&other={Second}");
        return Render<AdminMergeSeries>(parameters => parameters.AddCascadingValue(_request.Context));
    }

    private static string[] Values(IRenderedComponent<AdminMergeSeries> page, string row) =>
        [.. page.FindAll($"input[name={row}]").Select(radio => radio.ParentElement!.QuerySelector(".merge-choice-value")!.TextContent.Trim())];

    private static string? Checked(IRenderedComponent<AdminMergeSeries> page, string row) =>
        page.FindAll($"input[name={row}]").SingleOrDefault(radio => radio.HasAttribute("checked"))?.GetAttribute("value");

    // The rows presented once, the same in both series, by their label.
    private static List<string> Same(IRenderedComponent<AdminMergeSeries> page) =>
        [.. page.FindAll(".merge-choice-same").Select(same => same.PreviousElementSibling!.Id!)];
}
