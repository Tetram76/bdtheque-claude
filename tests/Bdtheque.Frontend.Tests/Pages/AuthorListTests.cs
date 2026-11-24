using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The list of the authors of the consultation: its rows, those of the list of the administration without its actions, each
/// leading to the record of its author, its count, its only filter, the possession, which limits it to the collection
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md), its empty lists and its errors, told apart by role, link and
/// text, never by appearance.
/// </summary>
public sealed class AuthorListTests : BunitContext, IDisposable
{
    private static readonly AuthorCatalogListItem Herge = new(
        Guid.Parse("01900000-0000-7000-8000-000000000001"), "Remi", "Georges", "Hergé", "Belge", 9, 4,
        [ContributionRole.Scenarist, ContributionRole.Illustrator]);

    private static readonly AuthorCatalogListItem Franquin = new(
        Guid.Parse("01900000-0000-7000-8000-000000000002"), "Franquin", "André", null, null, 7, 7, [ContributionRole.Illustrator]);

    private static readonly AuthorCatalogListItem VanHamme = new(
        Guid.Parse("01900000-0000-7000-8000-000000000003"), "Van Hamme", "Jean", null, null, 1, 0, [ContributionRole.Scenarist]);

    // Credited on a series alone: no album, yet a role.
    private static readonly AuthorCatalogListItem Seule = new(
        Guid.Parse("01900000-0000-7000-8000-000000000004"), "Seule", null, null, null, 0, 0, [ContributionRole.Colorist]);

    private static readonly AuthorCatalogListItem Inconnu = new(
        Guid.Parse("01900000-0000-7000-8000-000000000005"), "Inconnu", null, null, null, 0, 0, []);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visitor, not signed in, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() },
        authenticated: false);

    public AuthorListTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddScoped<RecordSearch>();
        Services.AddSingleton(TimeProvider.System);
        Answer([Herge, Franquin, VanHamme, Seule, Inconnu], totalCount: 5);
        // The whole list, whose number the heading gives when criteria narrow the list.
        _api.Answers[CatalogAuthorListQuery.All.ApiPath(1)] = Page([Herge], 1234);
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Row_LeadsToTheRecordOfItsAuthor_NamedByItsMainIdentifier_ItsNamesUnderAPseudonym()
    {
        var herge = Rows(Open(""))[0];

        var link = herge.QuerySelector($"a[href='{AppPaths.AuthorRecord(Herge.Id)}']")!;
        Assert.Equal("Hergé", Text(link));
        Assert.Contains("Georges Remi", Text(herge.QuerySelector(".author-main")!));
        Assert.Equal("Belge", Text(herge.QuerySelector(".author-nationality")!));
        Assert.Equal("Scénario · Dessin", Text(herge.QuerySelector(".author-roles")!));
        // Without a pseudonym, the name is the main identifier, with nothing under it.
        Assert.Equal("André Franquin", Text(Rows(Open(""))[1].QuerySelector(".author-main")!));
    }

    [Theory]
    [InlineData(0, "9 albums dont 4 dans la collection")]
    [InlineData(1, "7 albums dans la collection")]
    [InlineData(2, "1 album")]
    public void Albums_SayHowManyAreInTheCollection(int row, string expected)
    {
        Assert.Equal(expected, Text(Rows(Open(""))[row].QuerySelector(".author-albums")!));
    }

    [Fact]
    public void Row_WithoutAlbum_HasTheDashOfAZero_ItsRolesOnASeriesKept_AndWithoutRole_TheDashOfAMissingData()
    {
        var rows = Rows(Open(""));

        var seule = rows[3];
        Assert.True(seule.QuerySelector(".author-albums")!.HasAttribute("data-zero"));
        Assert.Equal("Aucun album", Text(seule.QuerySelector(".author-albums .visually-hidden")!));
        Assert.Equal("Couleurs", Text(seule.QuerySelector(".author-roles")!));
        var inconnu = rows[4];
        Assert.True(inconnu.QuerySelector(".author-roles")!.HasAttribute("data-none"));
        Assert.Equal("—", inconnu.QuerySelector(".author-roles [aria-hidden]")!.TextContent.Trim());
        Assert.True(inconnu.QuerySelector(".author-nationality")!.HasAttribute("data-none"));
    }

    [Fact]
    public void Row_OfTheConsultation_HasNoAction()
    {
        var page = Open("");

        Assert.Empty(page.FindAll(".author-actions"));
        Assert.Empty(page.FindAll(".author-row button"));
        Assert.DoesNotContain(page.FindAll("a"), a => a.GetAttribute("href")?.StartsWith(AppPaths.AdminHome, StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Heading_GivesTheNumberOfAllTheAuthors_AndTheSummaryThoseFound()
    {
        Answer([Herge], totalCount: 1);

        var page = Open("state=InCollection");

        Assert.Equal("1 234", Text(page.Find(".list-heading-count")));
        Assert.Contains("1 auteur sur 1 234", Text(page.Find(".list-summary")));
    }

    [Fact]
    public void Possession_IsItsOnlyFilter_ItsChoicesNamedAfterTheAlbumsOfTheAuthor()
    {
        var page = Open("filters=open&state=PurchaseIntent");

        var select = page.Find("select[name='state']");
        // An author is not owned (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
        Assert.Equal(
            ["Tous les auteurs", "Dont des albums possédés", "Dont des albums en intention", "Dont des albums catalogués"],
            select.QuerySelectorAll("option").Select(Text));
        Assert.Equal("PurchaseIntent", select.QuerySelector("option[selected]")!.GetAttribute("value"));
        Assert.Single(page.FindAll(".list-filters select, .list-filters input:not([type=hidden])"));
        Assert.Equal(AppPaths.Authors, page.Find("form.list-search").GetAttribute("action"));
    }

    [Fact]
    public void Possession_ChosenIsAnActiveFilter_WhichTheNavigationAndThePagesKeep()
    {
        Answer([Herge], totalCount: 120);

        var page = Open("state=InCollection");

        Assert.Contains("Possession : Dont des albums possédés", Text(page.Find(".active-filters")));
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.Authors}?entry=B&state=InCollection");
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == $"{AppPaths.Authors}?state=InCollection&page=2");
    }

    [Theory]
    [InlineData("q=zorglub", "Aucun auteur ne correspond à « zorglub ».", "Effacer la recherche", AppPaths.Authors)]
    [InlineData("state=InCollection", "Aucun auteur ne correspond à ces critères.", "Effacer les filtres", AppPaths.Authors)]
    [InlineData("entry=Q", "Aucun auteur sous cette entrée.", "Tous les auteurs", AppPaths.Authors)]
    public void EmptyList_SaysWhyAndOffersToClearWhatNarrowsIt_NeverToCreateAnAuthor(string criteria, string message, string action, string href)
    {
        Answer([], totalCount: 0);

        var empty = Open(criteria).Find(".empty-list");

        Assert.Contains(message, Text(empty));
        var link = Assert.Single(empty.QuerySelectorAll(".empty-list-actions a"));
        Assert.Equal((action, href), (Text(link), link.GetAttribute("href")));
    }

    [Fact]
    public void EmptyList_WithoutCriterion_SaysThatNoAuthorIsRecorded_WithoutAction()
    {
        Answer([], totalCount: 0);

        var empty = Open("").Find(".empty-list");

        Assert.Contains("Aucun auteur n'est encore enregistré.", Text(empty));
        Assert.Empty(empty.QuerySelectorAll(".empty-list-actions a"));
    }

    [Fact]
    public void Error_OfTheApi_IsPresentedInsteadOfTheList()
    {
        _api.Failures["/catalog/authors"] = HttpStatusCode.InternalServerError;

        var page = Open("");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".author-row"));
    }

    [Theory]
    [InlineData("state=InCollection", "authors.filter")]
    [InlineData("q=herge", "authors.search")]
    [InlineData("entry=H", "authors.initial")]
    [InlineData("page=2", "authors.page")]
    public void Audience_TheActionOfItsCriteria_ThenTheScreen(string criteria, string action)
    {
        Answer([Herge], totalCount: 120);

        Open(criteria, recorded: true);

        Assert.Equal([action, "authors"], _audience.Functions);
    }

    [Fact]
    public void Audience_TheWholeList_TheScreenAlone()
    {
        Open("", recorded: true);

        Assert.Equal(["authors"], _audience.Functions);
    }

    private IRenderedComponent<AuthorList> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.Authors}?{criteria}");
        return Render<AuthorList>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<AuthorList> page) => page.FindAll(".author-row:not(.author-row-head)");

    private void Answer(IReadOnlyList<AuthorCatalogListItem> items, int totalCount) => _api.Answers["/catalog/authors"] = Page(items, totalCount);

    private static Page<AuthorCatalogListItem> Page(IReadOnlyList<AuthorCatalogListItem> items, int totalCount) =>
        new(items, 1, ListQuery.PageSize, totalCount);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
