using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The search of a record to modify, from the header of the administration (visuel/administration/en-tete-et-menu.md): the
/// records found by the search of each list, in the single order of the types, each leading to its page of modification; told
/// apart by role, link and text, never by appearance.
/// </summary>
public sealed class AdminSearchTests : BunitContext, IDisposable
{
    private static readonly DateTimeOffset Modified = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

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
    private static readonly Guid SavedSearchId = Guid.Parse("01900000-0000-7000-8000-000000000009");
    private static readonly Guid IntentId = Guid.Parse("01900000-0000-7000-8000-00000000000a");

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminSearchTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
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
    public void Heading_TheTitleUnderTheAdministration_AndTheSearchOnItsText()
    {
        var page = Open("q=ast");

        Assert.Equal(["Administration", "Retrouver une fiche"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal("Retrouver une fiche", Text(page.Find("h1")));
        var form = page.Find("form.list-search");
        Assert.Equal(AppPaths.AdminSearch, form.GetAttribute("action"));
        var field = form.QuerySelector("input[type='search']")!;
        Assert.Equal(("q", "ast"), (field.GetAttribute("name"), field.GetAttribute("value")));
        // Its cross leads to the search without text (visuel/liste-albums/controles.md).
        Assert.Equal(AppPaths.AdminSearch, form.QuerySelector("a.search-clear")!.GetAttribute("href"));
    }

    [Fact]
    public void WithoutText_InvitesToSearch_WithoutReadingAnything()
    {
        var page = Open("q=%20");

        Assert.Equal("Saisissez le titre, le nom ou l'ISBN de la fiche à modifier.", Text(page.Find(".empty-list strong")));
        Assert.Empty(page.FindAll(".search-group"));
        Assert.DoesNotContain(_api.Requested, path => path.StartsWith("/admin/", StringComparison.Ordinal) && !path.StartsWith("/admin/record-counts", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryType_ReadByTheSearchOfItsList_WithItsFirstRecordsOnly()
    {
        Open("q=ast");

        string[] expected =
        [
            "/admin/series?q=ast&page=1&pageSize=5", "/admin/albums?q=ast&page=1&pageSize=5", "/admin/publishers?q=ast&page=1&pageSize=5",
            "/catalog/publisher-collections?q=ast&pageSize=5", "/admin/editions?q=ast&page=1&pageSize=5",
            "/admin/purchase-intents?q=ast&page=1&pageSize=5", "/admin/authors?q=ast&page=1&pageSize=5", "/admin/genres?q=ast&page=1&pageSize=5",
            "/admin/universes?q=ast&page=1&pageSize=5", "/catalog/saved-searches?q=ast&pageSize=5",
        ];
        Assert.All(expected, path => Assert.Contains(path, _api.Requested));
    }

    [Fact]
    public void Groups_InTheSingleOrderOfTheTypes_EachRecordWithItsDetail()
    {
        AnswerEveryType();

        var page = Open("q=ast");
        var groups = page.FindAll(".search-group").Select(group => (
            Text(group.QuerySelector("h2")!),
            group.QuerySelectorAll(".search-hit").Select(hit => (
                Text(hit.QuerySelector(".row-entry")!),
                hit.QuerySelector(".row-entry-detail") is { } detail ? Text(detail) : null)).ToArray()));

        // Series, albums, publishers, collections, editions, intents, authors, genres, universes, saved searches
        // (fonctionnel/presentation/ordre-des-types-de-fiche.md); each named as its list names it, with its detail under it.
        Assert.Equal(
            [
                ("Séries", new[] { ("Astérix", (string?)null) }),
                ("Albums", [("Le Tour de Gaule (Astérix - T. 5)", "René Goscinny · Albert Uderzo")]),
                ("Éditeurs", [("Astéroïde", "Petits formats")]),
                ("Collections", [("Astéroïde", "Dargaud")]),
                ("Éditions", [("Dargaud [1965] - ISBN 9782012101340", "Le Tour de Gaule (Astérix - T. 5)")]),
                ("Intentions d'acquisition", [("Le Tour de Gaule (Astérix - T. 5) · Dargaud [1965] - ISBN 9782012101340", null)]),
                ("Auteurs", [("Astrid", "Anne Dupont")]),
                ("Genres", [("Aventure astrale", null)]),
                ("Univers", [("Astres", "Cosmos › Galaxies")]),
                ("Recherches enregistrées", [("Astérix à compléter", null)]),
            ],
            groups.Select(group => (group.Item1, group.Item2)));
    }

    [Fact]
    public void EachRecord_LeadsToItsPageOfModification_WhateverItsType()
    {
        AnswerEveryType();

        var links = Open("q=ast").FindAll(".search-hit a.button").Select(link => (link.GetAttribute("href"), link.GetAttribute("aria-label")));

        // « Modifier » alone opens the record, its entry being no link (visuel/listes/principes-communs.md).
        Assert.Equal(
            [
                (AppPaths.AdminEditSeries(Album.SeriesId!.Value), "Modifier Astérix"),
                (AppPaths.AdminEditAlbum(Album.Id), "Modifier Le Tour de Gaule (Astérix - T. 5)"),
                (AppPaths.AdminEditPublisher(Edition.PublisherId), "Modifier Astéroïde"),
                // A collection, entered with its publisher.
                (AppPaths.AdminEditPublisher(Edition.PublisherId), "Modifier Astéroïde"),
                (AppPaths.AdminEditEdition(Album.Id, Edition.Id), "Modifier Dargaud [1965] - ISBN 9782012101340"),
                // An intent, on the page of the editions and intents of its album.
                (AppPaths.AdminAlbumEditions(Album.Id), "Modifier Le Tour de Gaule (Astérix - T. 5) · Dargaud [1965] - ISBN 9782012101340"),
                (AppPaths.AdminEditAuthor(AuthorId), "Modifier Astrid"),
                (AppPaths.AdminEditGenre(GenreId), "Modifier Aventure astrale"),
                (AppPaths.AdminEditUniverse(UniverseId), "Modifier Astres"),
                (AppPaths.AdminSavedSearches, "Modifier Astérix à compléter"),
            ],
            links);
        Assert.All(Open("q=ast").FindAll(".search-hit .row-entry"), entry => Assert.Null(entry.QuerySelector("a")));
    }

    [Fact]
    public void MoreRecordsThanPresented_LeadToTheListSearchedOnTheSameText()
    {
        var series = Enumerable.Range(1, AdminSearch.HitCount)
            .Select(i => new SeriesAdminListItem(Guid.CreateVersion7(), $"Astérix {i}", null, new SeriesOverview(0, []), 0, false, Modified, 1))
            .ToList();
        _api.Answers["/admin/series"] = new SeriesAdminList(new Page<SeriesAdminListItem>(series, 1, AdminSearch.HitCount, 12), NoSeriesFilters);

        var group = Open("q=ast").Find(".search-group");

        Assert.Equal(AdminSearch.HitCount, group.QuerySelectorAll(".search-hit").Length);
        var more = group.QuerySelector(".search-more a")!;
        Assert.Equal(("Voir les 12 séries", $"{AppPaths.AdminSeries}?q=ast"), (Text(more), more.GetAttribute("href")));
    }

    [Fact]
    public void MoreRecordsThanPresented_OfATypeWithoutList_AreCountedOnly()
    {
        var collections = Enumerable.Range(1, AdminSearch.HitCount)
            .Select(i => new PublisherCollectionListItem(Guid.CreateVersion7(), $"Astéroïde {i}", Edition.PublisherId, "Dargaud"))
            .ToList();
        _api.Answers["/catalog/publisher-collections"] = new Page<PublisherCollectionListItem>(collections, 1, AdminSearch.HitCount, 6);

        var group = Open("q=ast").Find(".search-group");

        Assert.Equal("et 1 collection de plus", Text(group.QuerySelector(".search-more")!));
        Assert.Null(group.QuerySelector(".search-more a"));
    }

    [Fact]
    public void AllPresented_LeadsNowhereElse()
    {
        AnswerEveryType();

        Assert.Empty(Open("q=ast").FindAll(".search-more"));
    }

    [Fact]
    public void NothingFound_SaysSo_AndRecallsWhatTheSearchLooksInto()
    {
        var page = Open("q=zzz");

        var empty = page.Find(".empty-list");
        Assert.Equal("Aucune fiche ne correspond à « zzz ».", Text(empty.QuerySelector("strong")!));
        Assert.StartsWith("La recherche porte sur ce qui nomme chaque fiche dans sa liste", Text(empty.QuerySelector("span")!));
        Assert.EndsWith("sans tenir compte des majuscules ni des accents.", Text(empty.QuerySelector("span")!));
        Assert.Empty(page.FindAll(".search-group"));
    }

    [Fact]
    public void AnErrorOfTheApi_IsPresentedInsteadOfTheRecords()
    {
        AnswerEveryType();
        _api.Failures["/admin/albums"] = HttpStatusCode.InternalServerError;

        var page = Open("q=ast");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".search-group"));
        Assert.Empty(page.FindAll(".empty-list"));
    }

    [Fact]
    public void Audience_TheSearchWithItsText_ThenTheScreen()
    {
        Open("q=Ast%C3%A9rix%20le", recorded: true);

        Assert.Equal(["admin.search.query", "admin.search"], _audience.Functions);
        Assert.Contains(_audience.Sent, sent => sent.Content.GetRawText().Contains("q=Ast%C3%A9rix%20le", StringComparison.Ordinal));
    }

    [Fact]
    public void Audience_WithoutText_TheScreenOnly()
    {
        Open("", recorded: true);

        Assert.Equal(["admin.search"], _audience.Functions);
    }

    private static readonly SeriesListFilters NoSeriesFilters = new(null, null, null, null, null);

    private void AnswerNothing()
    {
        _api.Answers["/admin/series"] = new SeriesAdminList(new Page<SeriesAdminListItem>([], 1, AdminSearch.HitCount, 0), NoSeriesFilters);
        _api.Answers["/admin/albums"] = new AlbumAdminList(new Page<AlbumAdminListItem>([], 1, AdminSearch.HitCount, 0), new AlbumListFilters(null, null, null, null, null, null));
        _api.Answers["/admin/publishers"] = new Page<PublisherAdminListItem>([], 1, AdminSearch.HitCount, 0);
        _api.Answers["/catalog/publisher-collections"] = new Page<PublisherCollectionListItem>([], 1, AdminSearch.HitCount, 0);
        _api.Answers["/admin/editions"] = new EditionAdminList(new Page<EditionAdminListItem>([], 1, AdminSearch.HitCount, 0), new EditionListFilters(null, null, null, null));
        _api.Answers["/admin/purchase-intents"] = new Page<PurchaseIntentAdminListItem>([], 1, AdminSearch.HitCount, 0);
        _api.Answers["/admin/authors"] = new Page<AuthorAdminListItem>([], 1, AdminSearch.HitCount, 0);
        _api.Answers["/admin/genres"] = new Page<GenreAdminListItem>([], 1, AdminSearch.HitCount, 0);
        _api.Answers["/admin/universes"] = new Page<UniverseAdminListItem>([], 1, AdminSearch.HitCount, 0);
        _api.Answers["/catalog/saved-searches"] = new Page<SavedSearchListItem>([], 1, AdminSearch.HitCount, 0);
    }

    // One record of each type, which its list finds for « ast ».
    private void AnswerEveryType()
    {
        _api.Answers["/admin/series"] = new SeriesAdminList(
            new Page<SeriesAdminListItem>([new(Album.SeriesId!.Value, "Astérix", null, new SeriesOverview(0, []), 0, false, Modified, 1)], 1, AdminSearch.HitCount, 1),
            NoSeriesFilters);
        _api.Answers["/admin/albums"] = new AlbumAdminList(
            new Page<AlbumAdminListItem>(
                [new(Album, null, [new(Guid.CreateVersion7(), "Goscinny", "René", null), new(Guid.CreateVersion7(), "Uderzo", "Albert", null)], 1965, null, 1, 1, AlbumCollectionState.InCollection, Modified, 1)],
                1, AdminSearch.HitCount, 1),
            new AlbumListFilters(null, null, null, null, null, null));
        _api.Answers["/admin/publishers"] = new Page<PublisherAdminListItem>(
            [new(Edition.PublisherId, "Astéroïde", null, ["Petits formats"], 0, 0, Modified, 1)], 1, AdminSearch.HitCount, 1);
        _api.Answers["/catalog/publisher-collections"] = new Page<PublisherCollectionListItem>(
            [new(CollectionId, "Astéroïde", Edition.PublisherId, "Dargaud")], 1, AdminSearch.HitCount, 1);
        _api.Answers["/admin/editions"] = new EditionAdminList(
            new Page<EditionAdminListItem>([new(Album, Edition, null, null, AcquisitionMode.Purchase, 2001, Modified, 1)], 1, AdminSearch.HitCount, 1),
            new EditionListFilters(null, null, null, null));
        _api.Answers["/admin/purchase-intents"] = new Page<PurchaseIntentAdminListItem>(
            [new(IntentId, Album, AlbumCollectionState.PurchaseIntent, Edition, null, false, Modified, 1)], 1, AdminSearch.HitCount, 1);
        _api.Answers["/admin/authors"] = new Page<AuthorAdminListItem>(
            [new(AuthorId, "Dupont", "Anne", "Astrid", null, 3, [], Modified, 1)], 1, AdminSearch.HitCount, 1);
        _api.Answers["/admin/genres"] = new Page<GenreAdminListItem>([new(GenreId, "Aventure astrale", 0, 0, Modified, 1)], 1, AdminSearch.HitCount, 1);
        _api.Answers["/admin/universes"] = new Page<UniverseAdminListItem>(
            [new(UniverseId, "Astres", Guid.CreateVersion7(), [new(Guid.CreateVersion7(), "Cosmos", null, null), new(Guid.CreateVersion7(), "Galaxies", null, "Cosmos")], 0, 0, 0, Modified, 1)],
            1, AdminSearch.HitCount, 1);
        _api.Answers["/catalog/saved-searches"] = new Page<SavedSearchListItem>(
            [new(SavedSearchId, "Astérix à compléter", EntityKind.Album)], 1, AdminSearch.HitCount, 1);
    }

    private IRenderedComponent<AdminSearch> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminSearch}?{criteria}");
        return Render<AdminSearch>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
