using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
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
/// The list of the universes of the administration (visuel/listes/liste-par-liste.md): its rows in the order of the hierarchy,
/// each in retreat under its parent, and the side panel of a universe over the list (visuel/listes/saisie-selon-l-entite.md),
/// told apart by role, link and text, never by appearance. What it shares with the other lists (dialogs, empty list, errors of
/// the list) is pinned by AdminPublishersTests and AdminGenresTests.
/// </summary>
public sealed class AdminUniversesTests : BunitContext, IDisposable
{
    private static readonly UniverseListItem PrattItem = new(Guid.Parse("01900000-0000-7000-8000-000000000001"), "Univers Hugo Pratt", null, null);
    private static readonly UniverseListItem CortoItem = new(Guid.Parse("01900000-0000-7000-8000-000000000002"), "Corto Maltese", PrattItem.Id, PrattItem.Name);
    private static readonly DateTimeOffset January = new(2026, 1, 12, 9, 30, 0, TimeSpan.Zero);

    private static readonly UniverseAdminListItem Pratt = Row(PrattItem, [], 31, 4, 3, 41);
    private static readonly UniverseAdminListItem Corto = Row(CortoItem, [PrattItem], 18, 1, 1, 42);
    private static readonly UniverseAdminListItem Antilles = Row(
        new(Guid.Parse("01900000-0000-7000-8000-000000000003"), "Les Antilles", CortoItem.Id, CortoItem.Name), [PrattItem, CortoItem], 4, 0, 0, 43);
    private static readonly UniverseAdminListItem Cities = Row(
        new(Guid.Parse("01900000-0000-7000-8000-000000000004"), "Les Cités obscures", null, null), [], 14, 1, 0, 44);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminUniversesTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Pratt, Corto, Antilles, Cities], totalCount: 4);
        _api.Answers[$"/admin/universes/{Corto.Id}"] = new UniverseForm(
            Corto.Id, "Corto Maltese", "Les voyages du marin.", PrattItem.Id, new UniverseInHierarchy(PrattItem, []), January, January, 42);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfUniversesAndThePanelOfANewOne()
    {
        var page = Open("q=c");

        Assert.Equal(("Univers", "37"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Equal(("Nouvel univers", $"{AppPaths.AdminUniverses}?q=c&new=open"),
            (Text(page.Find(".list-heading a.button")), page.Find(".list-heading a.button").GetAttribute("href")));
        Assert.Equal(("4 univers sur 37", "Chaque univers suivi de ses sous-univers, dont il compte les albums et séries"),
            (Text(page.Find(".list-summary").Children[0]), Text(page.Find(".list-summary").Children[1])));
    }

    [Fact]
    public void Rows_InRetreatUnderTheirParent_WithTheirNumbers()
    {
        var rows = Open("").FindAll(".universe-row:not(.universe-row-head)").Select(row => (
            Text(row.QuerySelector(".universe-name")!),
            row.GetAttribute("data-depth"),
            Text(row.QuerySelector(".universe-albums")!),
            Text(row.QuerySelector(".universe-series")!),
            Text(row.QuerySelector(".universe-subuniverses")!)));

        Assert.Equal(
            [
                ("Univers Hugo Pratt", "0", "Albums 31", "Séries 4", "Sous-univers 3"),
                ("Corto Maltese", "1", "Albums 18", "Séries 1", "Sous-univers 1"),
                ("Les Antilles", "2", "Albums 4", "Séries 0", "Sous-univers 0"),
                ("Les Cités obscures", "0", "Albums 14", "Séries 1", "Sous-univers 0"),
            ],
            rows);
    }

    [Fact]
    public void Rows_WhoseParentIsNotOnThePage_NameTheirAncestors()
    {
        Answer([Antilles, Cities], totalCount: 2);

        var rows = Open("q=les").FindAll(".universe-row:not(.universe-row-head)");

        // Found without its ancestors, a sub-universe is no longer in retreat, and says where it belongs.
        Assert.Equal(("0", "Univers Hugo Pratt › Corto Maltese"), (rows[0].GetAttribute("data-depth"), Text(rows[0].QuerySelector(".universe-path")!)));
        Assert.Null(rows[1].QuerySelector(".universe-path"));
    }

    [Fact]
    public void Rows_OpenThePanelAndTheirDialogsInTheList()
    {
        var row = Open("q=c").FindAll(".universe-row:not(.universe-row-head)")[1];
        var id = Corto.Id;

        Assert.Equal($"{AppPaths.AdminUniverses}?q=c&edit={id}", row.QuerySelector(".universe-actions a.button")!.GetAttribute("href"));
        Assert.Equal(
            [
                ("Voir ses œuvres dans la consultation", AppPaths.UniverseWorks(id)),
                ("Fusionner avec un autre univers…", $"{AppPaths.AdminUniverses}?q=c&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminUniverses}?q=c&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Fact]
    public void Panel_OfAUniverse_ItsFieldsItsParentWithItsAncestorsAndItsDates()
    {
        var panel = Open($"q=c&edit={Corto.Id}").Find("dialog.side-panel");

        Assert.Equal("Modifier l'univers", Text(panel.QuerySelector("h2")!));
        Assert.Equal("Corto Maltese", panel.QuerySelector("input[name='name']")!.GetAttribute("value"));
        Assert.Equal("Les voyages du marin.", ((AngleSharp.Html.Dom.IHtmlTextAreaElement)panel.QuerySelector("textarea[name='description']")!).Value);
        Assert.Equal("Univers Hugo Pratt", panel.QuerySelector(".record-picker input[role='combobox']")!.GetAttribute("value"));
        Assert.Equal(PrattItem.Id.ToString(), panel.QuerySelector("input[type='hidden'][name='parentId']")!.GetAttribute("value"));
        Assert.Equal("Ses albums et séries sont aussi ceux de Univers Hugo Pratt.", Text(panel.QuerySelector(".field-help")!));
        // The dates at the top of the panel, above its first field.
        Assert.Equal("Univers créé le 12/01/2026 10:30, modifié le 12/01/2026 10:30.", Text(panel.QuerySelector(".side-panel-body > :first-child")!));
        Assert.Equal(
            [("Annuler", $"{AppPaths.AdminUniverses}?q=c"), ("Enregistrer", null)],
            panel.QuerySelectorAll(".side-panel-actions .button").Select(button => (Text(button), button.GetAttribute("href"))));
    }

    [Fact]
    public void Panel_OfANewUniverse_AtTheTopOfTheHierarchyByDefault()
    {
        var panel = Open("new=open").Find("dialog.side-panel");

        Assert.Equal("Nouvel univers", Text(panel.QuerySelector("h2")!));
        Assert.Equal("Sans parent, l'univers est de premier niveau.", Text(panel.QuerySelector(".field-help")!));
        Assert.Null(panel.QuerySelector(".side-panel-dates"));
        Assert.Equal(["Annuler", "Créer l'univers"], panel.QuerySelectorAll(".side-panel-actions .button").Select(Text));
    }

    [Fact]
    public void Panel_SavesTheUniverseWithItsVersion_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/universes/{Corto.Id}"]);
        var page = Open($"q=c&edit={Corto.Id}", recorded: true);

        page.Find("input[name='name']").Change("Corto");
        page.Find("form.side-panel-form").Submit();

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/universes/{Corto.Id}", "Corto", "Les voyages du marin.", PrattItem.Id.ToString(), 42u),
            (written.Method, written.Path, written.Body.GetProperty("name").GetString(), written.Body.GetProperty("description").GetString(),
                written.Body.GetProperty("parentId").GetString(), written.Body.GetProperty("version").GetUInt32()));
        Assert.EndsWith($"{AppPaths.AdminUniverses}?q=c", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.universe.saved", _audience.Functions);
    }

    [Fact]
    public void Panel_AParentThatWouldMakeACycle_IsRefusedInPlace_WithItsReason()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Universe.HierarchyCycle");
        var page = Open($"edit={Corto.Id}");

        page.Find("form.side-panel-form").Submit();

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(
            "Un univers ne peut pas être placé sous lui-même, ni sous l'un de ses propres sous-univers.",
            Text(panel.QuerySelector(".record-picker")!.ParentElement!.QuerySelector(".field-error")!));
        Assert.Equal("true", panel.QuerySelector(".record-picker input[role='combobox']")!.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Panel_ANameRequired_IsRefusedAtTheName()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Universe.NameRequired");
        var page = Open("new=open");

        page.Find("form.side-panel-form").Submit();

        Assert.Equal("true", page.Find("input[name='name']").GetAttribute("aria-invalid"));
        Assert.Equal("Le nom de l'univers est obligatoire.", Text(page.Find("input[name='name']").ParentElement!.QuerySelector(".field-error")!));
    }

    [Fact]
    public void Panel_CreatesTheUniverse_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/universes/{Corto.Id}"], HttpStatusCode.Created);
        var page = Open("new=open", recorded: true);

        page.Find("input[name='name']").Change("Les Scorpions du désert");
        page.Find("form.side-panel-form").Submit();

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Post, "/admin/universes", "Les Scorpions du désert"), (written.Method, written.Path, written.Body.GetProperty("name").GetString()));
        Assert.EndsWith(AppPaths.AdminUniverses, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.universe.created", _audience.Functions);
    }

    [Fact]
    public void Panel_OfAUniverseDeletedMeanwhile_IsNotOpened()
    {
        var page = Open($"edit={Guid.NewGuid()}");

        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal(4, page.FindAll(".universe-row:not(.universe-row-head)").Count);
    }

    [Fact]
    public void Empty_ASearchWithoutResult_OffersToCreateTheUniverseSearched_InThePanel()
    {
        Answer([], totalCount: 0);

        var link = Open("q=Mu").Find(".empty-list a.button[data-variant='primary']");
        Assert.Equal(("Créer l'univers « Mu »", $"{AppPaths.AdminUniverses}?q=Mu&new=open"), (Text(link), link.GetAttribute("href")));

        Assert.Equal("Mu", Open("q=Mu&new=open").Find("input[name='name']").GetAttribute("value"));
    }

    [Fact]
    public void Deletion_RefusedWhileTheUniverseHasSubUniverses_SaysWhatToDo()
    {
        var id = Pratt.Id;
        _api.Answers[$"/admin/universes/{id}/deletion-impact"] = new DeletionImpact([new ImpactCount(EntityKind.Universe, 3)], [], [], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Suppression impossible", "Univers Hugo Pratt"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Contains("3 univers", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Equal(
            ["Rangez d'abord ses sous-univers sous un autre univers, ou supprimez-les, puis recommencez."],
            dialog.QuerySelectorAll(".dialog-note").Select(Text));
    }

    [Fact]
    public void Deletion_OfAUniverseWithoutSubUniverse_AnnouncesTheAlbumsAndSeriesItLeaves_AndConfirmsWithItsVersion()
    {
        var id = Antilles.Id;
        _api.Answers[$"/admin/universes/{id}/deletion-impact"] = new DeletionImpact([], [new ImpactCount(EntityKind.Album, 4)], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Supprimer cet univers ?", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Contains("4 albums", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Equal(
            [("Version", "43"), ("Fingerprint", "f2")],
            dialog.QuerySelectorAll("form[method='post'] input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherUniverse_EachCandidateWithItsPlaceInTheHierarchy()
    {
        var dialog = Open($"merge={Cities.Id}&mergeQ=o").Find("dialog");

        Assert.Equal("Fusionner deux univers", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [
                ("Univers Hugo Pratt", "31 albums"),
                ("Corto Maltese", "Dans Univers Hugo Pratt · 18 albums"),
                ("Les Antilles", "Dans Univers Hugo Pratt › Corto Maltese · 4 albums"),
            ],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        Assert.Equal(AppPaths.AdminMergeUniverses, dialog.QuerySelector("#universe-merge-continue")!.GetAttribute("action"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/universes"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".universe-row"));
    }

    [Fact]
    public void APageBeyondTheLast_LeadsToTheLastOne()
    {
        _api.Answers["/admin/universes"] = new Page<UniverseAdminListItem>([], 3, AdminListQuery.PageSize, 60);

        Open("q=o&page=3");

        Assert.EndsWith($"{AppPaths.AdminUniverses}?q=o&page=2", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Audience_TheActionOfItsAddress_ThenTheScreen()
    {
        Open("new=open", recorded: true);

        Assert.Equal(["admin.universes.new", "admin.universes"], _audience.Functions);
    }

    private static UniverseAdminListItem Row(UniverseListItem universe, IReadOnlyList<UniverseListItem> ancestors, int albums, int series, int subUniverses, uint version) =>
        new(universe.Id, universe.Name, universe.ParentId, ancestors, albums, series, subUniverses, January, version);

    private IRenderedComponent<AdminUniverses> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminUniverses}?{criteria}");
        return Render<AdminUniverses>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<UniverseAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/universes"] = new Page<UniverseAdminListItem>(items, 1, AdminListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
