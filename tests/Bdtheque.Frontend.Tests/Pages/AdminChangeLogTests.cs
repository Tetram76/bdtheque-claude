using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The whole change log (visuel/journal/journal-des-modifications.md): its entries grouped by day in the time zone of the browser,
/// their operation, their changes and their menu, its search, its filters and the history of one record, its empty cases, told
/// apart by role, link and text, never by appearance.
/// </summary>
public sealed class AdminChangeLogTests : BunitContext, IDisposable
{
    // Wednesday 25 November 2026, 11:00 in Paris.
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);

    private static readonly AlbumLabel Iris = new("L'Iris blanc", "Astérix", AlbumType.Regular, false, 40, null, null);

    private static readonly Guid AlbumId = Guid.Parse("01900000-0000-7000-8000-0000000000a1");
    private static readonly Guid EditionId = Guid.Parse("01900000-0000-7000-8000-0000000000e1");
    private static readonly Guid GenreId = Guid.Parse("01900000-0000-7000-8000-0000000000c1");

    private static readonly ChangeLogEntryItem AlbumModified = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), Now.AddMinutes(15), RecordType.Album, AlbumId, null, true, Iris,
        ChangeLogOperation.Modification,
        [
            new(ChangeLogAttribute.Rating, new EnumValue(nameof(AlbumRating), nameof(AlbumRating.Good)), new EnumValue(nameof(AlbumRating), nameof(AlbumRating.VeryGood))),
            new(ChangeLogAttribute.Genre, null, new ReferenceValue(Guid.NewGuid(), new NameLabel("Humour"))),
            new(ChangeLogAttribute.Contribution, new ContributionValue(Guid.NewGuid(), new AuthorLabel("Uderzo", "Albert", null), ContributionRole.Illustrator), null),
            new(ChangeLogAttribute.Summary, null, null),
            new(ChangeLogAttribute.PersonalNotes, new TextValue(""), new TextValue("À relire")),
            new(ChangeLogAttribute.Status, null, new EnumValue(nameof(SeriesStatus), nameof(SeriesStatus.Completed))),
            new(ChangeLogAttribute.Website, new TextValue("https://example.org"), null),
        ]);

    private static readonly ChangeLogEntryItem EditionCreated = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), Now.AddMinutes(12), RecordType.Edition, EditionId, AlbumId, true,
        new EditionLabel(Iris, "Albert René", null, 2023, null), ChangeLogOperation.Creation, []);

    // Yesterday in Paris, at 23:30; universal time still says the day before at 22:30.
    private static readonly ChangeLogEntryItem GenreDeleted = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), new DateTimeOffset(2026, 11, 24, 22, 30, 0, TimeSpan.Zero), RecordType.Genre, GenreId, null, false,
        new NameLabel("Western spaghetti"), ChangeLogOperation.Deletion, []);

    private static readonly ChangeLogEntryItem GenreCreated = new(
        Guid.Parse("01900000-0000-7000-8000-000000000004"), new DateTimeOffset(2026, 10, 8, 16, 2, 0, TimeSpan.Zero), RecordType.Genre, GenreId, null, false,
        new NameLabel("Western spaghetti"), ChangeLogOperation.Creation, []);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUi = CultureInfo.CurrentUICulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 25), "base", new DateOnly(2026, 11, 25)).Format() },
        authenticated: true);

    public AdminChangeLogTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<ChangeLogTexts>();
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
        Answer([AlbumModified, EditionCreated, GenreDeleted, GenreCreated]);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        CultureInfo.CurrentUICulture = _previousUi;
        Dispose();
    }

    [Fact]
    // Its title, with the number of entries of the whole change log; nothing is created here.
    public void Heading_TheTitleAndTheWholeCount()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Journal des modifications"], page.FindAll(".breadcrumb li").Select(item => Text(item)));
        Assert.Equal(("Journal des modifications", "12 408"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Empty(page.FindAll(".list-heading a.button"));
        Assert.Equal("Journal", Text(page.Find(".block-kicker")));
    }

    [Fact]
    // Grouped by day in the time zone of the browser: today and yesterday named, any other day by its date.
    public void Days_InTheTimeZoneOfTheBrowser_TodayAndYesterdayNamed()
    {
        var page = Open("");

        Assert.Equal(
            ["Aujourd'hui, mercredi 25 novembre 2026", "Hier, mardi 24 novembre 2026", "Jeudi 8 octobre 2026"],
            page.FindAll(".log-day").Select(Text));
        Assert.Equal(["11:15", "11:12", "23:30", "18:02"], Rows(page).Select(row => Text(row.QuerySelector(".log-time")!)));
    }

    [Fact]
    // The label of the record and its type below it; an edition by itself, its album on the line of its type.
    public void Rows_TheRecordItsTypeAndTheOperation()
    {
        var rows = Rows(Open("")).Select(row => (
            Text(row.QuerySelector(".row-entry")!), Text(row.QuerySelector(".row-entry-detail")!), Text(row.QuerySelector(".log-operation")!)));

        Assert.Equal(
            [
                ("L'Iris blanc (Astérix - T. 40)", "Album", "Modification"),
                ("Albert René [2023]", "Édition de L'Iris blanc (Astérix - T. 40)", "Création"),
                ("Western spaghetti", "Genre", "Suppression"),
                ("Western spaghetti", "Genre", "Création"),
            ],
            rows);
    }

    [Fact]
    // Each change: its attribute, then its value before and after, an element carried added or withdrawn, a secret without its
    // values, a value missing or empty of any other attribute as a data absent.
    public void Changes_OfAModification_EachWithItsValues()
    {
        var changes = Rows(Open(""))[0].QuerySelectorAll(".log-change")
            .Select(change => (Text(change.QuerySelector(".log-attribute")!), Text(change.QuerySelector(".log-values")!)));

        Assert.Equal(
            [
                ("Appréciation", "Bien → Très bien"),
                ("Genres", "+ Humour"),
                ("Auteurs", "Albert Uderzo (Dessin) retiré"),
                ("Résumé", "renouvelée, valeur non consignée"),
                ("Notes personnelles", "—Non renseigné → À relire"),
                ("Statut", "—Non renseigné → Terminée"),
                ("Site web", "https://example.org → —Non renseigné"),
            ],
            changes);
        Assert.Empty(Rows(Open(""))[1].QuerySelectorAll(".log-changes"));
    }

    [Fact]
    // The value withdrawn struck through, and it alone: no space at its end under the strikethrough.
    public void Changes_AWithdrawal_StrikesThroughItsValueOnly()
    {
        var struck = Rows(Open(""))[0].QuerySelector(".log-before s")!;

        Assert.Equal("Albert Uderzo (Dessin)", struck.TextContent);
    }

    [Fact]
    // Its record modified, its history, the consultation: a record deleted since leads only to its history.
    public void Menu_LeadsToTheRecordAsLongAsItExists()
    {
        var rows = Rows(Open(""));

        Assert.Equal(
            [
                ("Modifier la fiche", AppPaths.AdminEditAlbum(AlbumId)),
                ("Historique de cette fiche", $"{AppPaths.AdminChangeLog}?record={AlbumId}"),
                ("Voir dans la consultation", AppPaths.Album(AlbumId)),
            ],
            MenuOf(rows[0]));
        Assert.Equal(
            [("Modifier la fiche", AppPaths.AdminEditEdition(AlbumId, EditionId)), ("Historique de cette fiche", $"{AppPaths.AdminChangeLog}?record={EditionId}")],
            MenuOf(rows[1]));
        Assert.Equal([("Historique de cette fiche", $"{AppPaths.AdminChangeLog}?record={GenreId}")], MenuOf(rows[2]));
        Assert.Equal("Autres actions sur Western spaghetti", rows[2].QuerySelector(".actions-menu > button")!.GetAttribute("aria-label"));
    }

    [Fact]
    // The history of a record: its filter named by its type and label, without the history in the menu of its entries.
    public void History_OfARecord_IsAFilterNamedByTheRecord()
    {
        Answer([GenreDeleted, GenreCreated], record: new ChangeLogRecord(RecordType.Genre, new NameLabel("Western spaghetti")));

        var page = Open($"record={GenreId}");

        Assert.Equal(
            [("Fiche : Genre Western spaghetti", AppPaths.AdminChangeLog)],
            page.Find(".active-filters").QuerySelectorAll(".tag").Select(t => (Text(t.QuerySelector("span")!), t.QuerySelector("a")!.GetAttribute("href"))));
        // A record deleted since has nowhere left to lead in its own history: no menu.
        Assert.All(Rows(page), row => Assert.Null(row.QuerySelector(".actions-menu")));
        Assert.Contains($"/admin/change-log?record={GenreId}&page=1", _api.Requested[^1], StringComparison.Ordinal);
    }

    [Fact]
    // The type, the operation and the period, chosen in the panel; the days of the period in the time zone of the browser.
    public void Filters_TheTypeTheOperationAndThePeriod()
    {
        var page = Open("recordType=Genre&operation=Deletion&from=2026-11-01&to=2026-11-25&filters=open");

        Assert.Equal(
            ["Type de fiche : Genre", "Opération : Suppression", "Du : 01/11/2026", "Au : 25/11/2026"],
            page.Find(".active-filters").QuerySelectorAll(".tag").Select(t => Text(t.QuerySelector("span")!)));
        var form = page.Find("form.list-search");
        Assert.Equal("Genre", form.QuerySelector("select[name='recordType'] option[selected]")!.GetAttribute("value"));
        Assert.Equal("Deletion", form.QuerySelector("select[name='operation'] option[selected]")!.GetAttribute("value"));
        Assert.Equal(("2026-11-01", "2026-11-25"),
            (form.QuerySelector("input[name='from']")!.GetAttribute("value"), form.QuerySelector("input[name='to']")!.GetAttribute("value")));
        Assert.EndsWith("&timeZone=Europe%2FParis", _api.Requested.Last(r => r.StartsWith("/admin/change-log?", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    [Fact]
    // A record whose history is presented is kept by the panel of the filters, which no field of it chooses.
    public void History_KeptByThePanelOfTheFilters()
    {
        var form = Open($"record={GenreId}&filters=open").Find("form.list-search");

        Assert.Equal(GenreId.ToString(), form.QuerySelector("input[type='hidden'][name='record']")!.GetAttribute("value"));
    }

    [Theory]
    [InlineData("", "4 entrées")]
    [InlineData("q=Western", "4 entrées sur 12 408")]
    public void Summary_TheCount_OutOfTheWholeChangeLogOnceNarrowed(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal((count, "De la plus récente à la plus ancienne"), (Text(summary.Children[0]), Text(summary.Children[1])));
    }

    [Fact]
    public void Empty_ASearch_RecallsWhatItLooksInto()
    {
        Answer([]);

        var empty = Open("q=Spirou").Find(".empty-list");

        Assert.Equal(
            ("Aucune entrée ne correspond à « Spirou ».",
                "La recherche porte sur le libellé des fiches, sans tenir compte des majuscules ni des accents."),
            (Text(empty.QuerySelector("strong")!), Text(empty.QuerySelectorAll("span")[0])));
        Assert.Equal([("Effacer la recherche", AppPaths.AdminChangeLog)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_UnderFilters_OffersToClearThem()
    {
        Answer([]);

        var empty = Open("operation=Deletion").Find(".empty-list");

        Assert.Equal("Aucune entrée ne correspond à ces critères.", Text(empty.QuerySelector("strong")!));
        Assert.Equal([("Effacer les filtres", AppPaths.AdminChangeLog)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Theory]
    // Over a period, the entries found depend on the time zone of the browser even when there are none: the page is rendered
    // again once it knows it (wwwroot/browser-time-zone.js), which only an element marked data-local asks for.
    [InlineData("from=2026-11-01", true)]
    [InlineData("to=2026-11-25", true)]
    [InlineData("operation=Deletion", false)]
    public void Empty_OverAPeriod_DependsOnTheTimeZone(string query, bool local)
    {
        Answer([]);

        var page = Open(query);

        Assert.Equal(local, page.FindAll(".log-empty[data-local]").Count == 1);
        Assert.Single(page.FindAll(".log-empty .empty-list"));
    }

    [Fact]
    public void Empty_NothingYet_SaysSo()
    {
        Answer([]);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucune modification n'est encore consignée.", Text(empty.QuerySelector("strong")!));
        Assert.Empty(empty.QuerySelectorAll("a"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/change-log"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".log-row"));
    }

    [Theory]
    [InlineData("", new[] { "admin.change-log" })]
    [InlineData("q=Western", new[] { "admin.change-log.search", "admin.change-log" })]
    [InlineData("operation=Creation", new[] { "admin.change-log.filter", "admin.change-log" })]
    [InlineData("page=2", new[] { "admin.change-log.page", "admin.change-log" })]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen(string criteria, string[] functions)
    {
        Answer([AlbumModified], totalCount: 120);

        Open(criteria, recorded: true);

        Assert.Equal(functions, _audience.Functions);
    }

    private IRenderedComponent<AdminChangeLog> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminChangeLog}?{criteria}");
        return Render<AdminChangeLog>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<ChangeLogEntryItem> entries, ChangeLogRecord? record = null, int? totalCount = null) =>
        _api.Answers["/admin/change-log"] = new ChangeLogList(
            new Page<ChangeLogEntryItem>(entries, 1, ListQuery.PageSize, totalCount ?? entries.Count), 12408, record);

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<AdminChangeLog> page) => page.FindAll(".log-row:not(.log-row-head)");

    private static List<(string, string?)> MenuOf(IElement row) =>
        row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))).ToList();

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
