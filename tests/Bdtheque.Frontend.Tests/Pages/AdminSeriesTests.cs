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
/// The list of the series of the administration (visuel/listes/liste-par-liste.md): its rows, its count, its filters, its
/// dialogs and its errors, told apart by role, link and text, never by appearance. What it shares with the list of the albums
/// (search, active filters, navigation, empty list, page beyond the last) is pinned by AdminAlbumsTests.
/// </summary>
public sealed class AdminSeriesTests : BunitContext, IDisposable
{
    private static readonly Guid Genre = Guid.Parse("01900000-0000-7000-8000-0000000000a2");
    private static readonly AlbumCover FirstCover = new(Guid.Parse("01900000-0000-7000-8000-0000000000c1"));

    private static readonly SeriesAdminListItem Barracuda = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), "Barracuda", SeriesStatus.Completed,
        new SeriesOverview(6, [FirstCover, null, null]), 6, true, new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero), 41);

    private static readonly SeriesAdminListItem Becassine = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), "Bécassine", SeriesStatus.InProgress,
        new SeriesOverview(27, []), 1, false, new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly SeriesAdminListItem Bone = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), "Bone", null,
        new SeriesOverview(9, []), 0, false, new DateTimeOffset(2026, 5, 19, 8, 0, 0, TimeSpan.Zero), 43);

    private static readonly SeriesAdminListItem Bureau = new(
        Guid.Parse("01900000-0000-7000-8000-000000000004"), "Le Bureau des prolongations", SeriesStatus.Abandoned,
        new SeriesOverview(0, []), 0, false, new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero), 44);

    private static readonly SeriesListFilters NoFilters = new(null, null, null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminSeriesTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<Components.Controls.RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Barracuda, Becassine, Bone, Bureau], totalCount: 31);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfSeriesAndTheCreation()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Séries"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Séries", "412"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Equal(AppPaths.AdminNewSeries, page.Find(".list-heading a.button").GetAttribute("href"));
    }

    [Fact]
    public void Rows_TheSeriesItsStatusItsAlbumsAndWhetherItIsComplete()
    {
        var rows = Open("").FindAll(".series-row:not(.series-row-head)").Select(row => (
            Text(row.QuerySelector(".series-title")!),
            Text(row.QuerySelector(".series-status")!),
            Text(row.QuerySelector(".series-albums")!),
            Text(row.QuerySelector(".series-complete")!)));

        Assert.Equal(
            [
                ("Barracuda", "Terminée", "6 albums · 6 dans la collection", "Complète"),
                ("Bécassine", "En cours", "27 albums · 1 dans la collection", "À compléter"),
                ("Bone", "Non renseigné", "9 albums · aucun dans la collection", "À compléter"),
                ("Le Bureau des prolongations", "Abandonnée", "Aucun album", "À compléter"),
            ],
            rows);
    }

    [Fact]
    public void Rows_MarkASeriesWithoutAlbumAndACompleteOne_ByThePresenceOfTheirAttribute()
    {
        // A boolean attribute of Blazor has no value: present when true, absent when false, which the styles select on.
        var rows = Open("").FindAll(".series-row:not(.series-row-head)").Select(row => (
            row.QuerySelector(".series-albums")!.HasAttribute("data-empty"),
            row.QuerySelector(".series-complete")!.HasAttribute("data-complete")));

        Assert.Equal([(false, true), (false, false), (false, false), (true, false)], rows);
    }

    [Fact]
    public void Rows_APileOfThreeCovers_TheFirstVolumeInFront()
    {
        var pile = Open("").FindAll(".series-row:not(.series-row-head)")[0].QuerySelector(".cover-pile")!;

        // The back of the pile first: the third and second volumes, without cover, then the first one, in front.
        Assert.Equal(
            [null, null, VisualMediaRoutes.Display(FirstCover.VisualId)],
            pile.QuerySelectorAll(".cover").Select(cover => cover.GetAttribute("src")));
    }

    [Fact]
    public void Rows_LeadToTheEntryFormAndTheConsultation_AndOpenTheirDialogsInTheList()
    {
        var row = Open("q=barra").FindAll(".series-row:not(.series-row-head)")[0];
        var id = Barracuda.Id;

        Assert.Equal(AppPaths.AdminEditSeries(id), row.QuerySelector(".series-title")!.GetAttribute("href"));
        Assert.Equal("Modifier Barracuda", row.QuerySelector(".series-actions a.button")!.GetAttribute("aria-label"));
        Assert.Equal(
            [
                ("Voir dans la consultation", AppPaths.SeriesRecord(id)),
                ("Fusionner avec une autre série…", $"{AppPaths.AdminSeries}?q=barra&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminSeries}?q=barra&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Theory]
    [InlineData("", "31 séries")]
    [InlineData("entry=B", "31 séries sous la lettre B")]
    [InlineData("q=barra", "31 séries sur 412")]
    public void Summary_TheCountUnderTheEntryOrOutOfAllTheSeries_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Triées par titre", Text(summary.Children[1]));
    }

    [Fact]
    public void Filters_UnfoldedAPanelOfTheirFields_TheRecordsChosenByTyping()
    {
        _api.Answers["/admin/series"] = new SeriesAdminList(Page([Barracuda], 1), NoFilters with { Genre = new GenreListItem(Genre, "Aventure") });

        var form = Open($"filters=open&genreId={Genre}").Find("form.list-search");

        Assert.Equal("true", form.QuerySelector("button[aria-controls]")!.GetAttribute("aria-expanded"));
        Assert.Equal(["Auteur", "Éditeur", "Collection éditeur", "Genre", "Univers"], form.QuerySelectorAll(".list-filters label").Select(Text));
        Assert.Equal(
            ["authorId", "publisherId", "publisherCollectionId", "genreId", "universeId"],
            form.QuerySelectorAll(".list-filters input[type='hidden']").Select(input => input.GetAttribute("name")));
        Assert.Equal(Genre.ToString(), form.QuerySelector(".list-filters input[name='genreId']")!.GetAttribute("value"));
    }

    [Fact]
    public void ActiveFilters_TagsThatLeadToTheListWithoutThem()
    {
        _api.Answers["/admin/series"] = new SeriesAdminList(Page([Barracuda], 1), NoFilters with { Genre = new GenreListItem(Genre, "Aventure") });

        var filters = Open($"q=barra&genreId={Genre}").Find(".active-filters");

        Assert.Equal(
            [("Genre : Aventure", $"{AppPaths.AdminSeries}?q=barra")],
            filters.QuerySelectorAll(".tag").Select(tag => (Text(tag.QuerySelector("span")!), tag.QuerySelector("a")!.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearItOrToCreateTheSeries()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=Zorglub").Find(".empty-list");

        Assert.Equal("Aucune série ne correspond à « Zorglub ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal(
            [("Effacer la recherche", AppPaths.AdminSeries), ("Créer la série « Zorglub »", AppPaths.AdminNewSeries)],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Deletion_ADialogThatAnnouncesTheImpact_AndConfirmsWithTheVersionAndTheFingerprint()
    {
        var id = Bureau.Id;
        _api.Answers[$"/admin/series/{id}/deletion-impact"] = new DeletionImpact([], [], [new ImpactCount(EntityKind.Contribution, 2)], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Supprimer cette série ?", "Le Bureau des prolongations"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Contains("2 contributions", Text(dialog.QuerySelector(".deletion-impact")!));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "44"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
    }

    [Fact]
    public void Deletion_RefusedWhileTheSeriesHasAlbums_SaysWhatToDo()
    {
        var id = Barracuda.Id;
        _api.Answers[$"/admin/series/{id}/deletion-impact"] = new DeletionImpact([new ImpactCount(EntityKind.Album, 6)], [], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Contains("6 albums", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Contains("Rattachez d'abord ses albums à une autre série, ou supprimez-les, puis recommencez.", Text(dialog));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherSeries_AndContinuesWithTheOneChosen()
    {
        var dialog = Open($"merge={Barracuda.Id}&mergeQ=b").Find("dialog");

        Assert.Equal("Fusionner deux séries", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [
                ("Bécassine", "En cours · 27 albums · 1 dans la collection"),
                ("Bone", "9 albums · aucun dans la collection"),
                ("Le Bureau des prolongations", "Abandonnée · Aucun album"),
            ],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        var next = dialog.QuerySelector("#series-merge-continue")!;
        Assert.Equal(AppPaths.AdminMergeSeries, next.GetAttribute("action"));
        Assert.Equal(Barracuda.Id.ToString(), next.QuerySelector("input[name='series']")!.GetAttribute("value"));
        Assert.Contains(_api.Requested, path => path == "/admin/series?q=b&pageSize=11");
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/series"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".series-row"));
    }

    [Fact]
    public void Counts_NotGivenByTheApi_LeaveTheListWithoutItsNumber()
    {
        _api.Failures["/admin/record-counts"] = HttpStatusCode.InternalServerError;

        var page = Open("q=barra");

        Assert.Empty(page.FindAll(".list-heading-count"));
        Assert.Equal("31 séries", Text(page.Find(".list-summary").Children[0]));
        Assert.Equal(4, page.FindAll(".series-row:not(.series-row-head)").Count);
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("q=barra&entry=B", recorded: true);

        // The action of the list first, which displays the screen.
        Assert.Equal(["admin.series.initial", "admin.series"], _audience.Functions);
    }

    private IRenderedComponent<AdminSeries> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminSeries}?{criteria}");
        return Render<AdminSeries>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<SeriesAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/series"] = new SeriesAdminList(Page(items, totalCount), NoFilters);

    private static Page<SeriesAdminListItem> Page(IReadOnlyList<SeriesAdminListItem> items, int totalCount) =>
        new(items, 1, AdminListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
