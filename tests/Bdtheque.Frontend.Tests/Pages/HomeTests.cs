using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Dashboard;
using Bdtheque.Frontend.Components.Layout;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The home page of the consultation (fonctionnel/page-accueil.md): the dashboard, or the welcome screen of an empty
/// collection (visuel/dashboard/collection-vide.md), told apart by role, link and text, never by appearance.
/// </summary>
public sealed class HomeTests : BunitContext, IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 23, 30, 0, TimeSpan.Zero);

    private static readonly string[] Greetings =
    [
        "Entrez donc, il y a de quoi lire !",
        "Installez-vous, la collection vous attend !",
        "Alors, on bouquine ?",
        "Encore de la place sur les étagères ? Pas sûr…",
        "Attention, ça déborde des étagères !",
    ];

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeCatalogApi _catalog;

    public HomeTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _catalog = new FakeCatalogApi(_audience);
        Services.AddLocalization();
        _audience.AddServices(Services, new TestRequest(), new ApiClient(new HttpClient(_catalog) { BaseAddress = new Uri("http://api") }));
        Services.AddSingleton<TimeProvider>(new FixedClock(Now));
        // Paris, an hour ahead: the date of the browser is already the next day.
        Services.AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" });
        Services.AddScoped<DisplayFormatter>();
        Services.AddSingleton<RecordLabels>();
        Services.AddSingleton(new Random(7));
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void EmptyCollection_WelcomesInsteadOfTheDashboard_WithoutSearchNorCounter()
    {
        _catalog.Dashboard = new Dashboard(null);

        var page = Render<Home>();

        Assert.Equal("Les étagères attendent leur premier album !", page.Find("h1").TextContent.Trim());
        Assert.Single(page.FindComponents<NoHeaderSearch>());
        Assert.Empty(page.FindAll("[data-counter]"));
        Assert.Contains("Le tableau de bord s'affichera dès la première édition entrée", page.Markup);
    }

    [Fact]
    public void EmptyCollection_LeadsToTheDocumentaryBase_AndToTheAdministration()
    {
        _catalog.Dashboard = new Dashboard(null);

        var page = Render<Home>();

        Assert.Equal(["Albums", "Séries", "Auteurs", "Recherche avancée"], LinksOf(page, "On jette un œil quand même ?").Select(l => l.Text));
        Assert.All(LinksOf(page, "On jette un œil quand même ?"), link => Assert.Equal(AppPaths.NotYetImplemented, link.Href));
        Assert.Equal([("Ouvrir l'administration", AppPaths.AdminHome)], LinksOf(page, "C'est votre collection ?").Select(l => (l.Text, l.Href)));
    }

    [Fact]
    public void Dashboard_GreetsWithOneOfItsPhrases_AndCountsTheCollection()
    {
        _catalog.Dashboard = Collection();

        var page = Render<Home>();

        Assert.Contains(page.Find("h1").TextContent.Trim(), Greetings);
        Assert.Equal(["1 234 albums", "56 séries", "1 300 éditions"],
            page.FindAll("[data-counter]").Select(c => c.TextContent.Trim().Replace(' ', ' ').Replace(' ', ' ')));
        Assert.Empty(page.FindComponents<NoHeaderSearch>());
    }

    [Fact]
    public void Dashboard_AsksForTheDayOfTheBrowser_AndTheLatestEntriesItPresents()
    {
        _catalog.Dashboard = Collection();

        Render<Home>();

        Assert.Contains("/catalog/dashboard?day=2026-11-06&latestCount=6", _catalog.Requested);
    }

    [Fact]
    public void Spotlight_PresentsTheEdition_ItsAlbumLeadingToItsRecord()
    {
        _catalog.Dashboard = Collection();

        var page = Render<Home>();

        var spotlight = BlockOf(page, "À la une aujourd'hui !");
        Assert.Equal(AppPaths.Album(AlbumId), spotlight.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal("Le Lotus bleu (Tintin - T. 5)", spotlight.QuerySelector("a")!.TextContent.Trim());
        Assert.Contains("Casterman [1946]", spotlight.TextContent);
        Assert.Contains("Tintin en Chine.", spotlight.TextContent);
    }

    [Fact]
    public void Labels_FollowTheFormatOfTheSettings()
    {
        _catalog.Settings = new ApplicationSettings(AlbumLabelFormat.VolumeTitleSeries, Contracts.Enums.DisplayCulture.FrenchFrance);
        _catalog.Dashboard = Collection();

        var page = Render<Home>();

        Assert.Equal("T. 5 - Le Lotus bleu (Tintin)", BlockOf(page, "À la une aujourd'hui !").QuerySelector("a")!.TextContent.Trim());
    }

    [Fact]
    public void LatestEntries_FromTheMostRecent_DatedByTheirAcquisition_OrTheirEntryInTheBrowserTimeZone()
    {
        _catalog.Dashboard = Collection();

        var page = Render<Home>();

        var entries = BlockOf(page, "Tout juste arrivés !").QuerySelectorAll("li");
        Assert.Equal(2, entries.Length);
        Assert.Contains("Le Lotus bleu (Tintin - T. 5)", entries[0].TextContent);
        Assert.Contains("01/11/2026", entries[0].TextContent);
        Assert.Contains("Astérix", entries[1].TextContent);
        // Entered at 23:30 universal time, the 4th: already the 5th in Paris.
        Assert.Contains("05/10/2026", entries[1].TextContent);
        Assert.Equal(AppPaths.Albums, BlockOf(page, "Tout juste arrivés !").QuerySelectorAll("a").Last().GetAttribute("href"));
    }

    [Fact]
    public void BlocksNotComputableYet_AreShownWithoutAnyFigure()
    {
        _catalog.Dashboard = Collection();

        var page = Render<Home>();

        var value = BlockOf(page, "Ça vaut combien, tout ça ?");
        Assert.DoesNotMatch(@"\d", value.TextContent);
        Assert.DoesNotContain("€", value.TextContent);
    }

    [Fact]
    public void ApiOutOfReach_PresentsATechnicalError()
    {
        _catalog.DashboardStatus = HttpStatusCode.ServiceUnavailable;

        var page = Render<Home>();

        Assert.Equal("technical", page.Find("[role='alert']").GetAttribute("data-error-category"));
    }

    [Fact]
    public void Display_IsRecordedAsTheHomeScreen_WithoutRecord()
    {
        _catalog.Dashboard = Collection();

        var page = Render<Home>();

        var screen = page.FindComponent<Bdtheque.Frontend.Components.Audience.AudienceScreen>().Instance;
        Assert.Equal(AudienceFunctions.Home, screen.Function);
        Assert.Null(screen.Record);
    }

    private static readonly Guid AlbumId = Guid.CreateVersion7();

    private static Dashboard Collection()
    {
        var lotus = new AlbumSummary(AlbumId, "Le Lotus bleu", Guid.NewGuid(), "Tintin", AlbumType.Regular, false, 5, null, null, true);
        var casterman = new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Casterman", null, null, 1946, null);
        var asterix = new AlbumSummary(Guid.NewGuid(), "Astérix le Gaulois", Guid.NewGuid(), "Astérix", AlbumType.Regular, false, 1, null, null, true);
        var dargaud = new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Dargaud", null, null, 1961, null);
        return new Dashboard(new DashboardContent(
            new DashboardCounters(1234, 56, 1300),
            new SpotlightEdition(lotus, casterman, null, "Tintin en Chine."),
            [
                new LatestEntry(lotus, casterman, null, new DateOnly(2026, 11, 1), new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero)),
                new LatestEntry(asterix, dargaud, null, null, new DateTimeOffset(2026, 10, 4, 23, 30, 0, TimeSpan.Zero)),
            ],
            AreValueIndicatorsComputable: false,
            AreRandomStatisticsComputable: false));
    }

    private static AngleSharp.Dom.IElement BlockOf(IRenderedComponent<Home> page, string title) =>
        page.FindAll("section.block").Single(block => block.QuerySelector(".block-title")!.TextContent.Trim() == title);

    private static IEnumerable<(string Text, string? Href)> LinksOf(IRenderedComponent<Home> page, string block) =>
        BlockOf(page, block).QuerySelectorAll("a").Select(link => (link.TextContent.Trim().TrimEnd('→').Trim(), link.GetAttribute("href")));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>The catalog endpoints the home page reads, answering as a test chooses; the audience ones, relayed.</summary>
    private sealed class FakeCatalogApi(FakeAudienceApi audience) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _audience = new(audience);

        public ApplicationSettings Settings { get; set; } = new(AlbumLabelFormat.TitleSeriesVolume, Contracts.Enums.DisplayCulture.FrenchFrance);

        public Dashboard Dashboard { get; set; } = new(null);

        public HttpStatusCode DashboardStatus { get; set; } = HttpStatusCode.OK;

        public List<string> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/audience/", StringComparison.Ordinal))
                return _audience.SendAsync(request, cancellationToken);

            Requested.Add(path);
            if (path == "/catalog/settings")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Settings) });
            if (DashboardStatus != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(DashboardStatus)
                {
                    Content = JsonContent.Create(new { type = ProblemTypes.Technical, status = (int)DashboardStatus },
                        mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Dashboard) });
        }
    }
}
