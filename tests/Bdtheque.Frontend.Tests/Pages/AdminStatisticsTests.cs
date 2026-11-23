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
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 23), "base", new DateOnly(2026, 11, 23)).Format() },
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
        var page = Open("period=All&users=Visitors&device=Smartphone&engine=WebKit");

        var form = page.Find("form.audience-criteria");
        Assert.Equal(("get", AppPaths.AdminStatistics), (form.GetAttribute("method"), form.GetAttribute("action")));
        Assert.Equal(
            [("Période", "period", "Depuis le début"), ("Utilisateurs", "users", "Visiteurs"), ("Appareil", "device", "Smartphone"),
                ("Moteur de rendu", "engine", "WebKit")],
            form.QuerySelectorAll("select").Select(select => (
                Text(form.QuerySelector($"label[for='{select.Id}']")!), select.GetAttribute("name")!,
                Text(select.QuerySelector("option[selected]")!))));
        Assert.Equal(
            ["Mois en cours", "3 derniers mois", "12 derniers mois", "Depuis le début"],
            form.QuerySelectorAll("select[name='period'] option").Select(option => Text(option)));
        Assert.Equal(
            ["Visiteurs et administrateur", "Visiteurs", "Administrateur"],
            form.QuerySelectorAll("select[name='users'] option").Select(option => Text(option)));
        Assert.Equal(
            ["Tous les appareils", "Ordinateur", "Tablette", "Smartphone"],
            form.QuerySelectorAll("select[name='device'] option").Select(option => Text(option)));
        Assert.Equal(
            ["Tous les moteurs", "Blink", "Gecko", "WebKit", "Autre moteur"],
            form.QuerySelectorAll("select[name='engine'] option").Select(option => Text(option)));
        Assert.NotNull(form.QuerySelector("button[type='submit']"));
    }

    [Fact]
    public void Indicators_ReadForTheCriteria_UpToTheCurrentMonthInUniversalTime()
    {
        Open("users=Administrator&device=Computer&engine=Gecko");

        Assert.Contains($"{Indicators}?from=2025-12-01&administrator=true&deviceType=Computer&renderingEngine=Gecko", _api.Requested);
    }

    [Fact]
    public void EveryFunctionFollowed_ThoseUsed_TheMostUsedFirst_ThenThoseNeverUsed_ByName()
    {
        var page = Open("");

        var names = page.FindAll(".audience-row:not(.audience-row-head) .row-entry").Select(entry => Text(entry)).ToList();
        Assert.Equal(["Fiche d'un album", "Liste des albums", "Liste des albums · recherche", "admin.former"], names.Take(4));
        // Every other function followed, never used, by its name.
        var never = names.Skip(4).ToList();
        Assert.Equal(AudienceFunctions.Followed.Count - 3, never.Count);
        Assert.Equal(never.Order(StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true)), never);
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
        Assert.Equal("—Aucun paramètre", Text(RowOf(page, "Connexion à l'administration · connexion").QuerySelector(".audience-objects")!));
    }

    [Fact]
    public void FunctionNeverUsed_NullIndicators_SaidInWords()
    {
        var page = Open("");

        var never = RowOf(page, "Liste des genres");
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
        Assert.NotNull(page.Find("form.audience-criteria"));
    }

    [Fact]
    public void Display_RecordsTheScreen()
    {
        Open("", recorded: true);

        Assert.Equal(["admin.statistics"], _audience.Functions);
    }

    [Fact]
    public void OtherCriteria_RecordTheFilter_WithItsCriteria_BeforeTheScreen()
    {
        Open("period=ThreeMonths&engine=Blink", recorded: true);

        Assert.Equal(["admin.statistics.filter", "admin.statistics"], _audience.Functions);
        Assert.Contains(_audience.Sent, sent => sent.Content.GetRawText().Contains("period=ThreeMonths\\u0026engine=Blink", StringComparison.Ordinal));
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
