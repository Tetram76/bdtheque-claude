using Bdtheque.Frontend.Components.Layout;
using Bdtheque.Frontend.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Layout;

/// <summary>
/// The headers and menus of the consultation and of the administration
/// (visuel/charte-graphique/apparence-propre-a-chaque-alternative.md; visuel/administration/en-tete-et-menu.md), told
/// apart by role, link and accessible name.
/// </summary>
public sealed class LayoutTests : BunitContext, IDisposable
{
    private static readonly RenderFragment Body = builder => builder.AddMarkupContent(0, "<p id=\"body\">content</p>");

    private readonly System.Globalization.CultureInfo _previous = System.Globalization.CultureInfo.CurrentCulture;
    private readonly Pages.FakeAdminApi _api = new(new Audience.FakeAudienceApi());

    public LayoutTests()
    {
        // The culture of the requests, which the numbers of the menu of the administration follow.
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
        _api.AddServices(Services, new TestRequest());
    }

    void IDisposable.Dispose()
    {
        System.Globalization.CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

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
    public void EveryPage_LeadsToTheInformationOnTheAudienceMeasurement()
    {
        // From any page, of the consultation as of the administration (fonctionnel/mesure-audience.md, Information et opposition).
        IRenderedComponent<IComponent>[] layouts =
        [
            Render<PublicLayout>(parameters => parameters.Add(p => p.Body, Body)),
            Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body)),
        ];

        Assert.All(layouts, layout =>
            Assert.Equal("Mesure d'audience", layout.Find($"footer a[href='{AppPaths.AudienceInformation}']").TextContent.Trim()));
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
                "Genres", "Univers", "Paramétrage", "Statistiques privées"],
            plan.QuerySelectorAll("a > span:first-child").Select(entry => entry.TextContent.Trim()));
        Assert.Equal(AppPaths.AdminHome, plan.QuerySelector("a")?.GetAttribute("href"));
        // The logout last, after a rule: a form posted to its page, never a link that any page could follow.
        var logout = plan.QuerySelector("hr + form")!;
        Assert.Equal(("post", AppPaths.AdminLogout), (logout.GetAttribute("method"), logout.GetAttribute("action")));
        Assert.Equal("Se déconnecter", logout.QuerySelector("button[type='submit']")?.TextContent.Trim());
        Assert.Equal("admin-logout", logout.QuerySelector("input[name='_handler']")?.GetAttribute("value"));
    }

    [Fact]
    public void AdminMenu_GivesTheNumberOfRecordsOfTheEntriesThatLeadToAList()
    {
        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        var counts = layout.FindAll("nav a").Where(link => link.QuerySelector(".admin-menu-count") is not null)
            .Select(link => (link.QuerySelector("span")!.TextContent.Trim(), string.Join(" ", link.QuerySelector(".admin-menu-count")!.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
        Assert.Equal(
            [("Albums", "2 847"), ("Séries", "412"), ("Éditions", "2 913"), ("Intentions d'achat", "18"), ("Auteurs", "1 106"),
                ("Éditeurs", "96"), ("Genres", "24"), ("Univers", "37")],
            counts.ToList());
    }

    [Fact]
    public void AdminMenu_WithoutTheNumbers_StillLeadsEverywhere()
    {
        _api.Failures["/admin/record-counts"] = System.Net.HttpStatusCode.ServiceUnavailable;

        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.Empty(layout.FindAll(".admin-menu-count"));
        Assert.Equal(12, layout.FindAll("nav[aria-label=\"Plan de l'administration\"] a").Count);
    }

    [Fact]
    public void AdminMenu_MarksTheEntryOfTheCurrentPage()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AdminHome);

        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.Equal(["Tableau de bord"], layout.FindAll("nav a[aria-current='page']").Select(link => link.TextContent.Trim()));
    }
}
