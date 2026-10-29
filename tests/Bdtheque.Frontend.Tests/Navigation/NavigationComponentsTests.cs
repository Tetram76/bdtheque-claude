using System.Globalization;
using Bdtheque.Frontend.Components.Navigation;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Navigation;

/// <summary>
/// The navigation components shared by the screens: fil d'Ariane, navigation par initiale, pagination, and the row that
/// scrolls sideways. Told apart by what does not depend on the theme (role, link and its target, accessible name).
/// </summary>
public sealed class NavigationComponentsTests : BunitContext
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    public NavigationComponentsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Services.AddLocalization();
        Services.AddScoped<BrowserTimeZone>();
        Services.AddScoped<DisplayFormatter>();
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _previous;
        base.Dispose(disposing);
    }

    private static string Group => CultureInfo.GetCultureInfo("fr-FR").NumberFormat.NumberGroupSeparator;

    [Fact]
    public void Breadcrumb_LinksItsLevels_AndEndsWithTheCurrentPage()
    {
        var breadcrumb = Render<Breadcrumb>(parameters => parameters.Add(p => p.Items,
        [
            new BreadcrumbItem("Administration", "/admin"),
            new BreadcrumbItem("Albums", "/admin/albums"),
            new BreadcrumbItem("Le Lotus bleu"),
        ]));

        var nav = breadcrumb.Find("nav");
        Assert.Equal("Fil d'Ariane", nav.GetAttribute("aria-label"));
        var items = nav.QuerySelectorAll("li");
        Assert.Equal(["/admin", "/admin/albums"], nav.QuerySelectorAll("a").Select(link => link.GetAttribute("href")));
        Assert.Equal("Le Lotus bleu", items[^1].TextContent.Trim());
        Assert.Equal("page", items[^1].QuerySelector("[aria-current]")?.GetAttribute("aria-current"));
        Assert.Null(items[^1].QuerySelector("a"));
    }

    [Fact]
    public void InitialNavigation_OffersAllTheEntries_TheCurrentOneMarked()
    {
        var navigation = Render<InitialNavigation>(parameters => parameters
            .Add(p => p.Current, "B")
            .Add(p => p.EntryHref, entry => $"/admin/albums?entry={Uri.EscapeDataString(entry ?? "")}"));

        var links = navigation.FindAll("nav a");
        Assert.Equal("Navigation par initiale", navigation.Find("nav").GetAttribute("aria-label"));
        Assert.Equal(["Toutes", .. "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(letter => letter.ToString()), "#", "@"],
            links.Select(link => link.TextContent.Trim()));
        Assert.Equal("/admin/albums?entry=%23", links[^2].GetAttribute("href"));
        Assert.Equal(["B"], links.Where(link => link.GetAttribute("aria-current") == "page").Select(link => link.TextContent.Trim()));
        Assert.NotNull(navigation.Find("app-scroll-row nav"));
    }

    [Fact]
    public void InitialNavigation_WithoutEntry_MarksToutes()
    {
        var navigation = Render<InitialNavigation>(parameters => parameters
            .Add(p => p.Current, null)
            .Add(p => p.EntryHref, entry => entry is null ? "/list" : $"/list?entry={entry}"));

        var current = navigation.Find("a[aria-current='page']");
        Assert.Equal("Toutes", current.TextContent.Trim());
        Assert.Equal("/list", current.GetAttribute("href"));
    }

    [Fact]
    public void Pagination_GivesThePositionInTheList_ThePages_AndThePreviousAndNextOnes()
    {
        var pagination = Render<Pagination>(parameters => parameters
            .Add(p => p.Number, 2)
            .Add(p => p.Size, 50)
            .Add(p => p.TotalCount, 2847)
            .Add(p => p.ItemsLabel, "Albums")
            .Add(p => p.PageHref, page => $"/list?page={page}"));

        var nav = pagination.Find("nav");
        Assert.Equal("Pages de la liste", nav.GetAttribute("aria-label"));
        Assert.Equal($"Albums 51 à 100 sur 2{Group}847", nav.QuerySelector(".pagination-position")?.TextContent.Trim());
        Assert.Equal("/list?page=1", pagination.Find("a[rel='prev']").GetAttribute("href"));
        Assert.Equal("/list?page=3", pagination.Find("a[rel='next']").GetAttribute("href"));
        Assert.Equal(["1", "2", "3", "4", "5", "…", "57"], pagination.FindAll(".pagination-pages li").Select(item => item.TextContent.Trim()));
        Assert.Equal("2", pagination.Find("[aria-current='page']").TextContent.Trim());
        Assert.Null(pagination.Find("[aria-current='page']").GetAttribute("href"));
    }

    [Fact]
    public void Pagination_OnTheOnlyPage_HasNeitherPreviousNorNext()
    {
        var pagination = Render<Pagination>(parameters => parameters
            .Add(p => p.Number, 1)
            .Add(p => p.Size, 50)
            .Add(p => p.TotalCount, 38)
            .Add(p => p.ItemsLabel, "Albums")
            .Add(p => p.PageHref, page => $"/list?page={page}"));

        Assert.Equal("Albums 1 à 38 sur 38", pagination.Find(".pagination-position").TextContent.Trim());
        Assert.Empty(pagination.FindAll("a[rel]"));
        Assert.Equal(2, pagination.FindAll("[aria-disabled='true']").Count);
    }

    [Fact]
    public void Pagination_OfAnEmptyList_ShowsNothing()
    {
        var pagination = Render<Pagination>(parameters => parameters
            .Add(p => p.Number, 1)
            .Add(p => p.Size, 50)
            .Add(p => p.TotalCount, 0)
            .Add(p => p.ItemsLabel, "Albums")
            .Add(p => p.PageHref, page => $"/list?page={page}"));

        Assert.Empty(pagination.Markup.Trim());
    }

    [Fact]
    public void ScrollRow_FramesItsContent_BetweenTwoChevronsHiddenFromAssistiveTechnologies()
    {
        var row = Render<ScrollRow>(parameters => parameters
            .AddUnmatched("class", "consultation-menu")
            .AddChildContent("<a href=\"/\">Accueil</a>"));

        var host = row.Find("app-scroll-row");
        Assert.Equal(["scroll-row", "consultation-menu"], host.ClassList);
        Assert.NotNull(host.QuerySelector(".scroll-row-items a"));
        var chevrons = host.QuerySelectorAll("button.scroll-row-chevron");
        Assert.Equal(["start", "end"], chevrons.Select(chevron => chevron.GetAttribute("data-direction")));
        Assert.All(chevrons, chevron =>
        {
            Assert.Equal("true", chevron.GetAttribute("aria-hidden"));
            Assert.Equal("-1", chevron.GetAttribute("tabindex"));
        });
    }
}
