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
/// The list of the publishers of the administration (visuel/listes/liste-par-liste.md): its rows, its count, its search, its
/// dialogs and its errors, told apart by role, link and text, never by appearance. What it shares with the list of the albums
/// (page beyond the last, pagination) is pinned by AdminAlbumsTests.
/// </summary>
public sealed class AdminPublishersTests : BunitContext, IDisposable
{
    private static readonly PublisherAdminListItem Casterman = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), "Casterman", "https://www.casterman.com/",
        ["Écritures", "Fac-similé", "Univers d'auteurs"], 412, new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero), 41);

    private static readonly PublisherAdminListItem Dargaud = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), "Dargaud", "https://dargaud.com/bd",
        ["Long courrier"], 1, new DateTimeOffset(2026, 7, 21, 8, 0, 0, TimeSpan.Zero), 42);

    private static readonly PublisherAdminListItem Futuropolis = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), "Futuropolis", null,
        [], 0, new DateTimeOffset(2026, 2, 2, 8, 0, 0, TimeSpan.Zero), 43);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: true);

    public AdminPublishersTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Answer([Casterman, Dargaud, Futuropolis], totalCount: 3);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleTheNumberOfPublishersAndTheCreation()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Éditeurs"], page.FindAll(".breadcrumb li").Select(item => item.TextContent.Trim()));
        Assert.Equal(("Éditeurs", "96"), (page.Find("h1").FirstChild!.TextContent.Trim(), Text(page.Find(".list-heading-count"))));
        Assert.Equal(AppPaths.AdminNewPublisher, page.Find(".list-heading a.button").GetAttribute("href"));
    }

    [Fact]
    public void Search_OnTheName_WithoutFiltersNorNavigationByInitial()
    {
        var page = Open("q=dar");

        var form = page.Find("form.list-search");
        Assert.Equal(AppPaths.AdminPublishers, form.GetAttribute("action"));
        Assert.Equal(("q", "dar"), (form.QuerySelector("input[type='search']")!.GetAttribute("name"), form.QuerySelector("input[type='search']")!.GetAttribute("value")));
        Assert.Null(form.QuerySelector("button[aria-controls]"));
        Assert.Empty(page.FindAll(".initial-navigation"));
        Assert.Empty(page.FindAll(".active-filters"));
    }

    [Fact]
    public void Rows_TheNameItsCollectionsItsWebsiteAndItsNumbers()
    {
        var rows = Open("").FindAll(".publisher-row:not(.publisher-row-head)").Select(row => (
            Text(row.QuerySelector(".publisher-name")!),
            Text(row.QuerySelector(".publisher-collection-names")!),
            Text(row.QuerySelector(".publisher-website")!),
            Text(row.QuerySelector(".publisher-collections")!),
            Text(row.QuerySelector(".publisher-editions")!)));

        // The website without its scheme, « www. » nor final slash. On two lines, each number is preceded by the name of its
        // column, which the styles hide in a table.
        Assert.Equal(
            [
                ("Casterman", "Écritures, Fac-similé, Univers d'auteurs", "casterman.com", "Collections 3", "Éditions 412"),
                ("Dargaud", "Long courrier", "dargaud.com/bd", "Collections 1", "Éditions 1"),
                ("Futuropolis", "Aucune collection", "—Non renseigné", "Collections 0", "Éditions 0"),
            ],
            rows);
    }

    [Fact]
    public void Rows_MarkWhatIsMissing_ByThePresenceOfTheirAttribute()
    {
        // A boolean attribute of Blazor has no value: present when true, absent when false, which the styles select on.
        var rows = Open("").FindAll(".publisher-row:not(.publisher-row-head)").Select(row => (
            row.QuerySelector(".publisher-collection-names")!.HasAttribute("data-empty"),
            row.QuerySelector(".publisher-collections")!.HasAttribute("data-empty"),
            row.QuerySelector(".publisher-editions")!.HasAttribute("data-empty")));

        Assert.Equal([(false, false, false), (false, false, false), (true, true, true)], rows);
    }

    [Fact]
    public void Rows_LeadToTheEntryFormAndToTheWebsite_AndOpenTheirDialogsInTheList()
    {
        var rows = Open("q=c").FindAll(".publisher-row:not(.publisher-row-head)");
        var id = Casterman.Id;

        Assert.Equal(AppPaths.AdminEditPublisher(id), rows[0].QuerySelector(".publisher-name")!.GetAttribute("href"));
        Assert.Equal("Modifier Casterman", rows[0].QuerySelector(".publisher-actions a.button")!.GetAttribute("aria-label"));
        // The website, outside the application, opens beside it.
        var website = rows[0].QuerySelector(".publisher-website a")!;
        Assert.Equal(("https://www.casterman.com/", "_blank", "noopener noreferrer"),
            (website.GetAttribute("href"), website.GetAttribute("target"), website.GetAttribute("rel")));
        Assert.Equal(
            [
                ("Ouvrir le site web", "https://www.casterman.com/", "_blank"),
                ("Fusionner avec un autre éditeur…", $"{AppPaths.AdminPublishers}?q=c&merge={id}", null),
                ("Supprimer…", $"{AppPaths.AdminPublishers}?q=c&delete={id}", null),
            ],
            rows[0].QuerySelectorAll(".actions-menu-panel .menu-item").Select(item => (Text(item), item.GetAttribute("href"), item.GetAttribute("target"))));
    }

    [Fact]
    public void Rows_OfAPublisherWithoutWebsite_HaveNoWebsiteInTheirMenu()
    {
        var row = Open("").FindAll(".publisher-row:not(.publisher-row-head)")[2];

        Assert.Equal(
            ["Fusionner avec un autre éditeur…", "Supprimer…"],
            row.QuerySelectorAll(".actions-menu-panel .menu-item").Select(Text));
    }

    [Theory]
    [InlineData("", "3 éditeurs")]
    [InlineData("q=dar", "3 éditeurs sur 96")]
    public void Summary_TheCountOutOfAllThePublishers_AndTheOrder(string criteria, string count)
    {
        var summary = Open(criteria).Find(".list-summary");

        Assert.Equal(count, Text(summary.Children[0]));
        Assert.Equal("Triés par nom", Text(summary.Children[1]));
    }

    [Fact]
    public void Empty_ASearchWithoutResult_SaysItAndOffersToClearItOrToCreateThePublisher()
    {
        Answer([], totalCount: 0);

        var empty = Open("q=Zorglub").Find(".empty-list");

        Assert.Equal("Aucun éditeur ne correspond à « Zorglub ».", Text(empty.QuerySelector("strong")!));
        Assert.Equal(
            [("Effacer la recherche", AppPaths.AdminPublishers), ("Créer l'éditeur « Zorglub »", AppPaths.AdminNewPublisher)],
            empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Empty_WithoutAnyPublisher_OffersToCreateOne()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Equal("Aucun éditeur n'est encore enregistré.", Text(empty.QuerySelector("strong")!));
        Assert.Equal([("Nouvel éditeur", AppPaths.AdminNewPublisher)], empty.QuerySelectorAll("a").Select(link => (Text(link), link.GetAttribute("href"))));
    }

    [Fact]
    public void Deletion_ADialogThatAnnouncesTheImpact_AndConfirmsWithTheVersionAndTheFingerprint()
    {
        var id = Futuropolis.Id;
        _api.Answers[$"/admin/publishers/{id}/deletion-impact"] = new DeletionImpact([], [], [new ImpactCount(EntityKind.PublisherCollection, 2)], [], "f1");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal(("Supprimer cet éditeur ?", "Futuropolis"),
            (Text(dialog.QuerySelector(".dialog-title")!), Text(dialog.QuerySelector(".dialog-subject")!)));
        Assert.Contains("2 collections éditeur", Text(dialog.QuerySelector(".deletion-impact")!));
        var form = dialog.QuerySelector("form[method='post']")!;
        Assert.Equal(
            [("Version", "43"), ("Fingerprint", "f1")],
            form.QuerySelectorAll("input[type='hidden']").Where(input => input.GetAttribute("name") is "Version" or "Fingerprint")
                .Select(input => (input.GetAttribute("name"), input.GetAttribute("value"))));
    }

    [Fact]
    public void Deletion_RefusedWhileEditionsOrSeriesUseThePublisher_SaysWhatToDo()
    {
        var id = Casterman.Id;
        _api.Answers[$"/admin/publishers/{id}/deletion-impact"] = new DeletionImpact(
            [new ImpactCount(EntityKind.Edition, 412), new ImpactCount(EntityKind.Series, 3)], [], [], [], "f2");

        var dialog = Open($"delete={id}").Find("dialog");

        Assert.Equal("Suppression impossible", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Contains("412 éditions", Text(dialog.QuerySelector(".deletion-impact")!));
        Assert.Equal(
            [
                "Rattachez d'abord ses éditions à un autre éditeur, ou supprimez-les, puis recommencez.",
                "Retirez d'abord l'éditeur des valeurs des nouvelles éditions de ces séries, puis recommencez.",
            ],
            dialog.QuerySelectorAll(".dialog-note").Select(Text));
        Assert.Equal(["Fermer"], dialog.QuerySelectorAll(".dialog-actions .button").Select(Text));
    }

    [Fact]
    public void Merge_ADialogThatSearchesTheOtherPublisher_AndContinuesWithTheOneChosen()
    {
        var dialog = Open($"merge={Casterman.Id}&mergeQ=a").Find("dialog");

        Assert.Equal("Fusionner deux éditeurs", Text(dialog.QuerySelector(".dialog-title")!));
        Assert.Equal(
            [("Dargaud", "1 collection · 1 édition"), ("Futuropolis", "Aucune collection · aucune édition")],
            dialog.QuerySelectorAll(".merge-candidate").Select(c => (Text(c.QuerySelector("strong")!), Text(c.QuerySelector("strong + span")!))));
        var next = dialog.QuerySelector("#publisher-merge-continue")!;
        Assert.Equal(AppPaths.AdminMergePublishers, next.GetAttribute("action"));
        Assert.Equal(Casterman.Id.ToString(), next.QuerySelector("input[name='publisher']")!.GetAttribute("value"));
        Assert.Contains(_api.Requested, path => path == "/admin/publishers?q=a&pageSize=11");
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/admin/publishers"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".publisher-row"));
    }

    [Fact]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen()
    {
        Open("q=dar&page=2", recorded: true);

        // The action of the list first, which displays the screen.
        Assert.Equal(["admin.publishers.page", "admin.publishers"], _audience.Functions);
    }

    private IRenderedComponent<AdminPublishers> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminPublishers}?{criteria}");
        return Render<AdminPublishers>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private void Answer(IReadOnlyList<PublisherAdminListItem> items, int totalCount) =>
        _api.Answers["/admin/publishers"] = new Page<PublisherAdminListItem>(items, 1, AdminListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
