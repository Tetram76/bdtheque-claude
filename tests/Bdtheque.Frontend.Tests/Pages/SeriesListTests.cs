using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the series of the consultation: its rows, each leading to the record of its series with its albums in the
/// collection, its count, its filters, the possession among them, the same as in every list
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md), its empty lists and its errors, told apart by role, link and
/// text, never by appearance.
/// </summary>
public sealed class SeriesListTests : BunitContext, IDisposable
{
    private static readonly SeriesCatalogListItem Gaston = new(
        Guid.Parse("01900000-0000-7000-8000-000000000011"), "Gaston",
        new SeriesOverview(3, [new AlbumCover(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette), null, null]), 1);

    private static readonly SeriesCatalogListItem Blueberry = new(
        Guid.Parse("01900000-0000-7000-8000-000000000012"), "Blueberry", new SeriesOverview(28, [null, null, null]), 0);

    private static readonly SeriesCatalogListItem Empty = new(
        Guid.Parse("01900000-0000-7000-8000-000000000013"), "Zorro", new SeriesOverview(0, []), 0);

    private static readonly SeriesListFilters NoFilters = new(null, null, null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visitor, not signed in, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: false);

    public SeriesListTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Blueberry, Gaston, Empty], totalCount: 3);
        // The whole list, whose number the heading gives when criteria narrow the list.
        _api.Answers[CatalogSeriesListQuery.All.ApiPath(1)] = new SeriesCatalogList(Page([Gaston], 412), NoFilters);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Row_LeadsToTheRecordOfItsSeries_WithItsPileOfCoversAndItsAlbumsInTheCollection()
    {
        var rows = Open("").FindAll(".series-row:not(.series-row-head)");

        Assert.Equal(3, rows.Count);
        var gaston = rows[1];
        Assert.Equal("Gaston", Text(gaston.QuerySelector($"a[href='{AppPaths.SeriesRecord(Gaston.Id)}']")!));
        Assert.Equal(3, gaston.QuerySelectorAll(".series-pile .cover").Length);
        // Out of all its albums, a zero included (visuel/charte-graphique/convivialite-et-efficacite.md).
        Assert.Equal("1 album sur 3 dans la collection", Text(gaston.QuerySelector(".series-albums")!));
        Assert.Equal("0 album sur 28 dans la collection", Text(rows[0].QuerySelector(".series-albums")!));
    }

    [Fact]
    public void Row_OfASeriesWithoutAlbum_HasTheDashOfAZero()
    {
        var zorro = Open("").FindAll(".series-row:not(.series-row-head)")[2];

        Assert.Equal("—", zorro.QuerySelector(".series-albums [aria-hidden]")!.TextContent.Trim());
        Assert.Contains("Aucun album", Text(zorro.QuerySelector(".series-albums")!));
    }

    [Fact]
    public void Heading_GivesTheNumberOfAllTheSeries_AndTheSummaryThoseFound()
    {
        Answer([Gaston], totalCount: 1);

        var page = Open("state=InCollection");

        Assert.Equal("412", Text(page.Find(".list-heading-count")));
        Assert.Contains("1 série sur 412", Text(page.Find(".list-summary")));
        Assert.Empty(page.FindAll(".list-heading nav"));
    }

    [Fact]
    public void Filters_AreThoseOfTheListOfTheAdministration_AndThePossession()
    {
        var page = Open("filters=open&state=PurchaseIntent");

        Assert.Equal(
            ["filter-author", "filter-publisher", "filter-collection", "filter-genre", "filter-universe", "filter-state"],
            page.FindAll(".list-filters label[for]").Select(label => label.GetAttribute("for")!));
        var select = page.Find("select[name='state']");
        Assert.Equal(["Toutes les séries", "Possédé", "Intention", "Non possédé"], select.QuerySelectorAll("option").Select(Text));
        Assert.Equal("PurchaseIntent", select.QuerySelector("option[selected]")!.GetAttribute("value"));
        Assert.Equal(AppPaths.Series, page.Find("form.list-search").GetAttribute("action"));
    }

    [Fact]
    public void Possession_ChosenIsAnActiveFilter_WhichTheNavigationAndThePagesKeep()
    {
        Answer([Gaston], totalCount: 120);

        var page = Open("state=InCollection");

        Assert.Contains("Possession : Possédé", Text(page.Find(".active-filters")));
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.Series}?entry=B&state=InCollection");
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.Series}?state=InCollection&page=2");
    }

    [Theory]
    [InlineData("q=zorglub", "Aucune série ne correspond à « zorglub ».", "Effacer la recherche")]
    [InlineData("state=InCollection", "Aucune série ne correspond à ces critères.", "Effacer les filtres")]
    [InlineData("entry=Q", "Aucune série sous cette entrée.", "Toutes les séries")]
    public void EmptyList_SaysWhyAndOffersToClearWhatNarrowsIt_NeverToCreateASeries(string criteria, string message, string action)
    {
        Answer([], totalCount: 0);

        var empty = Open(criteria).Find(".empty-list");

        Assert.Contains(message, Text(empty));
        var link = Assert.Single(empty.QuerySelectorAll(".empty-list-actions a"));
        Assert.Equal((action, AppPaths.Series), (Text(link), link.GetAttribute("href")));
    }

    [Fact]
    public void EmptyList_WithoutCriterion_SaysThatNoSeriesIsRecorded_WithoutAction()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Contains("Aucune série n'est encore enregistrée.", Text(empty));
        Assert.Empty(empty.QuerySelectorAll(".empty-list-actions a"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/catalog/series"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".series-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("state=InCollection", recorded: true);

        Assert.Equal(["series.list.filter", "series.list"], _audience.Functions);
    }

    private IRenderedComponent<SeriesList> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.Series}?{criteria}");
        return Render<SeriesList>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<SeriesCatalogListItem> items, int totalCount) =>
        _api.Answers["/catalog/series"] = new SeriesCatalogList(Page(items, totalCount), NoFilters);

    private static Page<SeriesCatalogListItem> Page(IReadOnlyList<SeriesCatalogListItem> items, int totalCount) =>
        new(items, 1, ListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
