using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the authors of the administration (visuel/listes/liste-par-liste.md): its rows, its count, its search without
/// filters, its dialogs, the side panel of an author over the list (visuel/listes/saisie-selon-l-entite.md) and its errors,
/// told apart by role, link and text, never by appearance. What it shares with the list of the albums (active filters,
/// navigation, page beyond the last) is pinned by AdminAlbumsTests.
/// </summary>
public sealed class AdminAuthorsTests : BunitContext, IDisposable
{
    private static readonly AuthorAdminListItem Gibelin = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), "Gibelin", "Christophe", null, "Française", 14, [ContributionRole.Scenarist],
        new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero), 41);

    private static readonly AuthorAdminListItem Gibrat = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), "Gibrat", "Jean-Pierre", null, "Française", 1,
        [ContributionRole.Scenarist, ContributionRole.Illustrator, ContributionRole.Colorist], new DateTimeOffset(2026, 4, 19, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly AuthorAdminListItem Gomez = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), "Gomez", "Alain", null, null, 0, [],
        new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero), 43);

    private static readonly AuthorAdminListItem Gotlib = new(
        Guid.Parse("01900000-0000-7000-8000-000000000004"), "Gotlieb", "Marcel", "Gotlib", "Française", 22,
        [ContributionRole.Scenarist, ContributionRole.Illustrator], new DateTimeOffset(2026, 3, 30, 8, 0, 0, TimeSpan.Zero), 44);

    private static readonly DateTimeOffset January = new(2026, 1, 12, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset March = new(2026, 3, 30, 8, 0, 0, TimeSpan.Zero);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminAuthorsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        Answer([Gibelin, Gibrat, Gomez, Gotlib], totalCount: 38);
        _api.Answers[$"/admin/authors/{Gotlib.Id}"] = new AuthorForm(
            Gotlib.Id, "Gotlieb", "Marcel", "Gotlib", "Créateur de Gai-Luron.", "Française", January, March, 44);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfAuthorsAndTheCreation()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Auteurs"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Auteurs", "1 106"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Equal(AppPaths.AdminNewAuthor, page.Find(".list-heading a.button").GetAttribute("href"));
    }

    [Fact]
    public void Search_OnTheNameTheFirstNameOrThePseudonym_WithoutFilters()
    {
        var form = Open("q=gib").Find("form.list-search");

        Assert.Equal(("q", "gib", "Nom, prénom ou pseudonyme…"),
            (form.QuerySelector("input[type='search']")!.GetAttribute("name"), form.QuerySelector("input[type='search']")!.GetAttribute("value"),
                form.QuerySelector("input[type='search']")!.GetAttribute("placeholder")));
        Assert.Null(form.QuerySelector("button[aria-controls]"));
        Assert.Empty(Open("q=gib").FindAll(".active-filters .tag"));
    }

    [Fact]
    public void Rows_TheAuthorItsNationalityItsAlbumsAndItsRoles()
    {
        var rows = Open("").FindAll(".author-row:not(.author-row-head)").Select(row => (
            Text(row.QuerySelector(".row-entry")!),
            row.QuerySelector(".author-identity") is { } identity ? Text(identity) : null,
            Text(row.QuerySelector(".author-nationality")!),
            Text(row.QuerySelector(".author-albums")!),
            Text(row.QuerySelector(".author-roles")!)));

        Assert.Equal(
            [
                ("Christophe Gibelin", null, "Française", "14 albums", "Scénario"),
                ("Jean-Pierre Gibrat", null, "Française", "1 album", "Scénario · Dessin · Couleurs"),
                // No album credited: a dash, said in full to a screen reader.
                ("Alain Gomez", null, "—Non renseignée", "—Aucun album", "—Aucun rôle"),
                // Under a pseudonym, the first name and the last name below it.
                ("Gotlib", "Marcel Gotlieb", "Française", "22 albums", "Scénario · Dessin"),
            ],
            rows);
    }

    [Fact]
    public void Rows_ADataThatIsMissing_ADashHiddenFromScreenReaders_WhichAreToldItsMention()
    {
        // Gomez: no nationality, credited on no album, hence no role (visuel/charte-graphique/convivialite-et-efficacite.md).
        var gomez = Open("").FindAll(".author-row:not(.author-row-head)")[2];

        Assert.Equal(
            [("—", "Non renseignée"), ("—", "Aucun album"), ("—", "Aucun rôle")],
            new[] { ".author-nationality", ".author-albums", ".author-roles" }.Select(cell => SeenAndHeard(gomez.QuerySelector(cell)!)));
    }

    // What the eye reads in a cell, then what a screen reader reads of it: each skips what is hidden from it.
    private static (string Seen, string Heard) SeenAndHeard(IElement cell) => (
        string.Concat(cell.QuerySelectorAll(".not-set > :not(.visually-hidden)").Select(part => part.TextContent)),
        string.Concat(cell.QuerySelectorAll(".not-set > :not([aria-hidden='true'])").Select(part => part.TextContent)));

    [Fact]
    public void Rows_AMonogramOfItsInitials_InTheToneOfItsAuthor_WhateverItsPlace()
    {
        var monograms = Open("").FindAll(".author-row:not(.author-row-head) .monogram")
            .Select(monogram => (monogram.TextContent, monogram.GetAttribute("class"))).ToList();
        Answer([Gotlib, Gomez, Gibrat, Gibelin], totalCount: 38);
        var reversed = Open("").FindAll(".author-row:not(.author-row-head) .monogram")
            .Select(monogram => (monogram.TextContent, monogram.GetAttribute("class")));

        Assert.Equal(
            [("CG", "monogram monogram-tone-4"), ("JG", "monogram monogram-tone-6"), ("AG", "monogram monogram-tone-2"), ("G", "monogram monogram-tone-4")],
            monograms);
        // In another order, each author keeps its tone.
        Assert.Equal(Enumerable.Reverse(monograms), reversed);
    }

    [Fact]
    public void Rows_LeadToTheConsultation_AndOpenTheirPanelAndTheirDialogsInTheList()
    {
        var row = Open("q=gib").FindAll(".author-row:not(.author-row-head)")[3];
        var id = Gotlib.Id;

        // « Modifier » alone opens the record: the entry of the row is no link (visuel/liste-albums/contenu.md, Actions d'une ligne).
        Assert.Equal(["Modifier Gotlib"], row.QuerySelectorAll($"a[href='{AppPaths.AdminAuthors}?q=gib&edit={id}']").Select(link => link.GetAttribute("aria-label")));
        Assert.Equal($"{AppPaths.AdminAuthors}?q=gib&edit={id}", row.QuerySelector(".author-actions a.button")!.GetAttribute("href"));
        Assert.Equal("Modifier Gotlib", row.QuerySelector(".author-actions a.button")!.GetAttribute("aria-label"));
        Assert.Equal(
            [
                ("Voir dans la consultation", AppPaths.AuthorRecord(id)),
                ("Fusionner avec un autre auteur…", $"{AppPaths.AdminAuthors}?q=gib&merge={id}"),
                ("Supprimer…", $"{AppPaths.AdminAuthors}?q=gib&delete={id}"),
            ],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"))));
    }

    [Theory]
    [InlineData("", "38 auteurs")]
    [InlineData("entry=G", "38 auteurs sous la lettre G")]
    [InlineData("q=gib", "38 auteurs sur 1 106")]
    public void Summary_TheCountUnderTheEntryOrOutOfAllTheAuthors_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Triés par nom, le pseudonyme tenant lieu de nom", Text(summary.Children[1]));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearItOrToCreateTheAuthor()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=Zorglub&entry=Z").Find(".empty-list");

        Assert.Equal("Aucun auteur ne correspond à « Zorglub ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal(
            [("Effacer la recherche", $"{AppPaths.AdminAuthors}?entry=Z"), ("Créer l'auteur « Zorglub »", $"{AppPaths.AdminAuthors}?q=Zorglub&entry=Z&new=open")],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Theory]
    [InlineData("Zorglub", "", "Zorglub")]
    [InlineData("Jean Van Hamme", "Jean", "Van Hamme")]
    public void Empty_TheAuthorSearched_IsTheNewAuthorOfThePanel_ItsNameSplitAsWhenCreatedOnTheSpot(string searched, string firstName, string lastName)
    {
        Answer([], totalCount: 0);

        var panel = Open($"q={Uri.EscapeDataString(searched)}&new=open").Find("dialog.side-panel");

        Assert.Equal((firstName, lastName),
            (panel.QuerySelector("input[name='firstName']")!.GetAttribute("value") ?? "", panel.QuerySelector("input[name='lastName']")!.GetAttribute("value")));
    }

    [Fact]
    public void Empty_AnEntryWithoutAuthor_OffersAllTheAuthors()
    {
        Answer([], totalCount: 0);

        var empty = Open("entry=X").Find(".empty-list");

        Assert.Equal("Aucun auteur sous cette entrée.", Text(empty.QuerySelector("strong")!));
        Assert.Equal([("Tous les auteurs", AppPaths.AdminAuthors)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_WithoutAnyCriterion_SaysThereIsNoAuthorYet_AndOffersToCreateOne_WithNothingToClear()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucun auteur n'est encore enregistré.", Text(empty.QuerySelector("strong")!));
        Assert.Equal([("Nouvel auteur", $"{AppPaths.AdminAuthors}?new=open")], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Deletion_ADialogThatAnnouncesTheImpact_AndConfirmsWithTheVersionAndTheFingerprint()
    {
        var id = Gomez.Id;
        _api.Answers[$"/admin/authors/{id}/deletion-impact"] = new DeletionImpact([], [], [], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Supprimer cet auteur ?", "Alain Gomez"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Equal("Aucune autre fiche n'est concernée.", Text(dialog.QuerySelector(".deletion-impact")!));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "43"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
    }

    [Fact]
    public void Deletion_RefusedWhileTheAuthorIsCredited_SaysWhatToDo()
    {
        var id = Gotlib.Id;
        _api.Answers[$"/admin/authors/{id}/deletion-impact"] = new DeletionImpact(
            [new ImpactCount(EntityKind.Album, 22), new ImpactCount(EntityKind.Series, 2)], [], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Contains("22 albums", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Equal(
            [
                "Retirez d'abord l'auteur des contributions de ces albums, ou fusionnez-le avec un autre auteur, puis recommencez.",
                "Retirez d'abord l'auteur des auteurs de ces séries, ou fusionnez-le avec un autre auteur, puis recommencez.",
            ],
            dialog.QuerySelectorAll(".dialog-note").Select(Text));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherAuthor_AndContinuesWithTheOneChosen()
    {
        var dialog = Open($"merge={Gotlib.Id}&mergeQ=g").Find("dialog");

        Assert.Equal(("Fusionner deux auteurs", "Gotlib"), (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Equal(
            [
                ("Christophe Gibelin", "Française · 14 albums · Scénario"),
                ("Jean-Pierre Gibrat", "Française · 1 album · Scénario · Dessin · Couleurs"),
                ("Alain Gomez", "Aucun album"),
            ],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        var next = dialog.QuerySelector("#author-merge-continue")!;
        Assert.Equal(AppPaths.AdminMergeAuthors, next.GetAttribute("action"));
        Assert.Equal(Gotlib.Id.ToString(), next.QuerySelector("input[name='author']")!.GetAttribute("value"));
        Assert.Contains(_api.Requested, path => path == "/admin/authors?q=g&pageSize=11");
    }

    [Fact]
    public void Panel_OfAnAuthor_ItsDatesAtTheTopThenItsFields()
    {
        var panel = Open($"q=gib&edit={Gotlib.Id}").Find("dialog.side-panel");

        Assert.Equal("Modifier l'auteur", Text(panel.QuerySelector("h2")!));
        Assert.Equal(
            [("firstName", "Marcel"), ("lastName", "Gotlieb"), ("pseudonym", "Gotlib"), ("nationality", "Française")],
            panel.QuerySelectorAll("input[type='text']").Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
        Assert.Equal("Créateur de Gai-Luron.", ((AngleSharp.Html.Dom.IHtmlTextAreaElement)panel.QuerySelector("textarea[name='biography']")!).Value);
        Assert.Equal("Auteur créé le 12/01/2026 10:30, modifié le 30/03/2026 10:00.", Text(panel.QuerySelector(".side-panel-body > :first-child")!));
        Assert.Equal(
            [("Annuler", $"{AppPaths.AdminAuthors}?q=gib"), ("Enregistrer", null)],
            panel.QuerySelectorAll(".side-panel-actions .button").Select(button => (Text(button), button.GetAttribute("href"))));
    }

    [Fact]
    public void Panel_OfANewAuthor_EmptyWithoutDates()
    {
        var panel = Open("entry=G&new=open").Find("dialog.side-panel");

        Assert.Equal("Nouvel auteur", Text(panel.QuerySelector("h2")!));
        Assert.All(panel.QuerySelectorAll("input[type='text']"), input => Assert.Null(input.GetAttribute("value")));
        Assert.Null(panel.QuerySelector(".side-panel-dates"));
        Assert.Equal(
            [("Annuler", $"{AppPaths.AdminAuthors}?entry=G"), ("Créer l'auteur", null)],
            panel.QuerySelectorAll(".side-panel-actions .button").Select(button => (Text(button), button.GetAttribute("href"))));
    }

    [Fact]
    public void Panel_SavesTheAuthorWithItsVersion_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/authors/{Gotlib.Id}"]);
        var page = Open($"q=gib&edit={Gotlib.Id}", recorded: true);

        page.Find("input[name='nationality']").Change("Belge");
        page.Find("form.side-panel-form").Submit();

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/authors/{Gotlib.Id}", "Gotlieb", "Marcel", "Gotlib", "Belge", "Créateur de Gai-Luron.", 44u),
            (written.Method, written.Path, written.Body.GetProperty("lastName").GetString(), written.Body.GetProperty("firstName").GetString(),
                written.Body.GetProperty("pseudonym").GetString(), written.Body.GetProperty("nationality").GetString(),
                written.Body.GetProperty("biography").GetString(), written.Body.GetProperty("version").GetUInt32()));
        Assert.EndsWith($"{AppPaths.AdminAuthors}?q=gib", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.author.saved", _audience.Functions);
    }

    [Fact]
    public void Panel_CreatesTheAuthor_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/authors/{Gotlib.Id}"], HttpStatusCode.Created);
        var page = Open("new=open", recorded: true);

        page.Find("form.side-panel-form").Submit();

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Post, "/admin/authors"), (written.Method, written.Path));
        Assert.EndsWith(AppPaths.AdminAuthors, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.author.created", _audience.Functions);
    }

    [Fact]
    public void Panel_AnAuthorWithoutLastNameNorPseudonym_IsRefusedAtBothFields()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Author.LastNameOrPseudonymRequired");
        var page = Open("new=open");

        page.Find("form.side-panel-form").Submit();

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(("true", "true"), (panel.QuerySelector("input[name='lastName']")!.GetAttribute("aria-invalid"),
            panel.QuerySelector("input[name='pseudonym']")!.GetAttribute("aria-invalid")));
        Assert.Equal("Un auteur a au moins un nom ou un pseudonyme : renseignez l'un ou l'autre.", Text(panel.QuerySelector(".field-error")!));
        Assert.StartsWith("author-last-name-error", panel.QuerySelector("input[name='pseudonym']")!.GetAttribute("aria-describedby"));
        Assert.Empty(panel.QuerySelectorAll("[data-error-category]"));
    }

    [Theory]
    [InlineData("", "Gotlib", "pseudonym", "« Gotlib » est déjà le nom d'un autre auteur")]
    [InlineData("Marcel", "", "lastName", "« Marcel Gotlieb » est déjà le nom d'un autre auteur")]
    public void Panel_ADisplayNameAlreadyTaken_IsRefusedAtTheFieldItComesFrom(string firstName, string pseudonym, string field, string message)
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Author.DisplayNameAlreadyUsed");
        var page = Open("new=open");
        page.Find("input[name='firstName']").Change(firstName);
        page.Find("input[name='lastName']").Change("Gotlieb");
        page.Find("input[name='pseudonym']").Change(pseudonym);

        page.Find("form.side-panel-form").Submit();

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(["true"], panel.QuerySelectorAll("[aria-invalid]").Select(input => input.GetAttribute("aria-invalid")));
        Assert.Equal(field, panel.QuerySelector("[aria-invalid]")!.GetAttribute("name"));
        Assert.StartsWith(message, Text(panel.QuerySelector(".field-error")!));
        Assert.Empty(panel.QuerySelectorAll("[data-error-category]"));
    }

    [Fact]
    public void Panel_AnAuthorChangedMeanwhile_IsPresentedAtTheTopOfThePanel()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        var page = Open($"edit={Gotlib.Id}");

        page.Find("form.side-panel-form").Submit();

        Assert.Equal("functional", page.Find("dialog.side-panel [data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll("dialog.side-panel [aria-invalid]"));
    }

    [Fact]
    public void Panel_OfAnAuthorDeletedMeanwhile_IsNotOpened()
    {
        var page = Open($"edit={Guid.NewGuid()}");

        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal(4, page.FindAll(".author-row:not(.author-row-head)").Count);
    }

    [Fact]
    public void Audience_ThePanelOpened_ThenTheScreen()
    {
        Open($"edit={Gotlib.Id}", recorded: true);

        Assert.Equal(["admin.authors.edit", "admin.authors"], _audience.Functions);
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/authors"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".author-row"));
    }

    [Fact]
    public void Counts_NotGivenByTheApi_LeaveTheListWithoutItsNumber()
    {
        _api.Failures["/admin/record-counts"] = HttpStatusCode.InternalServerError;

        var page = Open("q=gib");

        Assert.Empty(page.FindAll(".list-heading-count"));
        Assert.Equal("38 auteurs", Text(page.Find(".list-summary").Children[0]));
        Assert.Equal(4, page.FindAll(".author-row:not(.author-row-head)").Count);
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("q=gib&entry=G", recorded: true);

        // The action of the list first, which displays the screen.
        Assert.Equal(["admin.authors.initial", "admin.authors"], _audience.Functions);
    }

    private IRenderedComponent<AdminAuthors> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminAuthors}?{criteria}");
        return Render<AdminAuthors>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<AuthorAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/authors"] = new Page<AuthorAdminListItem>(items, 1, AdminListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
