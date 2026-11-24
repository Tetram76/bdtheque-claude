using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
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
/// The list of the albums of the consultation: its rows, each leading to the record of its album, its count, its filters, the
/// possession among them, which limits it to the collection (fonctionnel/regles-metier/perimetre-de-la-consultation.md), its
/// empty lists and its errors, told apart by role, link and text, never by appearance.
/// </summary>
public sealed class AlbumListTests : BunitContext, IDisposable
{
    private static readonly AlbumCatalogListItem Bijoux = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000001"), "Les Bijoux de la Castafiore", Guid.NewGuid(), "Tintin",
            AlbumType.Regular, false, 21, null, null, true),
        new AlbumCover(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette),
        [new AuthorListItem(Guid.NewGuid(), "Remi", "Georges", "Hergé")],
        1963, null, AlbumRating.VeryGood, AlbumCollectionState.InCollection);

    private static readonly AlbumCatalogListItem Blacksad = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000002"), "Quelque part entre les ombres", Guid.NewGuid(), "Blacksad",
            AlbumType.Regular, false, 1, null, null, false),
        null,
        [new AuthorListItem(Guid.NewGuid(), "Díaz Canales", "Juan", null), new AuthorListItem(Guid.NewGuid(), "Guarnido", "Juanjo", null)],
        2000, 11, null, AlbumCollectionState.PurchaseIntent);

    private static readonly AlbumCatalogListItem Bleu = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000003"), "Le Bleu est une couleur chaude", null, null,
            AlbumType.Regular, false, null, null, null, false),
        null, [], null, null, null, AlbumCollectionState.OutsideCollection);

    private static readonly AlbumListFilters NoFilters = new(null, null, null, null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visitor, not signed in, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: false);

    public AlbumListTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Bijoux, Blacksad, Bleu], totalCount: 3);
        // The whole list, whose number the heading gives when criteria narrow the list.
        _api.Answers[CatalogAlbumListQuery.All.ApiPath(1)] = new AlbumCatalogList(Page([Bijoux], 2847), NoFilters);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Row_LeadsToTheRecordOfItsAlbum_NamedByItsLabelWithItsAuthorsItsDateAndItsPossession()
    {
        var rows = Open("").FindAll(".album-row:not(.album-row-head)");

        Assert.Equal(3, rows.Count);
        var bijoux = rows[0];
        // The label in the format of the settings, the series not being plain in a list (fonctionnel/presentation/libelle-d-un-album.md).
        var link = bijoux.QuerySelector($"a[href='{AppPaths.Album(Bijoux.Album.Id)}']")!;
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - T. 21)", Text(link));
        Assert.Contains("Hergé", Text(bijoux.QuerySelector(".album-main")!));
        Assert.Equal("1963", Text(bijoux.QuerySelector(".album-date")!));
        Assert.Equal("Possédé", Text(bijoux.QuerySelector(".album-state")!));
        Assert.Equal("Intention", Text(rows[1].QuerySelector(".album-state")!));
        Assert.Equal("Catalogué", Text(rows[2].QuerySelector(".album-state")!));
    }

    [Fact]
    public void Row_WithoutAuthor_HasNothingUnderItsLabel_AndWithoutDate_TheDashOfAMissingData()
    {
        var bleu = Open("").FindAll(".album-row:not(.album-row-head)")[2];

        Assert.Equal("Le Bleu est une couleur chaude", Text(bleu.QuerySelector(".album-main")!));
        Assert.Equal("—", bleu.QuerySelector(".album-date [aria-hidden]")!.TextContent.Trim());
        // A rating not given: marked, for the row on two lines to withdraw it, as a zero of a list.
        Assert.True(bleu.QuerySelector(".album-rating")!.HasAttribute("data-none"));
        Assert.False(Open("").FindAll(".album-row:not(.album-row-head)")[0].QuerySelector(".album-rating")!.HasAttribute("data-none"));
    }

    [Fact]
    public void Heading_OfAListOfTheConsultation_LeadsNeitherToTheAdministration_NorNamesASetOfRecords()
    {
        var page = Open("");

        Assert.Empty(page.FindAll(".list-heading nav"));
        Assert.DoesNotContain(page.FindAll("a"), a => a.GetAttribute("href") == AppPaths.AdminHome);
        Assert.Empty(page.FindAll(".block-kicker"));
    }

    [Fact]
    public void Heading_GivesTheNumberOfAllTheAlbums_AndTheSummaryThoseFound()
    {
        Answer([Bijoux], totalCount: 1);

        var page = Open("state=InCollection");

        Assert.Equal("2 847", Text(page.Find(".list-heading-count")));
        Assert.Contains("1 album sur 2 847", Text(page.Find(".list-summary")));
    }

    [Fact]
    public void Possession_IsOneOfTheFilters_ChosenInAListOfTheStatesOfAnAlbum()
    {
        var page = Open("filters=open&state=PurchaseIntent");

        var select = page.Find("select[name='state']");
        Assert.Equal(
            ["Tous les albums", "Possédé", "Intention", "Catalogué"],
            select.QuerySelectorAll("option").Select(Text));
        Assert.Equal("PurchaseIntent", select.QuerySelector("option[selected]")!.GetAttribute("value"));
        Assert.Equal(AppPaths.Albums, page.Find("form.list-search").GetAttribute("action"));
    }

    [Fact]
    public void Possession_ChosenIsAnActiveFilter_WhichTheNavigationAndThePagesKeep()
    {
        Answer([Bijoux], totalCount: 120);

        var page = Open("state=InCollection");

        Assert.Contains("Possession", Text(page.Find(".active-filters")));
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.Albums}?entry=B&state=InCollection");
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.Albums}?state=InCollection&page=2");
    }

    [Theory]
    [InlineData("q=zorglub", "Aucun album ne correspond à « zorglub ».", "Effacer la recherche", AppPaths.Albums)]
    [InlineData("state=InCollection", "Aucun album ne correspond à ces critères.", "Effacer les filtres", AppPaths.Albums)]
    [InlineData("entry=Q", "Aucun album sous cette entrée.", "Tous les albums", AppPaths.Albums)]
    public void EmptyList_SaysWhyAndOffersToClearWhatNarrowsIt_NeverToCreateAnAlbum(string criteria, string message, string action, string href)
    {
        Answer([], totalCount: 0);

        var empty = Open(criteria).Find(".empty-list");

        Assert.Contains(message, Text(empty));
        var link = Assert.Single(empty.QuerySelectorAll(".empty-list-actions a"));
        Assert.Equal((action, href), (Text(link), link.GetAttribute("href")));
    }

    [Fact]
    public void EmptyList_WithoutCriterion_SaysThatNoAlbumIsRecorded_WithoutAction()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Contains("Aucun album n'est encore enregistré.", Text(empty));
        Assert.Empty(empty.QuerySelectorAll(".empty-list-actions a"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/catalog/albums"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".album-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("state=InCollection", recorded: true);

        Assert.Equal(["albums.filter", "albums"], _audience.Functions);
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

    private void Answer(IReadOnlyList<AlbumCatalogListItem> items, int totalCount) =>
        _api.Answers["/catalog/albums"] = new AlbumCatalogList(Page(items, totalCount), NoFilters);

    private static Page<AlbumCatalogListItem> Page(IReadOnlyList<AlbumCatalogListItem> items, int totalCount) =>
        new(items, 1, ListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
