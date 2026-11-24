using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
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
/// The list of the ISBN to check (visuel/listes/liste-par-liste.md, fonctionnel/regles-metier/validation-de-l-isbn.md): its rows,
/// their actions, their state when the errors accepted are shown, its filters and its errors, told apart by role, link and text,
/// never by appearance. The writes of its actions, forms sent by the browser, are exercised end to end (AdminInvalidIsbnsTests of
/// Bdtheque.EndToEnd.Tests); what it shares with the list of the editions (search, navigation, empty list) is pinned by
/// AdminEditionsTests.
/// </summary>
public sealed class AdminInvalidIsbnsTests : BunitContext, IDisposable
{
    private static readonly Guid PublisherId = Guid.Parse("01900000-0000-7000-8000-0000000000b1");

    private static readonly AlbumSummary Barracuda = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a1"), null, Guid.NewGuid(), "Barracuda", AlbumType.Regular, false, 1, null, null, true);

    private static readonly AlbumSummary Longway = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a2"), null, Guid.NewGuid(), "Buddy Longway", AlbumType.Regular, false, 2, null, null, false);

    private static readonly AlbumSummary Passagers = new(
        Guid.Parse("01900000-0000-7000-8000-0000000000a3"), null, Guid.NewGuid(), "Les Passagers du vent", AlbumType.Regular, false, 1, null, null, true);

    private static readonly InvalidIsbnListItem Dargaud = new(
        Barracuda, new EditionSummary(Guid.Parse("01900000-0000-7000-8000-000000000001"), PublisherId, "Dargaud", null, null, 2010, "9782505008653"),
        null, "9782505008651", true, AcquisitionMode.Purchase, 2010, false, new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero), 41);

    private static readonly InvalidIsbnListItem Dupuis = new(
        Longway, new EditionSummary(Guid.Parse("01900000-0000-7000-8000-000000000002"), PublisherId, "Dupuis", null, null, 1974, "2800101234"),
        null, "2800101237", false, null, null, false, new DateTimeOffset(2026, 10, 9, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly InvalidIsbnListItem Glenat = new(
        Passagers, new EditionSummary(Guid.Parse("01900000-0000-7000-8000-000000000003"), PublisherId, "Glénat", null, null, 2001, "2-7234-3451"),
        null, null, true, AcquisitionMode.Purchase, null, false, new DateTimeOffset(2026, 2, 2, 8, 0, 0, TimeSpan.Zero), 43);

    private static readonly EditionListFilters NoFilters = new(null, null, null, null);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminInvalidIsbnsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<Components.Controls.RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        // The choice of a record imports its script once rendered, which these tests do not exercise.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Dargaud, Dupuis, Glenat], acceptedCount: 0);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    // Its title, that of its entry on the home page, with the number of errors to correct it counts; nothing is created here.
    public void Heading_TheTitleTheNumberOfErrorsToCorrect_AndWhatCorrectingDoes()
    {
        var page = Open("");

        Assert.Equal(["Administration", "ISBN au chiffre de contrôle incohérent"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("ISBN au chiffre de contrôle incohérent", "7"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Empty(page.FindAll(".list-heading a.button"));
        Assert.Equal("« Corriger » remplace l'ISBN saisi par l'ISBN corrigé.", Text(page.Find(".list-heading-note")));
    }

    [Fact]
    // The block bears the mention of the block « À vérifier » of the home page: the same set, the same mention.
    public void Block_BearsTheMentionOfTheChecks()
    {
        Assert.Equal("Contrôles", Text(Open("").Find(".block-kicker")));
    }

    [Fact]
    // No year, which the label already gives; an edition targeted by an intent is one, by its tag; without ISBN corrected, a dash.
    public void Rows_TheEditionItsAlbumTheIsbnCorrectedAndTheAcquisition()
    {
        var rows = Rows(Open("")).Select(row => (
            Text(row.QuerySelector(".row-entry")!),
            Text(row.QuerySelector(".row-entry-detail")!),
            Text(row.QuerySelector(".isbn-corrected")!),
            Text(row.QuerySelector(".isbn-acquisition")!)));

        Assert.Equal(
            [
                ("Dargaud [2010] - ISBN 9782505008653", "Barracuda - T. 1", "ISBN corrigé 9782505008651", "Achat, 2010"),
                ("Dupuis [1974] - ISBN 2800101234", "Buddy Longway - T. 2", "ISBN corrigé 2800101237", "Intention"),
                ("Glénat [2001] - ISBN 2-7234-3451", "Les Passagers du vent - T. 1", "—Aucun ISBN corrigé", "Achat"),
            ],
            rows);
        Assert.Empty(Open("").FindAll(".isbn-state"));
    }

    [Fact]
    // « Corriger » sends its own form, under the version of the album the row read; the menu modifies the edition, accepts its
    // error and leads to the album in the consultation.
    public void Rows_CorrectByAFormOfTheirOwn_AndOfferTheRestInTheirMenu()
    {
        var row = Rows(Open("q=978"))[0];
        var id = Dargaud.Edition.Id;

        var correction = row.QuerySelector(".isbn-actions > form")!;
        Assert.Equal(("post", "41"), (correction.GetAttribute("method"), correction.QuerySelector("input[name='version']")!.GetAttribute("value")));
        Assert.Equal(("Corriger", "Corriger l'ISBN de Dargaud [2010] - ISBN 9782505008653"),
            (Text(correction.QuerySelector("button[type='submit']")!), Normalized(correction.QuerySelector("button")!.GetAttribute("aria-label")!)));
        Assert.Equal(
            [
                ("Modifier l'édition", AppPaths.AdminEditEdition(Barracuda.Id, id)),
                ("Accepter l'erreur", null),
                ("Voir l'album dans la consultation", AppPaths.Album(Barracuda.Id)),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
        var acceptance = row.QuerySelector(".actions-menu-panel form")!;
        Assert.Equal(("post", "41"), (acceptance.GetAttribute("method"), acceptance.QuerySelector("input[name='version']")!.GetAttribute("value")));
    }

    [Fact]
    public void Rows_OfAnIntent_ModifyTheIntent()
    {
        var row = Rows(Open(""))[1];

        Assert.Equal("Modifier l'intention", Text(row.QuerySelectorAll(".actions-menu-panel .menu-item")[0]));
    }

    [Fact]
    // No ISBN to suppose: modifying the edition is the main action, which the menu then leaves out.
    public void Rows_WithoutIsbnCorrected_ModifyTheEditionAsTheirMainAction()
    {
        var row = Rows(Open(""))[2];

        Assert.Empty(row.QuerySelectorAll(".isbn-actions > form"));
        Assert.Equal([("Modifier l'édition", AppPaths.AdminEditEdition(Passagers.Id, Glenat.Edition.Id))],
            row.QuerySelectorAll(".isbn-actions > a.button").Select(button => (Text(button), button.GetAttribute("href"))));
        Assert.Equal(["Accepter l'erreur", "Voir l'album dans la consultation"], row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(Text));
    }

    [Fact]
    // Shown with those to correct, each error tells its state, an accepted one being accepted no more from its menu.
    public void State_AllTheErrors_EachTellingItsState()
    {
        Answer([Dargaud, Dupuis with { IsAccepted = true }], acceptedCount: 1);

        var page = Open("state=All");
        var rows = Rows(page);

        Assert.Equal("État", Text(page.Find(".isbn-row-head .isbn-state")));
        Assert.Equal(["À corriger", "Acceptée"], rows.Select(row => Text(row.QuerySelector(".isbn-state")!)));
        Assert.Equal("Ne plus accepter l'erreur", Text(rows[1].QuerySelectorAll(".actions-menu-panel .menu-item")[1]));
        Assert.Equal("2 éditions dont 1 acceptée", Text(page.Find(".list-summary").Children[0]));
        Assert.Contains(_api.Requested, path => path.StartsWith("/admin/editions/invalid-isbns?state=All", StringComparison.Ordinal));
    }

    [Fact]
    public void Filters_TheStateChosenInAList_ThenTheRecordsChosenByTyping()
    {
        var form = Open("filters=open&state=Accepted").Find("form.list-search");

        Assert.Equal(["État", "Série", "Album", "Éditeur", "Collection"], form.QuerySelectorAll(".list-filters label").Select(Text));
        Assert.Equal(
            [("", "À corriger"), ("Accepted", "Acceptées"), ("All", "Toutes")],
            form.QuerySelectorAll("select[name='state'] option").Select(option => (option.GetAttribute("value"), Text(option))));
        Assert.Equal("Accepted", form.QuerySelector("select[name='state'] option[selected]")!.GetAttribute("value"));
    }

    [Fact]
    public void ActiveFilters_TheStateChosen_ATagThatLeadsBackToTheErrorsToCorrect()
    {
        var filters = Open("q=978&state=All").Find(".active-filters");

        Assert.Equal(
            [("État : Toutes", $"{AppPaths.AdminInvalidIsbns}?q=978")],
            filters.QuerySelectorAll(".tag").Select(tag => (Text(tag.QuerySelector("span")!), tag.QuerySelector("a")!.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_WithoutAnyCriterion_SaysThereIsNoIsbnToCheck()
    {
        Answer([], acceptedCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucun ISBN à vérifier.", Normalized(empty.QuerySelector("strong")!.TextContent));
        Assert.Empty(empty.QuerySelectorAll("a"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/editions/invalid-isbns"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".isbn-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("state=All", recorded: true);

        Assert.Equal(["admin.invalid-isbns.filter", "admin.invalid-isbns"], _audience.Functions);
    }

    private IRenderedComponent<AdminInvalidIsbns> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminInvalidIsbns}?{criteria}");
        return Render<AdminInvalidIsbns>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<InvalidIsbnListItem> items, int acceptedCount) =>
        _api.Answers["/admin/editions/invalid-isbns"] = new InvalidIsbnList(
            new Page<InvalidIsbnListItem>(items, 1, ListQuery.PageSize, items.Count), NoFilters, acceptedCount);

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<AdminInvalidIsbns> page) =>
        page.FindAll(".isbn-row:not(.isbn-row-head)");

    private static string Text(IElement element) => Normalized(element.TextContent);

    // The non-breaking spaces of a label (after « ISBN ») as plain ones.
    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
