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
/// The list of the authors of the administration (visuel/listes/liste-par-liste.md): its rows, its count, its search without
/// filters, its dialogs and its errors, told apart by role, link and text, never by appearance. What it shares with the list
/// of the albums (active filters, navigation, page beyond the last) is pinned by AdminAlbumsTests.
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
            Text(row.QuerySelector(".author-name")!),
            row.QuerySelector(".author-identity") is { } identity ? Text(identity) : null,
            Text(row.QuerySelector(".author-nationality")!),
            Text(row.QuerySelector(".author-albums")!),
            Text(row.QuerySelector(".author-roles")!)));

        Assert.Equal(
            [
                ("Christophe Gibelin", null, "Française", "14 albums", "Scénario"),
                ("Jean-Pierre Gibrat", null, "Française", "1 album", "Scénario · Dessin · Couleurs"),
                ("Alain Gomez", null, "—Non renseignée", "Aucun album", "—"),
                // Under a pseudonym, the first name and the last name below it.
                ("Gotlib", "Marcel Gotlieb", "Française", "22 albums", "Scénario · Dessin"),
            ],
            rows);
    }

    [Fact]
    public void Rows_MarkTheDataThatIsMissing_ByThePresenceOfTheirAttribute()
    {
        // A boolean attribute of Blazor has no value: present when true, absent when false, which the styles select on.
        var rows = Open("").FindAll(".author-row:not(.author-row-head)").Select(row => (
            row.QuerySelector(".author-albums")!.HasAttribute("data-empty"),
            row.QuerySelector(".author-roles")!.HasAttribute("data-empty")));

        Assert.Equal([(false, false), (false, false), (true, true), (false, false)], rows);
    }

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
    public void Rows_LeadToTheEntryFormAndTheConsultation_AndOpenTheirDialogsInTheList()
    {
        var row = Open("q=gib").FindAll(".author-row:not(.author-row-head)")[3];
        var id = Gotlib.Id;

        Assert.Equal(AppPaths.AdminEditAuthor(id), row.QuerySelector(".author-name")!.GetAttribute("href"));
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
            [("Effacer la recherche", $"{AppPaths.AdminAuthors}?entry=Z"), ("Créer l'auteur « Zorglub »", AppPaths.AdminNewAuthor)],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
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
    public void Empty_WithoutAnyCriterion_SaysThereIsNoAuthorYet_WithNothingToClear()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucun auteur n'est encore enregistré.", Text(empty));
        Assert.Empty(empty.QuerySelectorAll("a"));
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
