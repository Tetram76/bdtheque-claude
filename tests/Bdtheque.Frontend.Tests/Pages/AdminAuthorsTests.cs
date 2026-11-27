using System.Globalization;
using System.Net;
using System.Text.Json;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Components.Lists;
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
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "planche", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminAuthorsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        // The pages of the administration are only ever served to the authenticated administrator (Pages/Admin/_Imports.razor).
        AddAuthorization().SetAuthorized("admin");
        Services.AddSingleton(TimeProvider.System);
        // The panel of an author is rendered by its circuit (InteractiveServer), which its frame marks; the error of its saving
        // imports the script that reveals it, which only the test of the focus sets up.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
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
            row.QuerySelector(".row-entry-detail") is { } identity ? Text(identity) : null,
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
            [SeenAndHeard(gomez, ".author-nationality"), SeenAndHeard(gomez, ".author-albums"), SeenAndHeard(gomez, ".author-roles")]);
    }

    [Fact]
    public void Rows_MarkAnAuthorCreditedOnNoAlbum_ByThePresenceOfTheAttribute()
    {
        // A boolean attribute of Blazor has no value: present when true, absent when false, which the styles select on: on
        // two lines, a zero is withdrawn (visuel/charte-graphique/convivialite-et-efficacite.md, nombre nul).
        var rows = Open("").FindAll(".author-row:not(.author-row-head)").Select(row => row.QuerySelector(".author-albums")!.HasAttribute("data-zero"));

        Assert.Equal([false, false, true, false], rows);
    }

    // What the eye reads in a cell of a row, then what a screen reader reads of it: each skips what is hidden from it.
    private static (string Seen, string Heard) SeenAndHeard(IElement row, string cellSelector) => (
        string.Concat(row.QuerySelectorAll($"{cellSelector} .not-set > :not(.visually-hidden)").Select(part => part.TextContent)),
        string.Concat(row.QuerySelectorAll($"{cellSelector} .not-set > :not([aria-hidden='true'])").Select(part => part.TextContent)));

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
        Assert.Equal("La recherche porte sur le nom, le prénom et le pseudonyme de l'auteur, sans tenir compte des majuscules ni des accents.", Text(empty.QuerySelector("strong + span")!));
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
            (panel.QuerySelector("#author-first-name")!.GetAttribute("value") ?? "", panel.QuerySelector("#author-last-name")!.GetAttribute("value")));
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
        Assert.Equal("La suppression est définitive.", Text(dialog.QuerySelector(".dialog-body > p")!));
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
            [new ImpactCount(EntityKind.Series, 2), new ImpactCount(EntityKind.Album, 22)], [], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal("L'auteur est encore utilisé par :", Text(dialog.QuerySelector(".dialog-body > p")!));
        Assert.Equal(
            [
                "2 séries : retirez l'auteur de leurs auteurs, ou fusionnez-le avec un autre auteur ;",
                "22 albums : retirez l'auteur de leurs contributions, ou fusionnez-le avec un autre auteur.",
            ],
            dialog.QuerySelectorAll(".deletion-refusal li").Select(Text));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    // fonctionnel/regles-metier/fusion-des-fiches.md, fiches pressenties: before any search, the dialog proposes the records the
    // API finds resembling the record, as the dialog of every list does (pinned by AdminGenresTests).
    [Fact]
    public void Merge_BeforeAnySearch_ProposesTheRecordsThatResembleIt()
    {
        var path = $"/admin/authors?resembling={Gotlib.Id}&pageSize={MergeDialog.SuggestionCount}";
        _api.Answers[path] = new Page<AuthorAdminListItem>([Gomez], 1, MergeDialog.SuggestionCount, 1);

        var dialog = Open($"merge={Gotlib.Id}").Find("dialog");

        Assert.Equal("Auteurs pressentis", Text(dialog.QuerySelector(".merge-suggestions")!));
        Assert.Equal(["Alain Gomez"], dialog.QuerySelectorAll(".merge-candidate strong").Select(Text));
        Assert.Contains("Ou recherchez l'autre auteur par son nom, son prénom ou son pseudonyme.", dialog.QuerySelectorAll(".dialog-note").Select(Text));
        Assert.Contains(_api.Requested, requested => requested == path);
        Assert.Equal("true", dialog.QuerySelector("#author-merge-continue input[name='suggested']")!.GetAttribute("value"));
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
        var page = Open($"q=gib&edit={Gotlib.Id}");
        var panel = page.Find("dialog.side-panel");

        Assert.Equal("Modifier l'auteur", Text(panel.QuerySelector("h2")!));
        Assert.Equal(
            [("author-first-name", "Marcel"), ("author-last-name", "Gotlieb"), ("author-pseudonym", "Gotlib"), ("author-nationality", "Française")],
            panel.QuerySelectorAll("input[type='text']").Select(input => (input.Id, input.GetAttribute("value"))));
        Assert.Equal("Créateur de Gai-Luron.", panel.QuerySelector("#author-biography")!.GetAttribute("value"));
        Assert.Equal("Auteur créé le 12/01/2026 10:30, modifié le 30/03/2026 10:00.", Text(panel.QuerySelector(".side-panel-body > :first-child")!));
        Assert.Equal(
            [("Annuler", $"{AppPaths.AdminAuthors}?q=gib"), ("Enregistrer", null)],
            panel.QuerySelectorAll(".side-panel-actions .button").Select(button => (Text(button), button.GetAttribute("href"))));
        // The first field gets the focus at the opening, declared in the keyboard path: without it, Blazor, which focuses the
        // field after an enhanced navigation, would take it out of that path (#443).
        Assert.Equal([("author-first-name", "0")], panel.QuerySelectorAll("[autofocus]").Select(input => (input.Id, input.GetAttribute("tabindex"))));
        // Interactive once its circuit has rendered it, which it marks.
        Assert.Equal("true", page.Find("form.side-panel-form").GetAttribute("data-interactive"));
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
    public async Task Panel_SaveInProgress_DisablesItsFieldsAndItsSaving_UntilItEnds()
    {
        // A field changed during the saving would be lost, the panel leaving once the author it sent is saved.
        var saved = new TaskCompletionSource();
        _api.Hold = (_, body) => body.ValueKind == JsonValueKind.Undefined ? Task.CompletedTask : saved.Task;
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);
        var page = Open($"q=gib&edit={Gotlib.Id}", recorded: true);
        await page.InputAsync("#author-nationality", "Belge");

        var saving = await page.StartSubmitAsync("form.side-panel-form");

        Assert.True(page.Find(".entry-panel-fields").HasAttribute("disabled"));
        Assert.True(page.Find(".side-panel-actions button[type=submit]").HasAttribute("disabled"));
        saved.SetResult();
        await saving.EndAsync("The saving");
        Assert.False(page.Find(".entry-panel-fields").HasAttribute("disabled"));
    }
    [Fact]
    public async Task Panel_SavesTheAuthorWithItsVersion_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/authors/{Gotlib.Id}"]);
        var page = Open($"q=gib&edit={Gotlib.Id}", recorded: true);

        await page.InputAsync("#author-nationality", "Belge");
        await page.SubmitAsync("form.side-panel-form");

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Put, $"/admin/authors/{Gotlib.Id}", "Gotlieb", "Marcel", "Gotlib", "Belge", "Créateur de Gai-Luron.", 44u),
            (written.Method, written.Path, written.Body.GetProperty("lastName").GetString(), written.Body.GetProperty("firstName").GetString(),
                written.Body.GetProperty("pseudonym").GetString(), written.Body.GetProperty("nationality").GetString(),
                written.Body.GetProperty("biography").GetString(), written.Body.GetProperty("version").GetUInt32()));
        Assert.EndsWith($"{AppPaths.AdminAuthors}?q=gib", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.author.saved", _audience.Functions);
    }

    [Fact]
    public async Task Panel_CreatesTheAuthor_ThenLeadsBackToTheList()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/authors/{Gotlib.Id}"], HttpStatusCode.Created);
        var page = Open("new=open", recorded: true);

        await page.SubmitAsync("form.side-panel-form");

        var written = Assert.Single(_api.Written);
        Assert.Equal((HttpMethod.Post, "/admin/authors"), (written.Method, written.Path));
        Assert.EndsWith(AppPaths.AdminAuthors, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains("admin.author.created", _audience.Functions);
    }

    // As on an entry form (visuel/listes/saisie-selon-l-entite.md, #411): the error at the top of the panel, the field it
    // designates bordered with its text under it, and brought into view, focused, by the script of the entry forms.
    [Fact]
    public async Task Panel_AnAuthorWithoutLastNameNorPseudonym_IsRefusedAtBothFields()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Author.LastNameOrPseudonymRequired", "/lastName");
        var reveal = JSInterop.SetupModule(module => module.Identifier == "import" && ((string)module.Arguments[0]!).EndsWith("entry-form.js"))
            .SetupVoid("revealProblem", _ => true);
        var page = Open("new=open");

        await page.SubmitAsync("form.side-panel-form");

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(("true", "true"), (panel.QuerySelector("#author-last-name")!.GetAttribute("aria-invalid"),
            panel.QuerySelector("#author-pseudonym")!.GetAttribute("aria-invalid")));
        Assert.Equal("Au moins le nom ou le pseudonyme.", Text(panel.QuerySelector(".field-error")!));
        Assert.StartsWith("author-last-name-error", panel.QuerySelector("#author-pseudonym")!.GetAttribute("aria-describedby"));
        Assert.Equal(("business", "Saisie à corriger"), ErrorOf(panel));
        Assert.Single(reveal.Invocations);
    }

    [Theory]
    [InlineData("", "Gotlib", "pseudonym", "« Gotlib » est déjà pris : deux noms qui ne diffèrent que par la casse ou les accents désignent le même auteur.")]
    [InlineData("Marcel", "", "lastName", "« Marcel Gotlieb » est déjà pris : deux noms qui ne diffèrent que par la casse ou les accents désignent le même auteur.")]
    public async Task Panel_ADisplayNameAlreadyTaken_IsRefusedAtTheFieldItComesFrom(string firstName, string pseudonym, string field, string message)
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(
            ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Author.DisplayNameAlreadyUsed", $"/{field}");
        var page = Open("new=open");
        await page.InputAsync("#author-first-name", firstName);
        await page.InputAsync("#author-last-name", "Gotlieb");
        await page.InputAsync("#author-pseudonym", pseudonym);

        await page.SubmitAsync("form.side-panel-form");

        var panel = page.Find("dialog.side-panel");
        Assert.Equal([field == "pseudonym" ? "author-pseudonym" : "author-last-name"], panel.QuerySelectorAll("[aria-invalid]").Select(input => input.Id));
        Assert.StartsWith(message, Text(panel.QuerySelector(".field-error")!));
        Assert.Equal(("business", "Saisie à corriger"), ErrorOf(panel));
    }

    // A text too long is refused at its own field, whichever it is.
    [Fact]
    public async Task Panel_ATextTooLong_IsRefusedAtItsField()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Business, HttpStatusCode.UnprocessableEntity, "Text.TooLong", "/nationality");
        var page = Open($"edit={Gotlib.Id}");

        await page.SubmitAsync("form.side-panel-form");

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(["author-nationality"], panel.QuerySelectorAll("[aria-invalid]").Select(input => input.Id));
        Assert.Equal("Trop long : raccourcissez ce texte.", Text(panel.QuerySelector(".field-error")!));
    }

    // Changed or deleted meanwhile: nothing overwritten nor saved, what was typed staying in the panel until the author is
    // reloaded, within the panel.
    [Theory]
    [InlineData(HttpStatusCode.Conflict, "Fiche modifiée entre-temps", "L'auteur a été modifié entre-temps, depuis un autre onglet ou un autre appareil : rien n'a été écrasé. Rechargez la fiche pour partir de son état actuel ; votre saisie reste dans le panneau jusque-là.")]
    [InlineData(HttpStatusCode.NotFound, "Fiche supprimée entre-temps", "L'auteur a été supprimé entre-temps : rien n'a été enregistré.")]
    public async Task Panel_AnAuthorChangedOrDeletedMeanwhile_OffersToReloadIt(HttpStatusCode status, string title, string text)
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, status);
        var page = Open($"q=gib&edit={Gotlib.Id}");
        await page.InputAsync("#author-nationality", "Belge");

        await page.SubmitAsync("form.side-panel-form");

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(("functional", title), ErrorOf(panel));
        Assert.Equal(text, Text(panel.QuerySelector("[data-error-text]")!));
        // « Voir mes changements » is not there: what was typed stays in front of the user (visuel/listes/saisie-selon-l-entite.md).
        Assert.Equal(["Recharger la fiche"], panel.QuerySelectorAll(".error-actions .button").Select(Text));
        Assert.Equal("Belge", panel.QuerySelector("#author-nationality")!.GetAttribute("value"));
        Assert.Empty(panel.QuerySelectorAll("[aria-invalid]"));
    }

    // A technical error: the same mention as any other, and the author saved again with what was typed.
    [Fact]
    public async Task Panel_ATechnicalError_OffersToSaveAgain()
    {
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Technical, HttpStatusCode.InternalServerError);
        var page = Open("new=open");
        await page.InputAsync("#author-last-name", "Hugo");

        await page.SubmitAsync("form.side-panel-form");

        var panel = page.Find("dialog.side-panel");
        Assert.Equal(("technical", "Problème technique"), ErrorOf(panel));
        Assert.Equal(["Réessayer"], panel.QuerySelectorAll(".error-actions .button").Select(Text));

        await page.ClickAsync(".side-panel .error-actions .button");

        Assert.Equal(2, _api.Written.Count);
        Assert.Equal("Hugo", _api.Written[1].Body.GetProperty("lastName").GetString());
    }

    // visuel/saisie-album/contenu.md, données modifiées (#355): the mark compares what is typed to what was read, as it is typed.
    [Fact]
    public async Task Datum_ChangedSinceTheReading_IsMarkedAfterItsLabel_UntilSetBackToWhatWasRead()
    {
        var page = Open($"edit={Gotlib.Id}");
        Assert.Empty(page.FindAll(".modified-mark"));

        await page.InputAsync("#author-nationality", "Belge");

        Assert.Equal("Modifié", Text(page.Find("label[for=author-nationality] .modified-mark")));
        Assert.Single(page.FindAll(".modified-mark"));

        await page.InputAsync("#author-nationality", "Française");

        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task NewAuthor_HasNoMark_AllOfItBeingNew()
    {
        var page = Open("new=open");

        await page.InputAsync("#author-last-name", "Hugo");
        await page.InputAsync("#author-biography", "Poète.");

        Assert.Empty(page.FindAll(".modified-mark"));
    }

    // visuel/saisie-album/contenu.md (#329): the author reloaded within the panel, what the user had typed offered again under
    // each datum where it differs from the author reloaded, and there only.
    [Fact]
    public async Task Reload_AfterAModificationMeanwhile_OffersUnderEachDatum_OnlyWhatDiffersFromTheAuthorReloaded()
    {
        var page = Open($"edit={Gotlib.Id}");
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        await page.InputAsync("#author-nationality", "Belge");
        await page.InputAsync("#author-biography", "Créateur de Gaston.");
        await page.SubmitAsync("form.side-panel-form");
        // The other modification typed the same biography, and a pseudonym the user did not touch.
        _api.Answers[$"/admin/authors/{Gotlib.Id}"] = new AuthorForm(
            Gotlib.Id, "Gotlieb", "Marcel", "Gotlib le grand", "Créateur de Gaston.", "Française", January, March, 45);

        await page.ClickAsync(".side-panel .error-actions .button");

        Assert.Empty(page.FindAll(".side-panel [data-error-category]"));
        Assert.Equal(("Française", "Gotlib le grand"),
            (page.Find("#author-nationality").GetAttribute("value"), page.Find("#author-pseudonym").GetAttribute("value")));
        var line = page.Find("#author-nationality-redo");
        Assert.Equal("Votre saisie : Belge", Text(line.QuerySelector("span")!));
        Assert.Equal(["Reprendre", "Rejeter"], line.QuerySelectorAll("button").Select(Text));
        Assert.Single(page.FindAll(".redo-line"));
        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task Taking_WhatWasTyped_PutsItBack_TheDatumThenMarked_AndSavedUnderTheVersionReloaded()
    {
        var page = await ReloadedWithANationalityTypedAsync();
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/authors/{Gotlib.Id}"]);

        await page.ClickAsync(() => page.Find("#author-nationality-redo").QuerySelectorAll("button")[0]);

        Assert.Equal("Belge", page.Find("#author-nationality").GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
        Assert.NotNull(page.Find("label[for=author-nationality] .modified-mark"));

        await page.SubmitAsync("form.side-panel-form");

        Assert.Equal(("Belge", 45u), (_api.Written[^1].Body.GetProperty("nationality").GetString(), _api.Written[^1].Body.GetProperty("version").GetUInt32()));
    }

    [Fact]
    public async Task Rejecting_WhatWasTyped_LeavesTheAuthorReloaded()
    {
        var page = await ReloadedWithANationalityTypedAsync();

        await page.ClickAsync(() => page.Find("#author-nationality-redo").QuerySelectorAll("button")[1]);

        Assert.Equal("Française", page.Find("#author-nationality").GetAttribute("value"));
        Assert.Empty(page.FindAll(".redo-line"));
        Assert.Empty(page.FindAll(".modified-mark"));
    }

    [Fact]
    public async Task Saving_WhileWhatWasTypedIsNeitherTakenNorRejected_WarnsFirst()
    {
        var page = await ReloadedWithANationalityTypedAsync();
        _api.Write = (_, _, _) => FakeAdminApi.Json(_api.Answers[$"/admin/authors/{Gotlib.Id}"]);
        var written = _api.Written.Count;

        await page.SubmitAsync("form.side-panel-form");

        var dialog = page.Find("dialog.dialog");
        Assert.Equal("Saisie précédente à reprendre ou rejeter", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(["Revenir à la fiche", "Enregistrer quand même"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
        Assert.Equal(written, _api.Written.Count);

        await page.ClickAsync("dialog.dialog .dialog-actions .button[data-variant=primary]");

        Assert.Equal(written + 1, _api.Written.Count);
        Assert.EndsWith(AppPaths.AdminAuthors, Services.GetRequiredService<NavigationManager>().Uri);
    }

    private async Task<IRenderedComponent<AdminAuthors>> ReloadedWithANationalityTypedAsync()
    {
        var page = Open($"edit={Gotlib.Id}");
        _api.Write = (_, _, _) => FakeAdminApi.Problem(ProblemTypes.Functional, HttpStatusCode.Conflict);
        await page.InputAsync("#author-nationality", "Belge");
        await page.SubmitAsync("form.side-panel-form");
        _api.Answers[$"/admin/authors/{Gotlib.Id}"] = new AuthorForm(
            Gotlib.Id, "Gotlieb", "Marcel", "Gotlib", "Créateur de Gai-Luron.", "Française", January, March, 45);
        await page.ClickAsync(".side-panel .error-actions .button");
        return page;
    }

    private static (string?, string) ErrorOf(IElement panel)
    {
        var error = panel.QuerySelector("[data-error-category]")!;
        return (error.GetAttribute("data-error-category"), Text(error.QuerySelector("[data-error-label]")!));
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
