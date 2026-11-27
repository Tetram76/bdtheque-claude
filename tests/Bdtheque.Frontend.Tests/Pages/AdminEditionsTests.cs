using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Lists;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Components.Presentation;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the editions of the administration (visuel/listes/liste-par-liste.md): its rows, its count, its filters, its
/// dialogs and its errors, told apart by role, link and text, never by appearance. What it shares with the list of the albums
/// (search, active filters, empty list, page beyond the last) is pinned by AdminAlbumsTests.
/// </summary>
public sealed class AdminEditionsTests : BunitContext, IDisposable
{
    private static readonly Guid PublisherId = Guid.Parse("01900000-0000-7000-8000-0000000000b1");
    private static readonly AlbumCover Cover = new(Guid.Parse("01900000-0000-7000-8000-0000000000c1"), TestCovers.Palette);

    private static readonly AlbumSummary Lunes = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a1"), "...Et après", Guid.NewGuid(), "Les Lunes de Saturne", AlbumType.Regular, false, 1,
        null, null, true);

    private static readonly AlbumSummary Longway = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a2"), null, Guid.NewGuid(), "Buddy Longway", AlbumType.Regular, false, 2, null, null, true);

    private static readonly EditionAdminListItem Glenat = new(
        Lunes, new EditionSummary(Guid.Parse("01900000-0000-7000-8000-000000000001"), PublisherId, "Glénat", null, null, 2004, "978-2-7234-4567-1"),
        Cover, null, AcquisitionMode.Purchase, 2004, new DateTimeOffset(2026, 1, 12, 8, 0, 0, TimeSpan.Zero), 41);

    private static readonly EditionAdminListItem Dupuis = new(
        Longway, new EditionSummary(Guid.Parse("01900000-0000-7000-8000-000000000002"), PublisherId, "Dupuis", Guid.NewGuid(), "Repérages", null, null),
        null, EditionCategory.FirstEdition, AcquisitionMode.Trade, null, new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly EditionListFilters NoFilters = new(null, null, null, null);

    private const string EnteredFromItsAlbum = "Une édition se saisit depuis la page des éditions et intentions d'acquisition de son album.";

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminEditionsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<Components.Controls.RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Glenat, Dupuis], totalCount: 2913);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    // An edition is entered from its album, as a purchase intent: the heading says where in place of a creation
    // (visuel/listes/principes-communs.md, création).
    public void Heading_TheTitleTheNumberOfEditions_AndWhereAnEditionIsEntered_InPlaceOfACreation()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Éditions"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Éditions", "2 913"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Empty(page.FindAll(".list-heading a.button"));
        Assert.Equal(EnteredFromItsAlbum, Text(page.Find(".list-heading-note")));
    }

    [Fact]
    // A missing data is a dash, followed by the mention a screen reader reads in its place.
    public void Rows_TheEditionItsAlbumItsYearItsCategoryAndItsAcquisition()
    {
        var rows = Open("").FindAll(".edition-row:not(.edition-row-head)").Select(row => (
            Text(row.QuerySelector(".row-entry")!),
            Text(row.QuerySelector(".row-entry-detail")!),
            Text(row.QuerySelector(".edition-year")!),
            Text(row.QuerySelector(".edition-category")!),
            Text(row.QuerySelector(".edition-acquisition")!)));

        Assert.Equal(
            [
                ("Glénat [2004] - ISBN 978-2-7234-4567-1", "...Et après (Les Lunes de Saturne - T. 1)", "Année 2004", "—Non renseignée", "Achat, 2004"),
                ("Dupuis (Repérages)", "Buddy Longway - T. 2", "Année —Non renseignée", "Édition originale", "Échange"),
            ],
            rows);
    }

    [Fact]
    public void Rows_TheCoverOfTheEdition_OrTheGenericVisual()
    {
        var covers = Open("").FindAll(".edition-row:not(.edition-row-head) .edition-cover .cover");

        Assert.Equal([VisualMediaRoutes.Display(Cover.VisualId), null], covers.Select(cover => cover.GetAttribute("src")));
    }

    [Fact]
    public void Rows_LeadToTheEntryFormAndToTheAlbumInTheConsultation_AndOpenTheirDialogsInTheList()
    {
        var row = Open("q=978").FindAll(".edition-row:not(.edition-row-head)")[0];
        var id = Glenat.Edition.Id;

        // « Modifier » alone opens the record: the entry of the row is no link (visuel/liste-albums/contenu.md, Actions d'une ligne).
        Assert.Equal(["Modifier Glénat [2004] - ISBN 978-2-7234-4567-1"], row.QuerySelectorAll($"a[href='{AppPaths.AdminEditEdition(Lunes.Id, id)}']").Select(link => Normalized(link.GetAttribute("aria-label")!)));
        // « Modifier », then the editions and purchase intents of its album, as a row of the list of the albums
        // (visuel/listes/principes-communs.md, Actions): an edition is entered from its album.
        Assert.Equal(
            [
                ("Modifier Glénat [2004] - ISBN 978-2-7234-4567-1", AppPaths.AdminEditEdition(Lunes.Id, id)),
                ("Éditions et intentions d'acquisition de ...Et après (Les Lunes de Saturne - T. 1)", AppPaths.AdminAlbumEditions(Lunes.Id)),
            ],
            row.QuerySelectorAll(".edition-actions > a.button").Select(button => (Normalized(button.GetAttribute("aria-label")!), button.GetAttribute("href"))));
        // On a smartphone, the editions and purchase intents of the album lead the menu in place of their button, which the
        // styles of the page show at one width or the other.
        Assert.Equal(
            [
                ("Éditions et intentions d'acquisition de l'album", AppPaths.AdminAlbumEditions(Lunes.Id)),
                ("Voir l'album dans la consultation", AppPaths.Album(Lunes.Id)),
                ("Fusionner avec une autre édition…", $"{AppPaths.AdminEditions}?q=978&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminEditions}?q=978&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Theory]
    [InlineData("", "2 913 éditions")]
    [InlineData("entry=B", "2 913 éditions sous la lettre B")]
    [InlineData("q=978", "2 913 éditions sur 2 913")]
    public void Summary_TheCountOutOfAllTheEditions_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Dans l'ordre des albums, puis par année d'édition", Text(summary.Children[1]));
    }

    [Fact]
    public void Filters_UnfoldedAPanelOfTheirFields_TheRecordsChosenByTyping()
    {
        _api.Answers["/admin/editions"] = new EditionAdminList(Page([Glenat], 1), NoFilters with { Album = Lunes });

        var form = Open($"filters=open&albumId={Lunes.Id}").Find("form.list-search");

        Assert.Equal("true", form.QuerySelector("button[aria-controls]")!.GetAttribute("aria-expanded"));
        Assert.Equal(["Série", "Album", "Éditeur", "Collection"], form.QuerySelectorAll(".list-filters label").Select(Text));
        Assert.Equal(
            ["seriesId", "albumId", "publisherId", "publisherCollectionId"],
            form.QuerySelectorAll(".list-filters input[type='hidden']").Select(input => input.GetAttribute("name")));
        Assert.Equal(Lunes.Id.ToString(), form.QuerySelector(".list-filters input[name='albumId']")!.GetAttribute("value"));
        // The album chosen is named by its label, as in the rows.
        Assert.Equal("...Et après (Les Lunes de Saturne - T. 1)", form.QuerySelector("#filter-album")!.GetAttribute("value"));
    }

    [Fact]
    public void ActiveFilters_TagsThatLeadToTheListWithoutThem()
    {
        var seriesId = Lunes.SeriesId!.Value;
        _api.Answers["/admin/editions"] = new EditionAdminList(
            Page([Glenat], 1),
            NoFilters with
            {
                Series = new SeriesListItem(seriesId, "Les Lunes de Saturne"), Album = Lunes, Publisher = new PublisherListItem(PublisherId, "Glénat", null),
            });

        var filters = Open($"q=978&seriesId={seriesId}&albumId={Lunes.Id}&publisherId={PublisherId}").Find(".active-filters");

        Assert.Equal(
            [
                ("Série : Les Lunes de Saturne", $"{AppPaths.AdminEditions}?q=978&albumId={Lunes.Id}&publisherId={PublisherId}"),
                ("Album : ...Et après (Les Lunes de Saturne - T. 1)", $"{AppPaths.AdminEditions}?q=978&seriesId={seriesId}&publisherId={PublisherId}"),
                ("Éditeur : Glénat", $"{AppPaths.AdminEditions}?q=978&seriesId={seriesId}&albumId={Lunes.Id}"),
            ],
            filters.QuerySelectorAll(".tag").Select(tag => (Text(tag.QuerySelector("span")!), tag.QuerySelector("a")!.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearIt_WithoutCreation()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=978-0").Find(".empty-list");

        Assert.Equal("Aucune édition ne correspond à « 978-0 ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal("La recherche porte sur l'ISBN, avec ou sans tirets ni espaces.", Text(empty.QuerySelector("strong + span")!));
        Assert.Equal(
            [("Effacer la recherche", AppPaths.AdminEditions)],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_WithoutAnyCriterion_SaysThereIsNoEditionYet_AndWhereAnEditionIsEntered_WithNothingToClearNorCreate()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucune édition n'est encore enregistrée.", Normalized(empty.QuerySelector("strong")!.TextContent));
        Assert.Equal(EnteredFromItsAlbum, Text(empty.QuerySelector("span")!));
        Assert.Empty(empty.QuerySelectorAll("a"));
    }

    [Fact]
    public void Deletion_ADialogThatAnnouncesTheImpactOnTheAlbum_AndConfirmsWithTheVersionOfTheAlbum()
    {
        var id = Glenat.Edition.Id;
        _api.Answers[$"/admin/albums/{Lunes.Id}/editions/{id}/deletion-impact"] = new DeletionImpact(
            [], [], [new ImpactCount(EntityKind.EditionVisual, 2)], [new ImpactCount(EntityKind.Album, 1)], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Supprimer cette édition ?", "Glénat [2004] - ISBN 978-2-7234-4567-1"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Equal("La suppression est définitive.", Text(dialog.QuerySelector(".dialog-body > p")!));
        // The last owned edition of its album: the album leaves the collection (fonctionnel/regles-metier/suppression-des-entites.md).
        Assert.Equal(["Suppression aussi de 2 visuels", "Sortie de la collection de 1 album"],
            dialog.QuerySelectorAll(".deletion-impact li").Select(Text));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "41"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
    }

    [Fact]
    public void Deletion_RefusedWhileASavedSearchDesignatesTheEdition_SaysWhatToDo()
    {
        var id = Dupuis.Edition.Id;
        _api.Answers[$"/admin/albums/{Longway.Id}/editions/{id}/deletion-impact"] = new DeletionImpact(
            [new ImpactCount(EntityKind.SavedSearch, 1)], [], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal("L'édition est encore désignée par :", Text(dialog.QuerySelector(".dialog-body > p")!));
        Assert.Equal(["1 recherche enregistrée : retirez l'édition de ses critères."], dialog.QuerySelectorAll(".deletion-refusal li").Select(Text));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    // fonctionnel/regles-metier/fusion-des-fiches.md, fiches pressenties: before any search, the dialog proposes the records the
    // API finds resembling the record, as the dialog of every list does (pinned by AdminGenresTests).
    [Fact]
    public void Merge_BeforeAnySearch_ProposesTheRecordsThatResembleIt()
    {
        var path = $"/admin/editions?resembling={Glenat.Edition.Id}&pageSize={MergeDialog.SuggestionCount}";
        _api.Answers[path] = new EditionAdminList(Page([Dupuis], 1), NoFilters);

        var dialog = Open($"merge={Glenat.Edition.Id}").Find("dialog");

        Assert.Equal("Éditions pressenties", Text(dialog.QuerySelector(".merge-suggestions")!));
        Assert.Equal(["Dupuis (Repérages)"], dialog.QuerySelectorAll(".merge-candidate strong").Select(Text));
        Assert.Contains("Ou recherchez l'autre édition par son ISBN ou par le titre de son album.", dialog.QuerySelectorAll(".dialog-note").Select(Text));
        Assert.Contains(_api.Requested, requested => requested == path);
        Assert.Equal("true", dialog.QuerySelector("#edition-merge-continue input[name='suggested']")!.GetAttribute("value"));
    }
    [Fact]
    public void Merge_ADialogThatSearchesTheOtherEditionByItsIsbnOrItsAlbum_AndContinuesWithTheOneChosen()
    {
        var dialog = Open($"merge={Glenat.Edition.Id}&mergeQ=978").Find("dialog");

        Assert.Equal("Fusionner deux éditions", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [("Dupuis (Repérages)", "Buddy Longway - T. 2 · Échange")],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        var next = dialog.QuerySelector("#edition-merge-continue")!;
        Assert.Equal(AppPaths.AdminMergeEditions, next.GetAttribute("action"));
        Assert.Equal(Glenat.Edition.Id.ToString(), next.QuerySelector("input[name='edition']")!.GetAttribute("value"));
        // By its ISBN or the title of its album or series: an edition without ISBN is found too.
        Assert.Equal("ISBN ou titre de l'album…", dialog.QuerySelector("input[type=search]")!.GetAttribute("placeholder"));
        Assert.Contains(_api.Requested, path => path == "/admin/editions?q=978&pageSize=11&byAlbum=true");
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/editions"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".edition-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open($"albumId={Lunes.Id}", recorded: true);

        // The action of the list first, which displays the screen.
        Assert.Equal(["admin.editions.filter", "admin.editions"], _audience.Functions);
    }

    [Fact]
    public void Navigation_ByInitial_KeepsTheSearch_ReturnsToTheFirstPage_AndMarksTheCurrentEntry()
    {
        var page = Open("q=978&entry=B&page=2");

        Assert.Equal($"{AppPaths.AdminEditions}?q=978&entry=C", page.Find(".initial-navigation a[href*='entry=C']").GetAttribute("href"));
        Assert.Equal($"{AppPaths.AdminEditions}?q=978", page.Find(".initial-navigation a:first-child").GetAttribute("href"));
        Assert.Equal("B", Text(page.Find(".initial-navigation a[aria-current='page']")));
    }

    [Fact]
    public void Empty_AnEntryWithoutResult_OffersTheWholeList()
    {
        Answer([], totalCount: 0);

        var empty = Open("entry=X").Find(".empty-list");

        Assert.Equal("Aucune édition sous cette entrée.", Text(empty.QuerySelector("strong")!));
        Assert.Equal([("Toutes les éditions", AppPaths.AdminEditions)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Audience_TheNavigationByInitial_ThenTheScreen()
    {
        Open("q=978&entry=B", recorded: true);

        Assert.Equal(["admin.editions.initial", "admin.editions"], _audience.Functions);
    }

    private IRenderedComponent<AdminEditions> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminEditions}?{criteria}");
        return Render<AdminEditions>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<EditionAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/editions"] = new EditionAdminList(Page(items, totalCount), NoFilters);

    private static Page<EditionAdminListItem> Page(IReadOnlyList<EditionAdminListItem> items, int totalCount) =>
        new(items, 1, AdminListQuery.PageSize, totalCount);

    [Fact]
    // The way to the editions and intents of an album has the icon of the editions wherever it is, its button and the entry of
    // the menu that takes its place on a smartphone (visuel/listes/principes-communs.md, Actions, #474).
    public void Rows_LeadToTheEditionsOfTheAlbum_WithTheIconOfTheEditions()
    {
        var row = Open("q=978").FindAll(".edition-row:not(.edition-row-head)")[0];

        var icons = row.QuerySelectorAll($".edition-actions a[href='{AppPaths.AdminAlbumEditions(Lunes.Id)}'] svg.icon path");
        Assert.Equal(2, icons.Length);
        Assert.All(icons, icon => Assert.Equal(
            Render<Icon>(parameters => parameters.Add(p => p.Name, IconName.Edition)).Find("path").GetAttribute("d"), icon.GetAttribute("d")));
    }

    private static string Text(IElement element) => Normalized(element.TextContent);

    // The non-breaking spaces of a label (after « ISBN ») as plain ones.
    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
