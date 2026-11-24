using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using Bdtheque.Contracts.Audience;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Pages.Admin;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The indicators of the audience measurement in the administration (fonctionnel/mesure-audience.md, Indicateurs): every
/// function followed, the most used first, a function never used with null indicators, read for the criteria of the address;
/// told apart by role, structure and text, never by appearance.
/// </summary>
public sealed class AdminStatisticsTests : BunitContext, IDisposable
{
    private const string Indicators = "/admin/audience/indicators";

    // The current month, in universal time, is November 2026: a year of use begins in December 2025.
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    private static readonly FunctionIndicators AlbumRecord = new(
        AudienceFunctions.AlbumRecord.Key, 42, 20, 3, 17, new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero),
        [new(new DateOnly(2026, 10, 1), 2, 1), new(new DateOnly(2026, 11, 1), 40, 19)]);

    private static readonly FunctionIndicators AdminAlbums = new(
        AudienceFunctions.AdminAlbums.Key, 30, 12, 0, 0, new DateTimeOffset(2026, 11, 2, 23, 30, 0, TimeSpan.Zero),
        [new(new DateOnly(2026, 9, 1), 10, 4), new(new DateOnly(2026, 11, 1), 20, 8)]);

    private static readonly FunctionIndicators AlbumsSearch = new(
        AudienceFunctions.AdminAlbumsSearch.Key, 5, 3, 1, 4, new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero),
        [new(new DateOnly(2026, 11, 1), 5, 3)]);

    // A function the frontend no longer follows, whose usages the API still counts under its key.
    private static readonly FunctionIndicators Former = new(
        "admin.former", 1, 1, 0, 0, new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero), [new(new DateOnly(2026, 9, 1), 1, 1)]);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeAdminApi _api;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 23), "planche", new DateOnly(2026, 11, 23)).Format() },
        authenticated: true);

    public AdminStatisticsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _api = new FakeAdminApi(_audience);
        _api.AddServices(Services, _request);
        Services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));
        JSInterop.Mode = JSRuntimeMode.Loose;
        _api.Answers[Indicators] = new List<FunctionIndicators> { AdminAlbums, AlbumsSearch, AlbumRecord, Former };
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Heading_TheTitleUnderTheAdministration_AndTheBlockOfTheAudience()
    {
        var page = Open("");

        Assert.Equal(["Administration", "Statistiques privées"], page.FindAll(".breadcrumb li").Select(item => Text(item)));
        Assert.Equal("Statistiques privées", Text(page.Find("h1")));
        Assert.Equal("Mesure d'audience", Text(page.Find("section.block h2")));
    }

    [Fact]
    public void Criteria_AFormThatLeadsToThePage_WithTheCriteriaOfTheAddress()
    {
        var page = Open("period=All&users=Visitors&device=Smartphone&engine=WebKit&screen=Liste%20des%20albums&filters=open");

        var form = page.Find("form.list-search");
        Assert.Equal(("get", AppPaths.AdminStatistics), (form.GetAttribute("method"), form.GetAttribute("action")));
        Assert.Equal(
            [("Période", "period", "Depuis le début"), ("Utilisateurs", "users", "Visiteurs"), ("Appareil", "device", "Smartphone"),
                ("Moteur de rendu", "engine", "WebKit"), ("Écran", "screen", "Liste des albums")],
            form.QuerySelectorAll("select").Select(select => (
                Text(form.QuerySelector($"label[for='{select.Id}']")!), select.GetAttribute("name")!,
                Text(select.QuerySelector("option[selected]")!))));
        Assert.Equal(
            ["Mois en cours", "3 derniers mois", "12 derniers mois", "Depuis le début"],
            form.QuerySelectorAll("select[name='period'] option").Select(option => Text(option)));
        Assert.Equal(
            ["Tous les utilisateurs", "Visiteurs", "Administrateur"],
            form.QuerySelectorAll("select[name='users'] option").Select(option => Text(option)));
        Assert.Equal(
            ["Tous les appareils", "Ordinateur", "Tablette", "Smartphone"],
            form.QuerySelectorAll("select[name='device'] option").Select(option => Text(option)));
        Assert.Equal(
            ["Tous les moteurs", "Blink", "Gecko", "WebKit", "Autre"],
            form.QuerySelectorAll("select[name='engine'] option").Select(option => Text(option)));
        Assert.NotNull(form.QuerySelector("button[type='submit']"));
    }

    [Fact]
    public void Filters_FoldedByDefault_CountedOnTheirButton_KeptWhileFolded()
    {
        var page = Open("users=Visitors&screen=Liste%20des%20albums");

        var form = page.Find("form.list-search");
        Assert.Empty(form.QuerySelectorAll("select"));
        Assert.Equal("2", Text(form.QuerySelector(".list-search-count")!));
        Assert.Equal(
            [("users", "Visitors"), ("screen", "Liste des albums")],
            form.QuerySelectorAll("input[type='hidden']").Select(field => (field.GetAttribute("name")!, field.GetAttribute("value")!)));
    }

    [Fact]
    public void ActiveFilters_EachATagLeadingToThePageWithoutIt_ThenEverythingCleared()
    {
        var page = Open("q=album&period=ThreeMonths&engine=Gecko&page=2");

        var tags = page.FindAll(".active-filters li");
        Assert.Equal(["Période : 3 derniers mois", "Moteur de rendu : Gecko"], tags.Select(tag => Text(tag.QuerySelector(".tag > span")!)));
        Assert.Equal(
            ["/admin/statistics?q=album&engine=Gecko", "/admin/statistics?q=album&period=ThreeMonths"],
            tags.Select(tag => tag.QuerySelector("a")!.GetAttribute("href")));
        Assert.Equal("/admin/statistics?q=album", page.Find(".active-filters > a").GetAttribute("href"));
    }

    [Fact]
    public void Search_TheFunctionsWhoseNameContainsTheText_WithoutCaseNorAccents()
    {
        var page = Open("q=FICHE%20D%27UN%20ALBUM");

        Assert.Equal(
            ["Fiche d'un album", "Fiche d'un album · visuel montré"],
            page.FindAll(".audience-row:not(.audience-row-head) .row-entry").Select(entry => Text(entry)));
        Assert.Equal(("q", "FICHE D'UN ALBUM"), (page.Find("input[type='search']").GetAttribute("name")!, page.Find("input[type='search']").GetAttribute("value")!));
    }

    [Fact]
    public void Search_ThatFindsNothing_SaysSo_AndOffersToClearIt()
    {
        var page = Open("q=nulle%20part&users=Visitors");

        Assert.Equal("Aucune fonction ne correspond à « nulle part ».", Text(page.Find(".empty-list strong")));
        Assert.Empty(page.FindAll(".audience-rows"));
        Assert.Contains(page.FindAll(".empty-list a"), link => link.GetAttribute("href") == "/admin/statistics?users=Visitors");
    }

    [Fact]
    public void Screen_ItsOwnFunctionsOnly_ItAndItsActions()
    {
        var page = Open("screen=Liste%20des%20albums");

        var names = page.FindAll(".audience-row:not(.audience-row-head) .row-entry").Select(entry => Text(entry)).ToList();
        Assert.Equal(["Liste des albums", "Liste des albums · recherche"], names.Take(2));
        Assert.All(names, name => Assert.True(name == "Liste des albums" || name.StartsWith("Liste des albums · ", StringComparison.Ordinal), name));
        Assert.Equal(AudienceFunctions.Followed.Count(f => f.Key.StartsWith("admin.albums", StringComparison.Ordinal)), names.Count);
        Assert.StartsWith("2 fonctions utilisées sur 8 suivies", Text(page.Find(".audience-summary")));
    }

    [Fact]
    public void Screens_EveryScreen_OnceByName_TheirsAndThoseOfTheActionsWithoutAScreen()
    {
        var page = Open("filters=open");

        var screens = page.FindAll("select[name='screen'] option").Skip(1).Select(option => Text(option)).ToList();
        Assert.Equal(screens.Distinct(), screens);
        Assert.Contains("Liste des albums", screens);
        Assert.Contains("Administration", screens);
        Assert.DoesNotContain(screens, screen => screen.Contains(" · ", StringComparison.Ordinal));
    }

    [Fact]
    public void ScreenItDoesNotKnow_EveryScreen()
    {
        var page = Open("screen=Nulle%20part&filters=open");

        Assert.Equal(AudienceIndicatorsQuery.PageSize, page.FindAll(".audience-row:not(.audience-row-head)").Count);
        Assert.Equal("Tous les écrans", Text(page.Find("select[name='screen'] option[selected]")));
    }

    [Fact]
    public void Indicators_ReadForTheCriteria_UpToTheCurrentMonthInUniversalTime()
    {
        Open("users=Administrator&device=Computer&engine=Gecko");

        Assert.Contains($"{Indicators}?from=2025-12-01&administrator=true&deviceType=Computer&renderingEngine=Gecko", _api.Requested);
    }

    [Fact]
    public void EveryFunctionFollowed_ThoseUsed_TheMostUsedFirst_ThenThoseNeverUsed_ByName_PageByPage()
    {
        var count = AudienceFunctions.Followed.Count + 1;
        var pages = (count + AudienceIndicatorsQuery.PageSize - 1) / AudienceIndicatorsQuery.PageSize;

        var names = Enumerable.Range(1, pages)
            .SelectMany(number => Open($"page={number}").FindAll(".audience-row:not(.audience-row-head) .row-entry").Select(entry => Text(entry)).ToList())
            .ToList();

        Assert.Equal(["Fiche d'un album", "Liste des albums", "Liste des albums · recherche", "admin.former"], names.Take(4));
        // Every other function followed, never used, by its name, each on a single page.
        var never = names.Skip(4).ToList();
        Assert.Equal(AudienceFunctions.Followed.Count - 3, never.Count);
        Assert.Equal(never.Order(StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true)), never);
    }

    [Fact]
    public void Page_AFewFunctionsOnly_TheOthersOnTheNextPages_WithTheSameCriteria()
    {
        var page = Open("users=Visitors");

        Assert.Equal(AudienceIndicatorsQuery.PageSize, page.FindAll(".audience-row:not(.audience-row-head)").Count);
        var next = page.Find(".pagination a[rel='next']");
        Assert.Equal("/admin/statistics?users=Visitors&page=2", next.GetAttribute("href"));
        Assert.StartsWith("Fonctions 1 à 20 sur", Text(page.Find(".pagination-position")));
    }

    [Fact]
    public void PageBeyondTheLast_LeadsToTheLast()
    {
        Open("period=All&page=99");

        var count = AudienceFunctions.Followed.Count + 1;
        var last = (count + AudienceIndicatorsQuery.PageSize - 1) / AudienceIndicatorsQuery.PageSize;
        Assert.EndsWith($"/admin/statistics?period=All&page={last}", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Summary_TheFunctionsUsed_AmongThoseFollowed_AndThoseNeverUsed()
    {
        var page = Open("");

        var count = AudienceFunctions.Followed.Count + 1;
        Assert.Equal($"4 fonctions utilisées sur {count} suivies · {count - 4} jamais utilisées", Text(page.Find(".audience-summary")));
    }

    [Fact]
    public void FunctionUsed_ItsIndicators_UnderTheHeadsOfTheirColumns()
    {
        var page = Open("");

        Assert.Equal(
            ["Fonction", "Utilisations", "Visites", "Fiches ou paramètres", "Répétitions", "Fréquence", "Dernière utilisation"],
            page.FindAll(".audience-row-head [role='columnheader']").Select(head => Text(head)));
        var album = RowOf(page, "Fiche d'un album");
        Assert.Equal("42 utilisations", Text(album.QuerySelector(".audience-usages")!));
        Assert.Equal("20 visites", Text(album.QuerySelector(".audience-visits")!));
        Assert.Equal("17 fiches différentes", Text(album.QuerySelector(".audience-objects")!));
        // 3 repeats out of 42 usages.
        Assert.Equal("7 % de répétitions", Text(album.QuerySelector(".audience-repeats")!));
        Assert.Equal("dernière le 20/11/2026", Text(album.QuerySelector(".audience-last")!));
        Assert.Equal("4 paramètres différents", Text(RowOf(page, "Liste des albums · recherche").QuerySelector(".audience-objects")!));
    }

    [Fact]
    public void LastUsage_TheDateOfTheBrowser()
    {
        var page = Open("");

        // 23:30 in universal time on the 2nd is already the 3rd in Paris.
        Assert.Equal("dernière le 03/11/2026", Text(RowOf(page, "Liste des albums").QuerySelector(".audience-last")!));
    }

    [Fact]
    public void ShareOfRepeats_NeverZero_ForARepeat()
    {
        _api.Answers[Indicators] = new List<FunctionIndicators> { AlbumRecord with { UsageCount = 1000, RepeatedUsageCount = 1 } };

        var page = Open("");

        Assert.Equal("1 % de répétitions", Text(RowOf(page, "Fiche d'un album").QuerySelector(".audience-repeats")!));
    }

    [Fact]
    public void ZeroOfAFunctionUsed_ItsDash_ItsMentionToAScreenReader()
    {
        var page = Open("");

        var albums = RowOf(page, "Liste des albums");
        Assert.Equal("—Aucune fiche présentée", Text(albums.QuerySelector(".audience-objects .not-set")!));
        Assert.Equal("—Aucune répétition", Text(albums.QuerySelector(".audience-repeats .not-set")!));
        Assert.NotNull(albums.QuerySelector(".audience-objects[data-zero]"));
        Assert.Null(RowOf(page, "Fiche d'un album").QuerySelector("[data-zero]"));
        Assert.Equal("—Aucun paramètre", Text(RowOf(page, "Connexion à l'administration · connexion réussie").QuerySelector(".audience-objects")!));
    }

    [Fact]
    public void FunctionNeverUsed_NullIndicators_SaidInWords()
    {
        var page = Open("");

        var never = RowOf(page, "Administration · déconnexion");
        Assert.True(never.HasAttribute("data-unused"));
        Assert.False(RowOf(page, "Fiche d'un album").HasAttribute("data-unused"));
        Assert.Equal("Jamais utilisée", Text(never.QuerySelector(".audience-never")!));
        Assert.All(
            never.QuerySelectorAll("[role='cell']").Skip(1),
            cell => Assert.Equal("—", Text(cell.QuerySelector(".not-set [aria-hidden='true']")!)));
        Assert.Null(never.QuerySelector(".frequency"));
    }

    [Fact]
    public void Frequency_AMonthOfThePeriod_EachAsHighAsItsUsages_AmongTheBusiestOfThePage()
    {
        var page = Open("");

        var frequency = RowOf(page, "Fiche d'un album").QuerySelector(".frequency")!;
        Assert.Equal("Utilisée 2 mois sur 12", frequency.GetAttribute("aria-label"));
        var months = frequency.QuerySelectorAll(".frequency-month");
        Assert.Equal(12, months.Length);
        Assert.Equal("décembre 2025 : aucune utilisation", months[0].GetAttribute("title"));
        Assert.Equal("octobre 2026 : 2 utilisations", months[10].GetAttribute("title"));
        Assert.Equal("novembre 2026 : 40 utilisations", months[11].GetAttribute("title"));
        // November of the record of an album is the busiest month of the page.
        Assert.Equal("--frequency-level: 1", months[11].GetAttribute("style"));
        Assert.Equal("--frequency-level: 0.05", months[10].GetAttribute("style"));
        Assert.Equal("--frequency-level: 0.5", RowOf(page, "Liste des albums").QuerySelectorAll(".frequency-month")[11].GetAttribute("style"));
        Assert.Equal(10, months.Count(month => month.HasAttribute("data-empty")));
    }

    [Fact]
    public void Frequency_SinceTheBeginning_FromTheFirstMonthMeasured()
    {
        var page = Open("period=All");

        Assert.Contains(Indicators, _api.Requested);
        var frequency = RowOf(page, "Fiche d'un album").QuerySelector(".frequency")!;
        Assert.Equal("Utilisée 2 mois sur 3", frequency.GetAttribute("aria-label"));
        Assert.Equal("septembre 2026 : aucune utilisation", frequency.QuerySelector(".frequency-month")!.GetAttribute("title"));
    }

    [Fact]
    public void NothingMeasuredYet_EveryFunctionNeverUsed_OverTheCurrentMonthAlone()
    {
        _api.Answers[Indicators] = new List<FunctionIndicators>();

        var page = Open("period=All");

        Assert.Equal(
            $"0 fonction utilisée sur {AudienceFunctions.Followed.Count} suivies · {AudienceFunctions.Followed.Count} jamais utilisées",
            Text(page.Find(".audience-summary")));
        Assert.All(page.FindAll(".audience-row:not(.audience-row-head)"), row => Assert.True(row.HasAttribute("data-unused")));
    }

    [Fact]
    public void AnErrorOfTheApi_IsPresentedInsteadOfTheIndicators_TheCriteriaKept()
    {
        _api.Failures[Indicators] = HttpStatusCode.InternalServerError;

        var page = Open("users=Visitors");

        Assert.Equal("technical", page.Find("[data-error-category]").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".audience-rows"));
        Assert.NotNull(page.Find("form.list-search"));
    }

    // The measurement does not measure itself (fonctionnel/mesure-audience.md, Fonctions suivies): neither the display of the
    // page, nor its criteria, nor its pages.
    [Theory]
    [InlineData("")]
    [InlineData("period=ThreeMonths&engine=Blink")]
    [InlineData("users=Visitors&page=2")]
    public void Display_RecordsNothing(string criteria)
    {
        Open(criteria, recorded: true);

        Assert.Empty(_audience.Sent);
    }

    private IRenderedComponent<AdminStatistics> Open(string criteria, bool recorded = false)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.AdminStatistics}?{criteria}");
        return Render<AdminStatistics>(parameters =>
        {
            if (recorded)
                parameters.AddCascadingValue(_request.Context);
        });
    }

    private static IElement RowOf(IRenderedComponent<AdminStatistics> page, string name) =>
        page.FindAll(".audience-row:not(.audience-row-head)").Single(row => Text(row.QuerySelector(".row-entry")!) == name);

    private static string Text(IElement element) => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
