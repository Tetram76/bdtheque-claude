using Bdtheque.Frontend.Components.Layout;
using Bdtheque.Frontend.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Layout;

/// <summary>
/// The headers and menus of the consultation and of the administration (visuel/charte-graphique.md § Apparence propre à
/// chaque alternative; visuel/administration.md § En-tête et menu), told apart by role, link and accessible name.
/// </summary>
public sealed class LayoutTests : BunitContext
{
    private static readonly RenderFragment Body = builder => builder.AddMarkupContent(0, "<p id=\"body\">content</p>");

    public LayoutTests() => Services.AddLocalization();

    [Fact]
    public void PublicLayout_FramesThePage_UnderTheHeaderOfTheConsultation()
    {
        var layout = Render<PublicLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.NotNull(layout.Find("main #body"));
        var header = layout.Find("header.app-header");
        Assert.Equal(AppPaths.Home, header.QuerySelector("a.app-logo")?.GetAttribute("href"));
        Assert.NotNull(header.QuerySelector($"a[href='{AppPaths.Help}'][aria-label='Aide sur cette page']"));
    }

    [Fact]
    public void ConsultationMenu_LeadsToItsEntries_AndScrollsSideways()
    {
        var layout = Render<PublicLayout>(parameters => parameters.Add(p => p.Body, Body));

        var menu = layout.Find("app-scroll-row nav[aria-label='Menu de la consultation']");
        Assert.Equal(["Accueil", "Albums", "Séries", "Auteurs", "Intentions d'achat"],
            menu.QuerySelectorAll("a").Select(link => link.TextContent.Trim()));
        Assert.Equal(AppPaths.Home, menu.QuerySelector("a")?.GetAttribute("href"));
        Assert.All(menu.QuerySelectorAll("a").Skip(1), link => Assert.Equal(AppPaths.NotYetImplemented, link.GetAttribute("href")));
    }

    [Fact]
    public void ConsultationMenu_MarksTheEntryOfTheCurrentPage()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.Home);

        var layout = Render<PublicLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.Equal(["Accueil"], layout.FindAll("nav a[aria-current='page']").Select(link => link.TextContent.Trim()));
    }

    [Fact]
    public void ConsultationHeader_SearchesFromAnyPage()
    {
        var layout = Render<PublicLayout>(parameters => parameters.Add(p => p.Body, Body));

        var form = layout.Find("header form[role='search']");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal(AppPaths.Search, form.GetAttribute("action"));
        var input = form.QuerySelector("input[type='search']")!;
        Assert.Equal("Rechercher", layout.Find($"label[for='{input.Id}']").TextContent.Trim());
    }

    [Fact]
    public void ConsultationHeader_LeavesTheSearchOut_ForAPageThatAsksIt_KeepingTheHelp()
    {
        var layout = Render<PublicLayout>(parameters => parameters.Add(p => p.Body, builder =>
        {
            builder.OpenComponent<NoHeaderSearch>(0);
            builder.CloseComponent();
        }));

        Assert.Empty(layout.FindAll("header form[role='search']"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.Help}']"));
    }

    [Fact]
    public void AdminLayout_FramesThePage_UnderTheHeaderOfTheAdministration()
    {
        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.NotNull(layout.Find("main #body"));
        var header = layout.Find("header.app-header");
        Assert.Equal(AppPaths.AdminHome, header.QuerySelector("a.app-logo")?.GetAttribute("href"));
        Assert.Equal("Administration", header.QuerySelector(".admin-tag")?.TextContent.Trim());
        Assert.Equal(AppPaths.AdminSearch, header.QuerySelector("form[role='search']")?.GetAttribute("action"));
        Assert.NotNull(header.QuerySelector($"a[href='{AppPaths.Help}'][aria-label='Aide sur cette page']"));
        Assert.Equal("Aller à la consultation", header.QuerySelector($"a[href='{AppPaths.Home}']")?.TextContent.Trim());
    }

    [Fact]
    public void AdminMenu_IsAPanelOpenedByTheMenuButton_AndClosedByItsCross()
    {
        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        var toggle = layout.Find("header button.admin-menu-toggle");
        var panel = layout.Find($"#{toggle.GetAttribute("popovertarget")}");
        Assert.Equal("Menu", toggle.TextContent.Trim());
        Assert.Equal("auto", panel.GetAttribute("popover"));
        Assert.Equal("Plan de l'administration", panel.QuerySelector("nav")?.GetAttribute("aria-label"));
        var close = panel.QuerySelector("button[aria-label='Fermer le menu']")!;
        Assert.Equal(panel.Id, close.GetAttribute("popovertarget"));
        Assert.Equal("hide", close.GetAttribute("popovertargetaction"));
    }

    [Fact]
    public void AdminMenu_PresentsThePlanByGroups_TheNewEdition_AndLogsOutLast()
    {
        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        var plan = layout.Find("nav[aria-label=\"Plan de l'administration\"]");
        Assert.Equal(["Accueil", "Collection", "Auteurs et éditeurs", "Référentiels", "Réglages"],
            plan.QuerySelectorAll(".admin-menu-group-title").Select(title => title.TextContent.Trim()));
        Assert.Equal(
            ["Tableau de bord", "Nouvelle édition", "Albums", "Séries", "Éditions", "Intentions d'achat", "Auteurs", "Éditeurs",
                "Genres", "Univers", "Paramétrage", "Statistiques privées", "Se déconnecter"],
            plan.QuerySelectorAll("a").Select(link => link.TextContent.Trim()));
        Assert.Equal(AppPaths.AdminHome, plan.QuerySelector("a")?.GetAttribute("href"));
        Assert.Equal(AppPaths.AdminLogout, plan.QuerySelectorAll("a")[^1].GetAttribute("href"));
        Assert.NotNull(plan.QuerySelector("hr + a, hr + * a"));
    }

    [Fact]
    public void AdminMenu_MarksTheEntryOfTheCurrentPage()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminHome);

        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.Equal(["Tableau de bord"], layout.FindAll("nav a[aria-current='page']").Select(link => link.TextContent.Trim()));
    }
}
