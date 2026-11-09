using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Pages.Admin;
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

    private static readonly EditionListFilters NoFilters = new(null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
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
    public void Heading_TheTitleTheNumberOfEditionsAndTheCreation()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Éditions"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Éditions", "2 913"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        // A new edition starts with the choice of its album, as the entry of the menu of the administration.
        Assert.Equal(AppPaths.AdminNewEdition, page.Find(".list-heading a.button").GetAttribute("href"));
    }

    [Fact]
    public void Rows_TheEditionItsAlbumItsYearItsCategoryAndItsAcquisition()
    {
        var rows = Open("").FindAll(".edition-row:not(.edition-row-head)").Select(row => (
            Text(row.QuerySelector(".edition-label")!),
            Text(row.QuerySelector(".edition-album")!),
            Text(row.QuerySelector(".edition-year")!),
            Text(row.QuerySelector(".edition-category")!),
            Text(row.QuerySelector(".edition-acquisition")!)));

        Assert.Equal(
            [
                ("Glénat [2004] - ISBN 978-2-7234-4567-1", "...Et après (Les Lunes de Saturne - T. 1)", "Année 2004", "Non renseignée", "Achat, 2004"),
                ("Dupuis (Repérages)", "Buddy Longway - T. 2", "Année Non renseignée", "Édition originale", "Échange"),
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

        Assert.Equal(AppPaths.AdminEditEdition(Lunes.Id, id), row.QuerySelector(".edition-label")!.GetAttribute("href"));
        Assert.Equal("Modifier Glénat [2004] - ISBN 978-2-7234-4567-1", Normalized(row.QuerySelector(".edition-actions a.button")!.GetAttribute("aria-label")!));
        Assert.Equal(
            [
                ("Voir l'album dans la consultation", AppPaths.Album(Lunes.Id)),
                ("Fusionner avec une autre édition…", $"{AppPaths.AdminEditions}?q=978&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminEditions}?q=978&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Theory]
    [InlineData("", "2 913 éditions")]
    [InlineData("q=978", "2 913 éditions sur 2 913")]
    public void Summary_TheCountOutOfAllTheEditions_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Dans l'ordre des albums, puis par année d'édition", Text(summary.Children[1]));
    }

    [Fact]
    public void Navigation_ByInitial_IsNone()
    {
        Assert.Empty(Open("").FindAll(".initial-navigation"));
    }

    [Fact]
    public void Filters_UnfoldedAPanelOfTheirFields_TheRecordsChosenByTyping()
    {
        _api.Answers["/admin/editions"] = new EditionAdminList(Page([Glenat], 1), NoFilters with { Album = Lunes });

        var form = Open($"filters=open&albumId={Lunes.Id}").Find("form.list-search");

        Assert.Equal("true", form.QuerySelector("button[aria-controls]")!.GetAttribute("aria-expanded"));
        Assert.Equal(["Album", "Éditeur", "Collection éditeur"], form.QuerySelectorAll(".list-filters label").Select(Text));
        Assert.Equal(
            ["albumId", "publisherId", "publisherCollectionId"],
            form.QuerySelectorAll(".list-filters input[type='hidden']").Select(input => input.GetAttribute("name")));
        Assert.Equal(Lunes.Id.ToString(), form.QuerySelector(".list-filters input[name='albumId']")!.GetAttribute("value"));
        // The album chosen is named by its label, as in the rows.
        Assert.Equal("...Et après (Les Lunes de Saturne - T. 1)", form.QuerySelector("#filter-album")!.GetAttribute("value"));
    }

    [Fact]
    public void ActiveFilters_TagsThatLeadToTheListWithoutThem()
    {
        _api.Answers["/admin/editions"] = new EditionAdminList(
            Page([Glenat], 1), NoFilters with { Album = Lunes, Publisher = new PublisherListItem(PublisherId, "Glénat", null) });

        var filters = Open($"q=978&albumId={Lunes.Id}&publisherId={PublisherId}").Find(".active-filters");

        Assert.Equal(
            [
                ("Album : ...Et après (Les Lunes de Saturne - T. 1)", $"{AppPaths.AdminEditions}?q=978&publisherId={PublisherId}"),
                ("Éditeur : Glénat", $"{AppPaths.AdminEditions}?q=978&albumId={Lunes.Id}"),
            ],
            filters.QuerySelectorAll(".tag").Select(tag => (Text(tag.QuerySelector("span")!), tag.QuerySelector("a")!.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearItOrToCreateAnEdition()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=978-0").Find(".empty-list");

        Assert.Equal("Aucune édition ne correspond à « 978-0 ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal(
            [("Effacer la recherche", AppPaths.AdminEditions), ("Nouvelle édition", AppPaths.AdminNewEdition)],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
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
        Assert.Contains("Retirez d'abord l'édition des critères de ces recherches enregistrées, puis recommencez.", Text(dialog));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherEditionByItsIsbn_AndContinuesWithTheOneChosen()
    {
        var dialog = Open($"merge={Glenat.Edition.Id}&mergeQ=978").Find("dialog");

        Assert.Equal("Fusionner deux éditions", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [("Dupuis (Repérages)", "Buddy Longway - T. 2 · Échange")],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        var next = dialog.QuerySelector("#edition-merge-continue")!;
        Assert.Equal(AppPaths.AdminMergeEditions, next.GetAttribute("action"));
        Assert.Equal(Glenat.Edition.Id.ToString(), next.QuerySelector("input[name='edition']")!.GetAttribute("value"));
        Assert.Contains(_api.Requested, path => path == "/admin/editions?q=978&pageSize=11");
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

    private static string Text(IElement element) => Normalized(element.TextContent);

    // The non-breaking spaces of a label (after « ISBN ») as plain ones.
    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
