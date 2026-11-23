using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the albums of the consultation: its heading, its rows, the filter on the possession, its navigation by
/// initial, its empty lists and its errors, told apart by role, link and text, never by appearance.
/// </summary>
public sealed class AlbumListTests : BunitContext, IDisposable
{
    private const string WholeList = "/catalog/albums?page=1&pageSize=1";

    private static readonly AlbumListItem Bijoux = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000001"), "Les Bijoux de la Castafiore", Guid.NewGuid(), "Tintin",
            AlbumType.Regular, false, 21, null, null, true),
        new AlbumCover(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette),
        [new AuthorListItem(Guid.NewGuid(), "Remi", "Georges", "Hergé")],
        1963, null, AlbumCollectionState.InCollection);

    private static readonly AlbumListItem Blacksad = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000002"), null, Guid.NewGuid(), "Blacksad",
            AlbumType.Omnibus, false, null, 1, 5, false),
        null,
        [new AuthorListItem(Guid.NewGuid(), "Díaz Canales", "Juan", null), new AuthorListItem(Guid.NewGuid(), "Guarnido", "Juanjo", null)],
        2023, 10, AlbumCollectionState.PurchaseIntent);

    private static readonly AlbumListItem Bleu = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000003"), "Le Bleu est une couleur chaude", null, null,
            AlbumType.Regular, false, null, null, null, false),
        null, [], null, null, AlbumCollectionState.OutsideCollection);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() });

    public AlbumListTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Answer([Bijoux, Blacksad, Bleu], totalCount: 143);
        _api.Answers[WholeList] = Page([Bijoux], 2847);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleAndTheNumberOfAllTheAlbums_WithoutAnyCreation()
    {
        var page = Open("state=InCollection");

        Assert.Equal(("Albums", "2 847"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".catalog-list-count"))));
        Assert.Empty(page.FindAll("a.button[href^='/admin']"));
    }

    [Fact]
    public void Heading_OfTheWholeList_CountsItsAlbumsWithoutReadingThemAgain()
    {
        var page = Open("");

        Assert.Equal("143", Text(page.Find(".catalog-list-count")));
        Assert.DoesNotContain(WholeList, _api.Requested);
    }

    [Fact]
    public void Header_NamesTheColumns_ThePossessionOfTheAlbum()
    {
        Assert.Equal(
            ["Couverture", "Album", "Parution", "Possession"],
            Open("").FindAll(".album-row-head [role='columnheader']").Select(Text));
    }

    [Fact]
    public void Rows_TheAlbumLeadingToItsRecord_ItsAuthorsItsPublicationAndItsPossession()
    {
        var rows = Open("").FindAll(".album-row:not(.album-row-head)").Select(row => (
            Text(row.QuerySelector(".row-entry")!),
            row.QuerySelector(".row-entry a")!.GetAttribute("href"),
            row.QuerySelector(".row-entry-detail") is { } authors ? Text(authors) : null,
            Text(row.QuerySelector(".album-date")!),
            Text(row.QuerySelector(".album-state")!)));

        Assert.Equal(
            [
                ("Les Bijoux de la Castafiore (Tintin - T. 21)", AppPaths.Album(Bijoux.Album.Id), "Hergé", "1963", "Possédé"),
                ("Blacksad - INT. [1 à 5]", AppPaths.Album(Blacksad.Album.Id), "Juan Díaz Canales · Juanjo Guarnido", "octobre 2023", "Intention"),
                // No author: nothing under the label, a column of a card; no date: the dash of a missing data in a column, said to
                // a screen reader (visuel/charte-graphique/convivialite-et-efficacite.md).
                ("Le Bleu est une couleur chaude", AppPaths.Album(Bleu.Album.Id), null, "—Non renseignée", "Non possédé"),
            ],
            rows);
    }

    [Fact]
    public void Rows_ShowTheCoverTheListReceived_TheGenericVisualWithout()
    {
        var covers = Open("").FindAll(".album-row:not(.album-row-head) .album-cover .cover")
            .Select(cover => cover.GetAttribute("src") ?? cover.ClassName);

        Assert.Equal([VisualMediaRoutes.Display(Bijoux.Cover!.VisualId), "cover cover-generic", "cover cover-generic"], covers);
    }

    [Fact]
    public void Possession_EveryStateAndAllTheAlbums_TheChosenOneCurrent_EachKeepingTheEntryBackOnTheFirstPage()
    {
        var filter = Open("entry=B&state=PurchaseIntent&page=2").Find("nav.filter-choices");

        Assert.Equal("Possession", filter.GetAttribute("aria-label"));
        Assert.Equal(
            [
                ("Tous les albums", $"{AppPaths.Albums}?entry=B", null),
                ("Possédé", $"{AppPaths.Albums}?entry=B&state=InCollection", null),
                ("Intention", $"{AppPaths.Albums}?entry=B&state=PurchaseIntent", "true"),
                ("Non possédé", $"{AppPaths.Albums}?entry=B&state=OutsideCollection", null),
            ],
            filter.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"), link.GetAttribute("aria-current"))));
    }

    [Fact]
    public void Possession_IsReadFromTheAddressAndSentToTheApi()
    {
        Open("entry=B&state=InCollection&page=2");

        Assert.Contains("/catalog/albums?entry=B&state=InCollection&page=2&pageSize=50", _api.Requested);
    }

    [Fact]
    public void Navigation_AndPagination_KeepThePossession()
    {
        Answer([Bijoux, Blacksad, Bleu], totalCount: 143);
        var page = Open("state=InCollection");

        Assert.Equal($"{AppPaths.Albums}?entry=B&state=InCollection", page.Find("nav.initial-navigation a[href*='entry=B']").GetAttribute("href"));
        Assert.Contains(
            $"{AppPaths.Albums}?state=InCollection&page=2",
            page.FindAll(".pagination a").Select(link => link.GetAttribute("href")));
    }

    [Theory]
    [InlineData("", "143 albums")]
    [InlineData("entry=B", "143 albums sous la lettre B")]
    [InlineData("state=InCollection", "143 albums sur 2 847")]
    public void Summary_TheCountUnderTheEntryOrOutOfAllTheAlbums_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Triés par titre, la série tenant lieu de titre aux albums qui n'en ont pas", Text(summary.Children[1]));
    }

    [Theory]
    // As in the lists of the administration (visuel/liste-albums/contenu.md, Liste vide): the filter cleared, the entry staying the
    // one the list is under; the entry, which is no criterion, left for « Toutes ».
    [InlineData("entry=B&state=InCollection", "Aucun album ne correspond à ce filtre.", "Effacer le filtre", "/albums?entry=B")]
    [InlineData("entry=B", "Aucun album sous cette entrée.", "Tous les albums", "/albums")]
    public void Empty_SaysWhyAndOffersToClearWhatRestrictsIt(string criteria, string message, string action, string href)
    {
        Answer([], totalCount: 0);

        var empty = Open(criteria).Find(".empty-list");

        Assert.Equal(message, Text(empty.QuerySelector("strong")!));
        Assert.Equal([(action, href)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_WithoutAnyAlbum_SaysSoWithoutAction()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucun album n'est encore enregistré.", Text(empty.QuerySelector("strong")!));
        Assert.Empty(empty.QuerySelectorAll("a"));
    }

    [Fact]
    public void Empty_TheListAndItsFilterStillThere()
    {
        Answer([], totalCount: 0);

        var page = Open("state=OutsideCollection");

        Assert.NotNull(page.Find("nav.filter-choices"));
        Assert.Empty(page.FindAll(".album-row"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/catalog/albums"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Equal("Albums", Text(page.Find("h1")));
        Assert.Empty(page.FindAll(".album-row"));
    }

    [Theory]
    [InlineData("", new[] { "albums" })]
    [InlineData("state=InCollection", new[] { "albums.filter", "albums" })]
    [InlineData("entry=B&state=InCollection", new[] { "albums.initial", "albums" })]
    [InlineData("state=InCollection&page=2", new[] { "albums.page", "albums" })]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen(string criteria, string[] functions)
    {
        Open(criteria, recorded: true);

        Assert.Equal(functions, _audience.Functions);
    }

    private IRenderedComponent<AlbumList> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.Albums}?{criteria}");
        return Render<AlbumList>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<AlbumListItem> items, int totalCount) => _api.Answers["/catalog/albums"] = Page(items, totalCount);

    private static Page<AlbumListItem> Page(IReadOnlyList<AlbumListItem> items, int totalCount) => new(items, 1, ListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
