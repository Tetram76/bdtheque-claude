using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AngleSharp.Dom;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Audience;
using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Navigation;
using Bdtheque.Frontend.Tests.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The record of a series in the consultation (visuel/fiche-serie/contenu.md): its pile of covers, its identity and progress,
/// its publisher and collection, its authors, genres and universes, its summary and notes, its albums in the order of the
/// series as cards, in two groups, and its dates, told apart by role, link and text, never by appearance.
/// </summary>
public sealed class SeriesRecordTests : BunitContext, IDisposable
{
    private static readonly Guid SeriesId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000b1");
    private static readonly Guid[] AlbumIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
    private static readonly Guid FirstCover = Guid.NewGuid();
    private static readonly DateTimeOffset Created = new(2026, 2, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Modified = new(2026, 10, 9, 23, 30, 0, TimeSpan.Zero);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeCatalogApi _catalog;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() });

    public SeriesRecordTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _catalog = new FakeCatalogApi(_audience);
        Services.AddLocalization();
        _audience.AddServices(Services, _request, new ApiClient(new HttpClient(_catalog) { BaseAddress = new Uri("http://api") }));
        // Paris, an hour ahead: a modification at 23:30 universal time is of the next day.
        Services.AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" });
        Services.AddScoped<DisplayFormatter>();
        Services.AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>();
        Services.AddSingleton<ValueLabels>();
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Record_ReadsTheSeriesOfItsAddress()
    {
        _catalog.Series = Series();

        Open();

        Assert.Contains($"/catalog/series/{SeriesId}", _catalog.Requested);
    }

    [Fact]
    public void Breadcrumb_LeadsToTheSeries_ThenNamesTheSeries()
    {
        _catalog.Series = Series();

        var page = Open();

        Assert.Equal(["Séries", "Corto Maltese"], page.FindAll("nav.breadcrumb li").Select(l => l.TextContent.Trim()));
        Assert.Equal([AppPaths.Series], page.FindAll("nav.breadcrumb a").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void Identity_TellsStatus_PlannedVolumes_AndWhetherComplete()
    {
        _catalog.Series = Series();

        var page = Open();

        Assert.Equal("Corto Maltese", page.Find("h1").TextContent.Trim());
        Assert.Equal(["Terminée", "12 tomes prévus", "À compléter"], page.FindAll(".series-facts li").Select(f => f.TextContent.Trim()));
    }

    [Fact]
    public void Identity_OfACompleteSeries_SaysSo_AndAMissingStatusOrPlannedCountIsSaidNotSet()
    {
        _catalog.Series = Series() with { Status = null, TheoreticalVolumeCount = null, IsComplete = true };

        var page = Open();

        Assert.Equal(["Statut non renseigné", "Nombre de tomes prévus non renseigné", "Complète"],
            page.FindAll(".series-facts li").Select(f => f.TextContent.Trim()));
        Assert.Equal(2, page.FindAll(".series-facts li[data-not-set]").Count);
    }

    [Fact]
    public void Identity_OfASingleVolumePlanned_IsSingular()
    {
        _catalog.Series = Series() with { TheoreticalVolumeCount = 1 };

        Assert.Contains("1 tome prévu", Open().FindAll(".series-facts li").Select(f => f.TextContent.Trim()));
    }

    [Fact]
    public void Progress_CountsTheAlbumsOfTheCollection_AmongThoseOfTheSeries_WithABar()
    {
        _catalog.Series = Series();

        var page = Open();

        Assert.Equal("3 albums sur 5 dans la collection", page.Find(".series-progress-label").TextContent.Trim());
        Assert.Equal("--progress: 60%", page.Find(".series-progress-bar").GetAttribute("style"));
    }

    [Fact]
    public void Progress_OfASingleAlbumOfTheCollection_IsSingular()
    {
        _catalog.Series = Series() with { Albums = [Series().Albums[0]] };

        Assert.Equal("1 album sur 1 dans la collection", Open().Find(".series-progress-label").TextContent.Trim());
    }

    [Fact]
    public void Publisher_LeadsToItsWebsite_InANewTab_AndTheCollectionIsNoLink()
    {
        _catalog.Series = Series();

        var page = Open();

        var publisher = page.Find(".series-publishing a.external");
        Assert.Equal(("Casterman", "https://www.casterman.com", "_blank", "noopener noreferrer"),
            (publisher.FirstChild!.TextContent.Trim(), publisher.GetAttribute("href"), publisher.GetAttribute("target"), publisher.GetAttribute("rel")));
        Assert.Equal(["Casterman, site externe", "Univers d'auteurs"], page.FindAll(".series-publishing dd").Select(d => d.TextContent.Trim()));
        Assert.Single(page.FindAll(".series-publishing a"));
    }

    [Fact]
    public void Publisher_WithoutWebsite_IsNoLink_AndMissingOnesAreSaidNotSet()
    {
        _catalog.Series = Series() with { Publisher = new PublisherListItem(Guid.NewGuid(), "Casterman", null) };
        Assert.Empty(Open().FindAll(".series-publishing a"));

        _catalog.Series = Series() with { Publisher = null, PublisherCollection = null };
        var page = Open();
        Assert.Equal(["Non renseigné", "Non renseignée"], page.FindAll(".series-publishing dd").Select(d => d.TextContent.Trim()));
        Assert.Empty(page.FindAll(".series-publishing a"));
    }

    [Fact]
    public void Authors_AreTheContributionsOfTheSeries_ByRole_LeadingToTheAuthors()
    {
        _catalog.Series = Series();

        var page = Open();

        Assert.Equal(["Scénario", "Dessin"], page.FindAll(".contributions dt .contributions-role").Select(r => r.TextContent.Trim()));
        Assert.Equal(2, page.FindAll(".contributions a.contributor").Count);
    }

    [Fact]
    public void Authors_OfASeriesWithoutAny_SaySo()
    {
        _catalog.Series = Series() with { Contributions = [] };

        Assert.Equal("Aucun auteur crédité sur la série", Open().Find(".contributions-none").TextContent.Trim());
    }

    [Fact]
    public void GenresAndUniverses_LeadToTheirWorks_EachUniverseWithItsAncestors()
    {
        _catalog.Series = Series();

        var page = Open();

        Assert.Equal(["Aventure"], page.FindAll(".classification a.genre").Select(g => g.TextContent.Trim()));
        Assert.Equal(["Univers Hugo Pratt", "Corto Maltese"], page.FindAll(".classification .universe-chain a").Select(u => u.TextContent.Trim()));
    }

    [Fact]
    public void Summary_IsShown_OrSaidNotSet_AndTheNotes_OrNone()
    {
        _catalog.Series = Series();
        var block = BlockOf(Open(), "De quoi ça parle ?");
        Assert.Contains("Les aventures d'un marin.", block.TextContent);
        Assert.Contains("Lue en intégrales.", block.TextContent);

        _catalog.Series = Series() with { Summary = null, PersonalNotes = null };
        block = BlockOf(Open(), "De quoi ça parle ?");
        Assert.Equal("Non renseigné", block.QuerySelector(".series-summary")!.TextContent.Trim());
        Assert.Equal("Aucune note", block.QuerySelector(".series-notes")!.TextContent.Trim());
    }

    [Fact]
    public void Albums_AreCounted_ByKind()
    {
        _catalog.Series = Series();

        Assert.Equal("5 albums : 3 tomes, 1 intégrale, 1 hors-série", BlockOf(Open(), "Combien d'albums ?").QuerySelector(".series-albums-count")!.TextContent.Trim());
    }

    [Fact]
    public void Albums_AreInTheOrderOfTheSeries_InTwoGroups_EachACardLeadingToItsRecord()
    {
        _catalog.Series = Series();

        var block = BlockOf(Open(), "Combien d'albums ?");

        var groups = block.QuerySelectorAll(".series-group");
        Assert.Equal(["Tomes et intégrales (4)", "Hors-séries (1)"], groups.Select(g => Normalized(g.QuerySelector("h3")!.TextContent)));
        var cards = block.QuerySelectorAll(".album-card");
        Assert.Equal(AlbumIds.Select(AppPaths.Album), cards.Select(c => c.QuerySelector("a")!.GetAttribute("href")));
        Assert.Equal(["Premier", "Le Premier Cycle", "Cinquième", "Tome 6", "Carnets"], cards.Select(c => c.QuerySelector(".album-card-title")!.TextContent.Trim()));
        // An album without its own title is named by its volume alone, which it does not repeat.
        Assert.Equal(["T. 1", "INT. [1 à 3]", "T. 5", null, "HS 1"], cards.Select(c => c.QuerySelector(".album-card-volume")?.TextContent.Trim()));
    }

    [Fact]
    public void AlbumCards_TellTheirRating_WhenSet_AndTheirMembership()
    {
        _catalog.Series = Series();

        var cards = Open().FindAll(".album-card");

        Assert.Equal(["Appréciation : Bien, 4 sur 5", null, null, null, null], cards.Select(c => c.QuerySelector("[role='img']")?.GetAttribute("aria-label")));
        Assert.Equal(["Dans la collection", "Dans la collection", "Intention d'achat", "Hors collection", "Dans la collection"],
            cards.Select(c => c.QuerySelector(".tag")!.TextContent.Trim()));
    }

    [Fact]
    public void AlbumCards_ShowTheCoverOfTheAlbum_OrTheGenericVisual()
    {
        _catalog.Series = Series();

        var cards = Open().FindAll(".album-card");

        Assert.Equal(VisualMediaRoutes.Display(FirstCover), cards[0].QuerySelector("img.cover")!.GetAttribute("src"));
        Assert.NotNull(cards[1].QuerySelector("svg.cover-generic"));
    }

    [Fact]
    public void Pile_KeepsThreeCards_TheCoversOfTheFirstVolumes_ThenBlankCards()
    {
        _catalog.Series = Series();

        var pile = Open().Find(".series-pile");

        var slots = pile.QuerySelectorAll(".cover-slot");
        Assert.Equal(3, slots.Length);
        // The first volume in front, last in the markup.
        Assert.Equal(VisualMediaRoutes.Display(FirstCover), slots[2].QuerySelector("img")!.GetAttribute("src"));
        Assert.NotNull(slots[1].QuerySelector("svg.cover-generic"));
        Assert.NotNull(slots[0].QuerySelector("svg.cover-blank"));
    }

    [Fact]
    public void SeriesWithoutAlbum_SaysSo_WithoutBarNorGroups()
    {
        _catalog.Series = Series() with { Albums = [], Overview = new SeriesOverview(0, []) };

        var page = Open();

        Assert.Equal("Aucun album dans la série pour l'instant", page.Find(".series-progress-label").TextContent.Trim());
        Assert.Empty(page.FindAll(".series-progress-bar"));
        var block = BlockOf(page, "Combien d'albums ?");
        Assert.Equal("Aucun album dans cette série pour l'instant.", block.QuerySelector(".series-albums-none")!.TextContent.Trim());
        Assert.Empty(block.QuerySelectorAll(".series-group"));
        Assert.Empty(block.QuerySelectorAll(".series-albums-count"));
        Assert.Equal(3, page.FindAll(".series-pile svg.cover-blank").Count);
    }

    [Fact]
    public void SeriesWithoutSpecialIssue_HasASingleGroup()
    {
        _catalog.Series = Series() with { Albums = [.. Series().Albums.Take(4)] };

        var block = BlockOf(Open(), "Combien d'albums ?");

        Assert.Equal(["Tomes et intégrales (4)"], block.QuerySelectorAll(".series-group h3").Select(h => Normalized(h.TextContent)));
        Assert.Equal("4 albums : 3 tomes, 1 intégrale", block.QuerySelector(".series-albums-count")!.TextContent.Trim());
    }

    [Fact]
    public void Foot_DatesTheSeries_InTheTimeZoneOfTheBrowser()
    {
        _catalog.Series = Series();

        Assert.Equal("Série créée le 02/02/2026, modifiée le 10/10/2026.", Open().Find(".series-dates").TextContent.Trim());
    }

    [Fact]
    public void Display_IsRecordedAsTheRecordOfTheSeries()
    {
        _catalog.Series = Series();

        var page = Open();

        var screen = page.FindComponent<AudienceScreen>().Instance;
        Assert.Equal(AudienceFunctions.SeriesRecord, screen.Function);
        Assert.Equal(new PresentedRecord(RecordType.Series, SeriesId), screen.Record);
        Assert.Equal(["series"], _audience.Functions);
    }

    [Fact]
    public void UnknownSeries_PresentsTheFunctionalError()
    {
        _catalog.SeriesStatus = HttpStatusCode.NotFound;

        Assert.Equal("functional", Open().Find("[role='alert']").GetAttribute("data-error-category"));
    }

    private IRenderedComponent<SeriesRecord> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.SeriesRecord(SeriesId));
        return Render<SeriesRecord>(parameters => parameters.Add(p => p.Id, SeriesId).AddCascadingValue(_request.Context));
    }

    private static IElement BlockOf(IRenderedComponent<SeriesRecord> page, string title) =>
        page.FindAll("section.block").Single(block => block.QuerySelector(".block-title")!.TextContent.Trim() == title);

    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static AlbumSummary Album(int index, string? title, int? volume, bool owned = true) =>
        new(AlbumIds[index], title, SeriesId, "Corto Maltese", AlbumType.Regular, false, volume, null, null, owned);

    // In the order of the series: a regular volume, owned, rated and illustrated; an omnibus; a volume wished for; a volume
    // without its own title, catalogued only; a special issue.
    private static List<AlbumCard> Albums() =>
    [
        new(Album(0, "Premier", 1), AlbumRating.Good, new AlbumCover(FirstCover, new CoverPalette("#335577", "#112233", "#DDAA44")), false),
        new(Album(1, "Le Premier Cycle", null) with { Type = AlbumType.Omnibus, StartVolumeNumber = 1, EndVolumeNumber = 3 }, null, null, false),
        new(Album(2, "Cinquième", 5, owned: false), null, null, true),
        new(Album(3, null, 6, owned: false), null, null, false),
        new(Album(4, "Carnets", 1) with { IsSpecialIssue = true }, null, null, false),
    ];

    private static SeriesDetail Series()
    {
        var pratt = new AuthorListItem(Guid.NewGuid(), "Pratt", "Hugo", null);
        var root = new UniverseListItem(Guid.NewGuid(), "Univers Hugo Pratt", null, null);
        var publisher = new PublisherListItem(Guid.NewGuid(), "Casterman", "https://www.casterman.com");
        return new SeriesDetail(
            SeriesId, "Corto Maltese", SeriesStatus.Completed, 12, false, "Les aventures d'un marin.", "Lue en intégrales.",
            publisher, new PublisherCollectionListItem(Guid.NewGuid(), "Univers d'auteurs", publisher.Id, publisher.Name),
            [new GenreListItem(Guid.NewGuid(), "Aventure")],
            [new UniverseInHierarchy(new UniverseListItem(Guid.NewGuid(), "Corto Maltese", root.Id, root.Name), [root])],
            [
                new ContributionItem(Guid.NewGuid(), pratt, ContributionRole.Scenarist, Created, Created),
                new ContributionItem(Guid.NewGuid(), pratt, ContributionRole.Illustrator, Created, Created),
            ],
            // The first regular volumes: the first with a cover, the fifth without; no third.
            new SeriesOverview(5, [new AlbumCover(FirstCover, new CoverPalette("#335577", "#112233", "#DDAA44")), null]),
            Albums(),
            Created,
            Modified);
    }

    /// <summary>The catalog endpoints the record reads, answering as a test chooses; the audience ones, relayed.</summary>
    private sealed class FakeCatalogApi(FakeAudienceApi audience) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _audience = new(audience);

        public SeriesDetail? Series { get; set; }

        public HttpStatusCode SeriesStatus { get; set; } = HttpStatusCode.OK;

        public List<string> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/audience/", StringComparison.Ordinal))
                return _audience.SendAsync(request, cancellationToken);

            Requested.Add(path);
            if (path == "/catalog/settings")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new ApplicationSettings(AlbumLabelFormat.TitleSeriesVolume, Contracts.Enums.DisplayCulture.FrenchFrance)),
                });
            if (SeriesStatus != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(SeriesStatus)
                {
                    Content = JsonContent.Create(new { type = ProblemTypes.Functional, status = (int)SeriesStatus },
                        mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Series) });
        }
    }
}
