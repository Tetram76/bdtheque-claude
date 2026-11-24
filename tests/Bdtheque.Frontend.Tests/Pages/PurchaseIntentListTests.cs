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
/// The public list of the purchase intents: its rows, each an intent leading to the record of the album it targets, with what
/// it targets, its count, its search and navigation, without any filter, its empty lists and its errors, told apart by role,
/// link and text, never by appearance.
/// </summary>
public sealed class PurchaseIntentListTests : BunitContext, IDisposable
{
    private static readonly PurchaseIntentListItem OnEdition = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000e1"),
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000001"), "Les Bijoux de la Castafiore", Guid.NewGuid(), "Tintin",
            AlbumType.Regular, false, 21, null, null, true),
        AlbumCollectionState.InCollection,
        new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Casterman", Guid.NewGuid(), "Petit format", 2013, null),
        "https://www.example.org/casterman",
        new AlbumCover(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette),
        new DateTimeOffset(2026, 3, 14, 10, 0, 0, TimeSpan.Zero));

    private static readonly PurchaseIntentListItem OnAlbum = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000e2"),
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000002"), "Le Dernier Convoi", null, null,
            AlbumType.Regular, false, null, null, null, false),
        AlbumCollectionState.PurchaseIntent,
        null,
        null,
        null,
        new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.Zero));

    private static readonly PurchaseIntentListItem WithoutSite = OnEdition with
    {
        Id = Guid.Parse("01900000-0000-7000-8000-0000000000e3"),
        Edition = new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "13 Bulles", null, null, null, null),
        PublisherWebsite = null,
    };

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visitor, not signed in, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: false);

    public PurchaseIntentListTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([OnEdition, OnAlbum, WithoutSite], totalCount: 3);
        // The whole list, whose number the heading gives when criteria narrow the list.
        _api.Answers[CatalogPurchaseIntentListQuery.All.ApiPath(1)] = Page([OnEdition], 12);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Row_LeadsToTheRecordOfTheAlbumTargeted_NamedByItsLabel_WithItsPossessionAndTheDateOfTheIntent()
    {
        var rows = Rows(Open(""));

        Assert.Equal(3, rows.Count);
        // The label in the format of the settings, the series not being plain in a list (fonctionnel/presentation/libelle-d-un-album.md).
        var link = rows[0].QuerySelector($".intent-main a[href='{AppPaths.Album(OnEdition.Album.Id)}']")!;
        Assert.Equal("Les Bijoux de la Castafiore (Tintin - T. 21)", Text(link));
        Assert.Equal("Possédé", Text(rows[0].QuerySelector(".intent-state")!));
        Assert.Equal("Intention", Text(rows[1].QuerySelector(".intent-state")!));
        Assert.Contains("14/03/2026", Text(rows[0].QuerySelector(".intent-created")!));
    }

    [Fact]
    public void Row_SaysWhatTheIntentTargets_TheWholeAlbumOrAnEditionNamedByItsLabel()
    {
        var rows = Rows(Open(""));

        Assert.Equal("Album toute édition", Text(rows[1].QuerySelector(".intent-target")!));
        // Its publisher, which leads to its site, announced as such to screen readers.
        Assert.Equal("Édition Casterman, site externe (Petit format) [2013]", Text(rows[0].QuerySelector(".intent-target")!));
    }

    [Fact]
    public void Row_OfAnEdition_LeadsToTheSiteOfItsPublisher_WhenItIsKnown()
    {
        var rows = Rows(Open(""));

        // The label of the edition is no link itself: its publisher leads to its site (fonctionnel/presentation/libelle-d-une-edition.md).
        var site = Assert.Single(rows[0].QuerySelectorAll(".intent-target a"));
        Assert.Equal("https://www.example.org/casterman", site.GetAttribute("href"));
        Assert.StartsWith("Casterman", Text(site));
        Assert.Empty(rows[2].QuerySelectorAll(".intent-target a"));
        Assert.Contains("13 Bulles", Text(rows[2].QuerySelector(".intent-target")!));
    }

    [Fact]
    public void List_HasNoFilter_NorAnyAction()
    {
        var page = Open("");

        // Neither the button of the filters nor their panel, and no row has a button: the consultation has no action.
        Assert.Empty(page.FindAll(".list-search [aria-controls], .list-search-panel, .active-filters, select"));
        Assert.Empty(page.FindAll(".intent-row button"));
        Assert.Equal(AppPaths.PurchaseIntents, page.Find("form.list-search").GetAttribute("action"));
    }

    [Fact]
    public void Heading_GivesTheNumberOfAllTheIntents_AndTheSummaryThoseFound()
    {
        Answer([OnEdition], totalCount: 1);

        var page = Open("q=bijoux");

        Assert.Equal("12", Text(page.Find(".list-heading-count")));
        Assert.Contains("1 intention sur 12", Text(page.Find(".list-summary")));
    }

    [Fact]
    public void Navigation_AndPages_KeepTheSearch()
    {
        Answer([OnEdition], totalCount: 120);

        var page = Open("q=tintin");

        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.PurchaseIntents}?q=tintin&entry=B");
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.PurchaseIntents}?q=tintin&page=2");
    }

    [Theory]
    [InlineData("q=zorglub", "Aucune intention d'acquisition ne correspond à « zorglub ».", "Effacer la recherche", AppPaths.PurchaseIntents)]
    [InlineData("entry=Q", "Aucune intention d'acquisition sous cette entrée.", "Toutes les intentions d'acquisition", AppPaths.PurchaseIntents)]
    public void EmptyList_SaysWhyAndOffersToClearWhatNarrowsIt(string criteria, string message, string action, string href)
    {
        Answer([], totalCount: 0);

        var empty = Open(criteria).Find(".empty-list");

        Assert.Contains(message, Text(empty));
        var link = Assert.Single(empty.QuerySelectorAll(".empty-list-actions a"));
        Assert.Equal((action, href), (Text(link), link.GetAttribute("href")));
    }

    [Fact]
    public void EmptyList_WithoutCriterion_SaysThatThereIsNoIntent_WithoutAction()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Contains("Aucune intention d'acquisition.", Text(empty));
        Assert.Empty(empty.QuerySelectorAll(".empty-list-actions a"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/catalog/purchase-intents"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".intent-row"));
    }

    [Theory]
    [InlineData("", new[] { "purchase-intents" })]
    [InlineData("q=tintin", new[] { "purchase-intents.search", "purchase-intents" })]
    [InlineData("entry=B", new[] { "purchase-intents.initial", "purchase-intents" })]
    [InlineData("page=2", new[] { "purchase-intents.page", "purchase-intents" })]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen(string criteria, string[] functions)
    {
        Answer([OnEdition], totalCount: 120);

        Open(criteria, recorded: true);

        Assert.Equal(functions, _audience.Functions);
    }

    private IRenderedComponent<PurchaseIntentList> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.PurchaseIntents}?{criteria}");
        return Render<PurchaseIntentList>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<PurchaseIntentList> page) => page.FindAll(".intent-row:not(.intent-row-head)");

    private void Answer(IReadOnlyList<PurchaseIntentListItem> items, int totalCount) =>
        _api.Answers["/catalog/purchase-intents"] = Page(items, totalCount);

    private static Page<PurchaseIntentListItem> Page(IReadOnlyList<PurchaseIntentListItem> items, int totalCount) =>
        new(items, 1, ListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
