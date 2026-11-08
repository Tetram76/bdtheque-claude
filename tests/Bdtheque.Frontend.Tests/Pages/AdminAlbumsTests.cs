using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the albums of the administration (visuel/liste-albums/contenu.md): its rows, its count, its filters, its
/// dialogs and its errors, told apart by role, link and text, never by appearance.
/// </summary>
public sealed class AdminAlbumsTests : BunitContext, IDisposable
{
    private static readonly Guid Series = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid Genre = Guid.Parse("01900000-0000-7000-8000-0000000000a2");

    private static readonly AlbumAdminListItem Bijoux = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000001"), "Les Bijoux de la Castafiore", Series, "Tintin",
            AlbumType.Regular, false, 21, null, null, true),
        new AlbumCover(Guid.Parse("01900000-0000-7000-8000-0000000000c1")),
        [new AuthorListItem(Guid.NewGuid(), "Remi", "Georges", "Hergé")],
        1963, null, 2, 1, AlbumCollectionState.InCollection, new DateTimeOffset(2026, 10, 12, 22, 30, 0, TimeSpan.Zero), 41);

    private static readonly AlbumAdminListItem Blacksad = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000002"), null, Guid.NewGuid(), "Blacksad",
            AlbumType.Omnibus, false, null, 1, 5, false),
        null,
        [new AuthorListItem(Guid.NewGuid(), "Díaz Canales", "Juan", null), new AuthorListItem(Guid.NewGuid(), "Guarnido", "Juanjo", null)],
        2023, 10, 0, 1, AlbumCollectionState.PurchaseIntent, new DateTimeOffset(2026, 10, 8, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly AlbumAdminListItem Bleu = new(
        new AlbumSummary(Guid.Parse("01900000-0000-7000-8000-000000000003"), "Le Bleu est une couleur chaude", null, null,
            AlbumType.Regular, false, null, null, null, false),
        null, [], null, null, 0, 0, AlbumCollectionState.OutsideCollection, new DateTimeOffset(2026, 5, 14, 8, 0, 0, TimeSpan.Zero), 43);

    private static readonly AlbumListFilters NoFilters = new(null, null, null, null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminAlbumsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Bijoux, Blacksad, Bleu], totalCount: 143);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfAlbumsAndTheCreation()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Albums"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Albums", "2 847"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Equal(AppPaths.AdminNewAlbum, page.Find(".list-heading a.button").GetAttribute("href"));
    }

    [Fact]
    public void Rows_TheAlbumItsAuthorsItsPublicationItsEditionsAndWhereItStands()
    {
        var rows = Open("").FindAll(".album-row:not(.album-row-head)").Select(row => (
            Text(row.QuerySelector(".album-label")!),
            Text(row.QuerySelector(".album-authors")!),
            Text(row.QuerySelector(".album-date")!),
            Text(row.QuerySelector(".album-editions")!),
            Text(row.QuerySelector(".album-state")!)));

        Assert.Equal(
            [
                ("Les Bijoux de la Castafiore (Tintin - T. 21)", "Hergé", "1963", "2 éditions · 1 intention", "Dans la collection"),
                ("Blacksad - INT. [1 à 5]", "Juan Díaz Canales · Juanjo Guarnido", "octobre 2023", "Aucune édition · 1 intention", "Intention d'achat"),
                ("Le Bleu est une couleur chaude", "", "Non renseignée", "Aucune édition", "Hors collection"),
            ],
            rows);
    }

    [Fact]
    public void Rows_LeadToTheEditionTheEditionsAndTheConsultation_AndOpenTheirDialogsInTheList()
    {
        var row = Open("q=bijoux").FindAll(".album-row:not(.album-row-head)")[0];
        var id = Bijoux.Album.Id;

        Assert.Equal(AppPaths.AdminEditAlbum(id), row.QuerySelector(".album-label")!.GetAttribute("href"));
        Assert.Equal(AppPaths.AdminAlbumEditions(id), row.QuerySelector(".album-editions a")!.GetAttribute("href"));
        Assert.Equal("Modifier Les Bijoux de la Castafiore (Tintin - T. 21)", row.QuerySelector(".album-actions a.button")!.GetAttribute("aria-label"));
        Assert.Equal(
            [
                ("Éditions et intentions d'achat", AppPaths.AdminAlbumEditions(id)),
                ("Voir dans la consultation", AppPaths.Album(id)),
                ("Fusionner avec un autre album…", $"{AppPaths.AdminAlbums}?q=bijoux&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminAlbums}?q=bijoux&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Theory]
    [InlineData("", "143 albums", "")]
    [InlineData("entry=B", "143 albums sous la lettre B", "")]
    [InlineData("entry=%23", "143 albums sous « # », commençant par un chiffre", "")]
    [InlineData("q=tintin", "143 albums sur 2 847", "")]
    public void Summary_TheCountUnderTheEntryOrOutOfAllTheAlbums_AndTheOrder(string criteria, string count, string _)
    {
        var summary = Open(criteria).Find(".admin-list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Triés par titre, la série tenant lieu de titre aux albums qui n'en ont pas", Text(summary.Children[1]));
    }

    [Fact]
    public void Search_AFormThatLeadsToTheListWithoutItsEntry_KeepingItsFilters()
    {
        var form = Open($"q=tintin&entry=T&genreId={Genre}&state=InCollection").Find("form.list-search");

        Assert.Equal(("get", AppPaths.AdminAlbums), (form.GetAttribute("method"), form.GetAttribute("action")));
        Assert.Equal("tintin", form.QuerySelector("input[name='q']")!.GetAttribute("value"));
        Assert.Equal(
            [("genreId", Genre.ToString()), ("state", "InCollection")],
            form.QuerySelectorAll("input[type='hidden']").Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
        Assert.Null(form.QuerySelector("input[name='entry']"));
        var filters = form.QuerySelector("button[aria-controls]")!;
        Assert.Equal(("false", "open", "2"), (filters.GetAttribute("aria-expanded"), filters.GetAttribute("value"), Text(filters.QuerySelector(".list-search-count")!)));
    }

    [Fact]
    public void Filters_UnfoldedAPanelOfTheirFields_TheRecordsChosenByTypingAndTheStateInAList()
    {
        _api.Answers["/admin/albums"] = new AlbumAdminList(
            Page([Bijoux], 1), NoFilters with { Genre = new GenreListItem(Genre, "Aventure") });

        var form = Open($"filters=open&genreId={Genre}&state=PurchaseIntent").Find("form.list-search");

        Assert.Equal("true", form.QuerySelector("button[aria-controls]")!.GetAttribute("aria-expanded"));
        Assert.Equal(
            ["Série", "Auteur", "Éditeur", "Collection éditeur", "Genre", "Univers", "Collection"],
            form.QuerySelectorAll(".album-filters label").Select(Text));
        Assert.Equal(
            ["seriesId", "authorId", "publisherId", "publisherCollectionId", "genreId", "universeId"],
            form.QuerySelectorAll(".album-filters input[type='hidden']").Select(input => input.GetAttribute("name")));
        Assert.Equal(Genre.ToString(), form.QuerySelector(".album-filters input[name='genreId']")!.GetAttribute("value"));
        Assert.Equal(
            [("", "Tous les albums"), ("InCollection", "Dans la collection"), ("PurchaseIntent", "Intention d'achat"), ("OutsideCollection", "Hors collection")],
            form.QuerySelectorAll("select[name='state'] option").Select(option => (option.GetAttribute("value"), Text(option))));
        Assert.Equal("PurchaseIntent", form.QuerySelector("select[name='state'] option[selected]")!.GetAttribute("value"));
        Assert.Equal("Filtrer", Text(form.QuerySelector(".album-filters-foot button[type='submit']")!));
    }

    [Fact]
    public void ActiveFilters_TagsThatLeadToTheListWithoutThem_ThenAllCleared()
    {
        _api.Answers["/admin/albums"] = new AlbumAdminList(
            Page([Bijoux], 1), NoFilters with { Genre = new GenreListItem(Genre, "Aventure") });

        var filters = Open($"q=tintin&genreId={Genre}&state=InCollection&page=2").Find(".active-filters");

        Assert.Equal(
            [
                ("Genre : Aventure", $"{AppPaths.AdminAlbums}?q=tintin&state=InCollection"),
                ("Collection : Dans la collection", $"{AppPaths.AdminAlbums}?q=tintin&genreId={Genre}"),
            ],
            filters.QuerySelectorAll(".tag").Select(tag => (Text(tag.QuerySelector("span")!), tag.QuerySelector("a")!.GetAttribute("href"))));
        Assert.Equal(("Tout effacer", $"{AppPaths.AdminAlbums}?q=tintin"), (Text(filters.Children[^1]), filters.Children[^1].GetAttribute("href")));
    }

    [Fact]
    public void Navigation_TheEntriesKeepTheSearchAndTheFilters_ThePagesKeepTheEntry()
    {
        var page = Open($"q=tintin&genreId={Genre}&entry=B");

        Assert.Equal(
            $"{AppPaths.AdminAlbums}?q=tintin&entry=C&genreId={Genre}",
            page.Find(".initial-navigation a[href*='entry=C']").GetAttribute("href"));
        Assert.Equal(
            $"{AppPaths.AdminAlbums}?q=tintin&entry=B&genreId={Genre}&page=2",
            page.Find(".pagination a[rel='next']").GetAttribute("href"));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearItOrToCreateTheAlbum()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=Zorglub").Find(".empty-list");

        Assert.Equal("Aucun album ne correspond à « Zorglub ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal(
            [("Effacer la recherche", AppPaths.AdminAlbums), ("Créer l'album « Zorglub »", AppPaths.AdminNewAlbum)],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Deletion_ADialogThatAnnouncesTheImpact_AndConfirmsWithTheVersionAndTheFingerprint()
    {
        var id = Bijoux.Album.Id;
        _api.Answers[$"/admin/albums/{id}/deletion-impact"] = new DeletionImpact(
            [], [new ImpactCount(EntityKind.Genre, 2)], [new ImpactCount(EntityKind.Edition, 2), new ImpactCount(EntityKind.EditionVisual, 9)], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Supprimer cet album ?", "Les Bijoux de la Castafiore (Tintin - T. 21)"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Contains("2 éditions et 9 visuels", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Equal(AppPaths.AdminAlbums, dialog.QuerySelector("[data-dialog-close]")!.GetAttribute("href"));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "41"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
        Assert.Equal("Supprimer définitivement", Text(form.QuerySelector("button[type='submit']")!));
    }

    [Fact]
    public void Deletion_Refused_SaysWhatBlocksItAndWhatToDo_WithoutAnyOtherButtonThanClose()
    {
        var id = Bleu.Album.Id;
        _api.Answers[$"/admin/albums/{id}/deletion-impact"] = new DeletionImpact([new ImpactCount(EntityKind.SavedSearch, 1)], [], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Contains("1 recherche enregistrée", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Contains("Retirez d'abord l'album des critères de ces recherches enregistrées, puis recommencez.", Text(dialog));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherAlbum_AndContinuesWithTheOneChosen()
    {
        var page = Open($"merge={Bijoux.Album.Id}&mergeQ=bl");
        var dialog = page.Find("dialog");

        Assert.Equal("Fusionner deux albums", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [
                ("Blacksad - INT. [1 à 5]", "Juan Díaz Canales · Juanjo Guarnido · octobre 2023 · Aucune édition · 1 intention"),
                ("Le Bleu est une couleur chaude", "Aucune édition"),
            ],
            dialog.QuerySelectorAll(".album-merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        Assert.Equal(Blacksad.Album.Id.ToString(), dialog.QuerySelector("input[name='other'][checked]")!.GetAttribute("value"));
        var next = dialog.QuerySelector("#album-merge-continue")!;
        Assert.Equal(AppPaths.AdminMergeAlbums, next.GetAttribute("action"));
        Assert.Equal(Bijoux.Album.Id.ToString(), next.QuerySelector("input[name='album']")!.GetAttribute("value"));
        Assert.Contains(_api.Requested, path => path == "/admin/albums?q=bl&pageSize=11");
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/albums"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".album-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("q=tintin&entry=T", recorded: true);

        // The action of the list first, which displays the screen.
        Assert.Equal(["admin.albums.initial", "admin.albums"], _audience.Functions);
    }

    private IRenderedComponent<AdminAlbums> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminAlbums}?{criteria}");
        return Render<AdminAlbums>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<AlbumAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/albums"] = new AlbumAdminList(Page(items, totalCount), NoFilters);

    private static Page<AlbumAdminListItem> Page(IReadOnlyList<AlbumAdminListItem> items, int totalCount) =>
        new(items, 1, AlbumListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
