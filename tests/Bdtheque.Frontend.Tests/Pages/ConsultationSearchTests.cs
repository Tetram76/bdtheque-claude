using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The simple search of the consultation, from its header (fonctionnel/structure-de-l-application.md): the records of the base
/// found by the search of each list of the consultation, in the single order of the types, presented by the rows of their list,
/// each leading where the consultation leads it; told apart by role, link and text, never by appearance.
/// </summary>
public sealed class ConsultationSearchTests : BunitContext, IDisposable
{
    private static readonly AlbumSummary Album = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), "Le Tour de Gaule", Guid.Parse("01900000-0000-7000-8000-000000000002"),
        "Astérix", AlbumType.Regular, false, 5, null, null, true);

    private static readonly EditionSummary Edition = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), Guid.Parse("01900000-0000-7000-8000-000000000004"), "Dargaud", null, null,
        1965, "9782012101340");

    private static readonly Guid CollectionId = Guid.Parse("01900000-0000-7000-8000-000000000005");
    private static readonly Guid AuthorId = Guid.Parse("01900000-0000-7000-8000-000000000006");
    private static readonly Guid GenreId = Guid.Parse("01900000-0000-7000-8000-000000000007");
    private static readonly Guid UniverseId = Guid.Parse("01900000-0000-7000-8000-000000000008");
    private static readonly Guid IntentId = Guid.Parse("01900000-0000-7000-8000-00000000000a");

    private static readonly AlbumListFilters NoAlbumFilters = new(null, null, null, null, null, null);
    private static readonly SeriesListFilters NoSeriesFilters = new(null, null, null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visitor, not signed in, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: false);

    public ConsultationSearchTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        AnswerNothing();
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleWithoutAdministration_AndTheSearchOnItsText()
    {
        var page = Open("q=ast");

        Assert.Empty(page.FindAll(".breadcrumb"));
        Assert.Equal("Recherche", Text(page.Find("h1")));
        var form = page.Find("form.list-search");
        Assert.Equal(AppPaths.Search, form.GetAttribute("action"));
        var field = form.QuerySelector("input[type='search']")!;
        Assert.Equal(("q", "ast"), (field.GetAttribute("name"), field.GetAttribute("value")));
        // Its cross leads to the search without text (visuel/liste-albums/controles.md).
        Assert.Equal(AppPaths.Search, form.QuerySelector("a.search-clear")!.GetAttribute("href"));
    }

    [Fact]
    public void WithoutText_InvitesToSearch_WithoutReadingAnything()
    {
        var page = Open("q=%20");

        Assert.Equal("Saisissez un titre, un nom ou un ISBN.", Text(page.Find(".empty-list strong")));
        Assert.Empty(page.FindAll(".search-group"));
        Assert.DoesNotContain(_api.Requested, path => path.StartsWith("/catalog/", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryType_ReadByTheSearchOfTheConsultation_WithItsFirstRecordsOnly_NeverByTheAdministration()
    {
        Open("q=ast");

        string[] expected =
        [
            "/catalog/series?q=ast&page=1&pageSize=5", "/catalog/albums?q=ast&page=1&pageSize=5", "/catalog/publishers?q=ast&pageSize=5",
            "/catalog/publisher-collections?q=ast&pageSize=5", "/catalog/editions?q=ast&pageSize=5&inCollection=true",
            "/catalog/purchase-intents?q=ast&page=1&pageSize=5", "/catalog/authors?q=ast&page=1&pageSize=5", "/catalog/genres?q=ast&pageSize=5",
            "/catalog/universes?q=ast&pageSize=5",
        ];
        Assert.All(expected, path => Assert.Contains(path, _api.Requested));
        Assert.DoesNotContain(_api.Requested, path => path.StartsWith("/admin/", StringComparison.Ordinal));
    }

    [Fact]
    public void Groups_InTheSingleOrderOfTheTypes_EachPresentedAsItsListPresentsIt()
    {
        AnswerEveryType();

        var page = Open("q=ast");
        var groups = page.FindAll(".search-group").Select(group => Text(group.QuerySelector("h2")!));

        // Series, albums, publishers, collections, editions, intents, authors, genres, universes
        // (fonctionnel/presentation/ordre-des-types-de-fiche.md); no saved search, which the consultation does not present.
        Assert.Equal(
            ["Séries", "Albums", "Éditeurs", "Collections", "Éditions", "Intentions d'acquisition", "Auteurs", "Genres", "Univers"],
            groups);
        // The rows of the lists of the consultation, their possession shown.
        Assert.NotNull(page.Find(".search-group .series-rows"));
        Assert.Equal("Possédé", Text(page.Find(".search-group .album-rows .album-row:not(.album-row-head) .album-state")));
        Assert.NotNull(page.Find(".search-group .intent-rows"));
        Assert.NotNull(page.Find(".search-group .author-rows"));
    }

    [Fact]
    public void EachRecord_LeadsWhereTheConsultationLeadsIt()
    {
        AnswerEveryType();

        var page = Open("q=ast");
        var links = page.FindAll(".search-group a:not(.search-more a)").Select(link => (Text(link), link.GetAttribute("href"))).ToList();

        Assert.Contains(("Astérix", AppPaths.SeriesRecord(Album.SeriesId!.Value)), links);
        Assert.Contains(("Le Tour de Gaule (Astérix - T. 5)", AppPaths.Album(Album.Id)), links);
        // A publisher to its website, an edition to the record of its album, a genre and a universe to their works.
        Assert.Contains(links, link => link.Item1.StartsWith("Astéroïde", StringComparison.Ordinal) && link.Item2 == "https://asteroide.example");
        Assert.Contains(("Dargaud [1965] - ISBN 9782012101340", AppPaths.Album(Album.Id)), links);
        Assert.Contains(("Astrid", AppPaths.AuthorRecord(AuthorId)), links);
        Assert.Contains(("Aventure astrale", AppPaths.GenreWorks(GenreId)), links);
        Assert.Contains(("Astres", AppPaths.UniverseWorks(UniverseId)), links);
        // A collection leads nowhere, named with its publisher.
        var collection = page.FindAll(".search-group")[3];
        Assert.Null(collection.QuerySelector("a"));
        Assert.Equal(("Astéroïde", "Dargaud"), (Text(collection.QuerySelector(".row-entry")!), Text(collection.QuerySelector(".row-entry-detail")!)));
    }

    [Fact]
    public void MoreRecordsThanPresented_LeadToTheListOfTheConsultationSearchedOnTheSameText()
    {
        var albums = Enumerable.Range(1, ConsultationSearch.HitCount)
            .Select(i => new AlbumCatalogListItem(Album with { Id = Guid.CreateVersion7(), Title = $"Astérix {i}" }, null, [], null, null, null, AlbumCollectionState.OutsideCollection))
            .ToList();
        _api.Answers["/catalog/albums"] = new AlbumCatalogList(new Page<AlbumCatalogListItem>(albums, 1, ConsultationSearch.HitCount, 28), NoAlbumFilters);

        var group = Open("q=ast").Find(".search-group");

        Assert.Equal(ConsultationSearch.HitCount, group.QuerySelectorAll(".album-row:not(.album-row-head)").Length);
        var more = group.QuerySelector(".search-more a")!;
        Assert.Equal(("Voir les 28 albums", $"{AppPaths.Albums}?q=ast"), (Text(more), more.GetAttribute("href")));
    }

    [Fact]
    public void MoreRecordsThanPresented_OfATypeWithoutListInTheConsultation_AreCountedOnly()
    {
        var genres = Enumerable.Range(1, ConsultationSearch.HitCount).Select(i => new GenreListItem(Guid.CreateVersion7(), $"Astral {i}")).ToList();
        _api.Answers["/catalog/genres"] = new Page<GenreListItem>(genres, 1, ConsultationSearch.HitCount, 7);

        var group = Open("q=ast").Find(".search-group");

        Assert.Equal("et 2 genres de plus", Text(group.QuerySelector(".search-more")!));
        Assert.Null(group.QuerySelector(".search-more a"));
    }

    [Fact]
    public void NothingFound_SaysSo_AndRecallsWhatTheSearchLooksInto()
    {
        var page = Open("q=zzz");

        var empty = page.Find(".empty-list");
        Assert.Equal("Rien ne correspond à « zzz ».", Text(empty.QuerySelector("strong")!));
        Assert.StartsWith("La recherche porte sur ce qui nomme chaque fiche dans sa liste", Text(empty.QuerySelector("span")!));
        Assert.EndsWith("sans tenir compte des majuscules ni des accents.", Text(empty.QuerySelector("span")!));
        Assert.Empty(page.FindAll(".search-group"));
    }

    [Fact]
    public void AnErrorOfTheApi_IsPresentedInsteadOfTheRecords()
    {
        AnswerEveryType();
        _api.Failures["/catalog/albums"] = HttpStatusCode.InternalServerError;

        var page = Open("q=ast");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".search-group"));
    }

    [Fact]
    public void Audience_TheSearchWithItsText_ThenTheScreen()
    {
        Open("q=Ast%C3%A9rix%20le", recorded: true);

        Assert.Equal(["search.query", "search"], _audience.Functions);
        Assert.Contains(_audience.Sent, sent => sent.Content.GetRawText().Contains("q=Ast%C3%A9rix%20le", StringComparison.Ordinal));
    }

    [Fact]
    public void Audience_WithoutText_TheScreenOnly()
    {
        Open("", recorded: true);

        Assert.Equal(["search"], _audience.Functions);
    }

    private void AnswerNothing()
    {
        _api.Answers["/catalog/series"] = new SeriesCatalogList(new Page<SeriesCatalogListItem>([], 1, ConsultationSearch.HitCount, 0), NoSeriesFilters);
        _api.Answers["/catalog/albums"] = new AlbumCatalogList(new Page<AlbumCatalogListItem>([], 1, ConsultationSearch.HitCount, 0), NoAlbumFilters);
        _api.Answers["/catalog/publishers"] = new Page<PublisherListItem>([], 1, ConsultationSearch.HitCount, 0);
        _api.Answers["/catalog/publisher-collections"] = new Page<PublisherCollectionListItem>([], 1, ConsultationSearch.HitCount, 0);
        _api.Answers["/catalog/editions"] = new Page<EditionListItem>([], 1, ConsultationSearch.HitCount, 0);
        _api.Answers["/catalog/purchase-intents"] = new PurchaseIntentCatalogList(new Page<PurchaseIntentListItem>([], 1, ConsultationSearch.HitCount, 0), NoAlbumFilters);
        _api.Answers["/catalog/authors"] = new Page<AuthorCatalogListItem>([], 1, ConsultationSearch.HitCount, 0);
        _api.Answers["/catalog/genres"] = new Page<GenreListItem>([], 1, ConsultationSearch.HitCount, 0);
        _api.Answers["/catalog/universes"] = new Page<UniverseListItem>([], 1, ConsultationSearch.HitCount, 0);
    }

    // One record of each type, which its list finds for « ast ».
    private void AnswerEveryType()
    {
        _api.Answers["/catalog/series"] = new SeriesCatalogList(
            new Page<SeriesCatalogListItem>([new(Album.SeriesId!.Value, "Astérix", new SeriesOverview(1, []), 1)], 1, ConsultationSearch.HitCount, 1),
            NoSeriesFilters);
        _api.Answers["/catalog/albums"] = new AlbumCatalogList(
            new Page<AlbumCatalogListItem>(
                [new(Album, null, [new(Guid.CreateVersion7(), "Goscinny", "René", null)], 1965, null, null, AlbumCollectionState.InCollection)],
                1, ConsultationSearch.HitCount, 1),
            NoAlbumFilters);
        _api.Answers["/catalog/publishers"] = new Page<PublisherListItem>(
            [new(Edition.PublisherId, "Astéroïde", "https://asteroide.example")], 1, ConsultationSearch.HitCount, 1);
        _api.Answers["/catalog/publisher-collections"] = new Page<PublisherCollectionListItem>(
            [new(CollectionId, "Astéroïde", Edition.PublisherId, "Dargaud")], 1, ConsultationSearch.HitCount, 1);
        _api.Answers["/catalog/editions"] = new Page<EditionListItem>([new(Album, Edition, true)], 1, ConsultationSearch.HitCount, 1);
        _api.Answers["/catalog/purchase-intents"] = new PurchaseIntentCatalogList(
            new Page<PurchaseIntentListItem>([new(IntentId, Album, AlbumCollectionState.InCollection, null, null, null, DateTimeOffset.UnixEpoch)], 1, ConsultationSearch.HitCount, 1),
            NoAlbumFilters);
        _api.Answers["/catalog/authors"] = new Page<AuthorCatalogListItem>(
            [new(AuthorId, "Dupont", "Anne", "Astrid", null, 3, 1, [])], 1, ConsultationSearch.HitCount, 1);
        _api.Answers["/catalog/genres"] = new Page<GenreListItem>([new(GenreId, "Aventure astrale")], 1, ConsultationSearch.HitCount, 1);
        _api.Answers["/catalog/universes"] = new Page<UniverseListItem>([new(UniverseId, "Astres", Guid.CreateVersion7(), "Cosmos")], 1, ConsultationSearch.HitCount, 1);
    }

    private IRenderedComponent<ConsultationSearch> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.Search}?{criteria}");
        return Render<ConsultationSearch>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
