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
/// The record of an album in the consultation (visuel/fiche-album/contenu.md): its identity, its contributions by role, its
/// genres and universes, its summary and its dates, told apart by role, link and text, never by appearance.
/// </summary>
public sealed class AlbumRecordTests : BunitContext, IDisposable
{
    private static readonly Guid AlbumId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000a1");
    private static readonly Guid SeriesId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000b1");
    private static readonly Guid FirstEditionId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e1");
    private static readonly Guid SecondEditionId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e2");
    private static readonly Guid IntendedEditionId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e3");
    private static readonly DateTimeOffset Created = new(2026, 2, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Modified = new(2026, 10, 9, 23, 30, 0, TimeSpan.Zero);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeCatalogApi _catalog;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() });

    public AlbumRecordTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _catalog = new FakeCatalogApi(_audience);
        Services.AddLocalization();
        _audience.AddServices(Services, _request, new ApiClient(new HttpClient(_catalog) { BaseAddress = new Uri("http://api") }));
        // Paris, an hour ahead: a modification at 23:30 universal time is of the next day.
        Services.AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" });
        Services.AddScoped<DisplayFormatter>();
        Services.AddSingleton<RecordLabels>();
        Services.AddSingleton<ValueLabels>();
    }

    void IDisposable.Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        Dispose();
    }

    [Fact]
    public void Record_ReadsTheAlbumOfItsAddress()
    {
        _catalog.Album = Album();

        Open();

        Assert.Contains($"/catalog/albums/{AlbumId}", _catalog.Requested);
    }

    [Fact]
    public void Breadcrumb_LeadsToTheAlbums_ThenToTheSeries_ThenNamesTheAlbum()
    {
        _catalog.Album = Album();

        var page = Open();

        var levels = page.FindAll("nav.breadcrumb li");
        Assert.Equal(["Albums", "Corto Maltese", "Lointaines îles du vent"], levels.Select(l => l.TextContent.Trim()));
        Assert.Equal([AppPaths.Albums, AppPaths.SeriesRecord(SeriesId)], page.FindAll("nav.breadcrumb a").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void Identity_LeadsToTheSeries_AndTellsVolumePublicationAndMembership()
    {
        _catalog.Album = Album();

        var page = Open();

        Assert.Equal("Lointaines îles du vent", page.Find("h1").TextContent.Trim());
        Assert.Equal(AppPaths.SeriesRecord(SeriesId), page.Find(".album-identity a.album-series").GetAttribute("href"));
        Assert.Equal(["T. 5", "Paru en novembre 1970", "Dans la collection, en 2 éditions"],
            page.FindAll(".album-facts li").Select(f => f.TextContent.Trim()));
        // An album of the collection has no banner: only those out of it are marked.
        Assert.Empty(page.FindAll(".album-status"));
    }

    [Fact]
    public void Identity_OfAnAlbumWithoutTitle_IsItsLabel_AndTheTitleOfThePage()
    {
        _catalog.Album = Album() with { Album = Summary() with { Title = null } };

        var page = Open();

        Assert.Equal("Corto Maltese - T. 5", page.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void Identity_FollowsTheFormatOfTheSettings_InTheTitleOfThePage()
    {
        _catalog.Settings = new ApplicationSettings(AlbumLabelFormat.VolumeTitleSeries, Contracts.Enums.DisplayCulture.FrenchFrance);
        _catalog.Album = Album() with { Album = Summary() with { Title = null } };

        var page = Open();

        Assert.Equal("T. 5 - Corto Maltese", page.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void Rating_IsFiveStars_OrNotRatedYet()
    {
        _catalog.Album = Album() with { Rating = AlbumRating.Good };
        var rated = Open();
        Assert.Equal("Appréciation : Bien, 4 sur 5", rated.Find(".album-rating [role='img']").GetAttribute("aria-label"));
        Assert.Equal(4, rated.FindAll(".album-rating .star[data-on]").Count);

        _catalog.Album = Album() with { Rating = null };
        var unrated = Open();
        Assert.Contains("Pas encore noté", unrated.Find(".album-rating").TextContent);
        Assert.Empty(unrated.FindAll(".album-rating .star[data-on]"));
    }

    [Fact]
    public void Contributions_CountTheDistinctAuthors_AndListEachRole_LeadingToTheAuthors()
    {
        _catalog.Album = Album();

        var page = Open();

        var contributions = page.Find(".contributions");
        Assert.Equal("2 auteurs crédités", contributions.QuerySelector(".contributions-total")!.TextContent.Trim());
        Assert.Equal(["Scénario", "Dessin", "Couleurs"], contributions.QuerySelectorAll("dt .contributions-role").Select(r => r.TextContent.Trim()));
        var scenario = contributions.QuerySelectorAll("dd")[0];
        Assert.Equal(["Hugo Pratt", "Léa Ferrand"], scenario.QuerySelectorAll("a").Select(a => a.TextContent.Trim()));
        Assert.All(scenario.QuerySelectorAll("a"), a => Assert.Equal(AppPaths.NotYetImplemented, a.GetAttribute("href")));
        Assert.Equal("Non crédité", contributions.QuerySelectorAll("dd")[2].TextContent.Trim());
    }

    [Fact]
    public void Contributions_OfAnAlbumWithoutAny_SaySo()
    {
        _catalog.Album = Album() with { Contributions = [] };

        var page = Open();

        Assert.Equal("Aucun auteur crédité", page.Find(".contributions-total").TextContent.Trim());
    }

    [Fact]
    public void Contributions_NameEveryAuthorOfARole_WithTheLinkThatUnfoldsWhatItsLineDoesNotHold()
    {
        var authors = Enumerable.Range(1, 9).Select(n => new AuthorListItem(Guid.NewGuid(), $"Auteur {n}", null, null)).ToList();
        _catalog.Album = Album() with
        {
            Contributions = [.. authors.Select(a => new ContributionItem(Guid.NewGuid(), a, ContributionRole.Illustrator, Created, Created))],
        };

        var page = Open();

        // Which names the line holds, only the browser knows (wwwroot/fold-row.js): each is in the row, and the link is ready.
        var row = page.FindAll(".contributions dd")[1].QuerySelector("app-fold-row")!;
        Assert.Equal(authors.Select(a => a.LastName), row.QuerySelectorAll(".contributor").Select(a => a.TextContent.Trim()));
        var toggle = row.QuerySelector("details summary")!;
        Assert.Equal(("et {0} autre", "et {0} autres"),
            (toggle.QuerySelector(".contributions-more")!.GetAttribute("data-one"), toggle.QuerySelector(".contributions-more")!.GetAttribute("data-other")));
        Assert.Equal("Réduire", toggle.QuerySelector(".contributions-less")!.TextContent.Trim());
        Assert.Equal("(9)", page.FindAll(".contributions dt")[1].QuerySelector(".contributions-count")!.TextContent.Trim());
    }

    [Fact]
    public void GenresAndUniverses_LeadToTheirWorks_EachUniverseWithItsAncestors()
    {
        _catalog.Album = Album();

        var page = Open();

        var classification = page.Find(".classification");
        Assert.Equal(["Aventure"], classification.QuerySelectorAll(".classification-genres a").Select(a => a.TextContent.Trim()));
        var chain = classification.QuerySelector(".universe-chain")!;
        Assert.Equal(["Univers Hugo Pratt", "Corto Maltese", "Les Antilles"], chain.QuerySelectorAll("a").Select(a => a.TextContent.Trim()));
        Assert.Equal("Les Antilles", chain.QuerySelector("a.universe-attached")!.TextContent.Trim());
        Assert.All(classification.QuerySelectorAll("a"), a => Assert.Equal(AppPaths.NotYetImplemented, a.GetAttribute("href")));
    }

    [Fact]
    public void Summary_AndPersonalNotes_AreShown_OrSaidNotSet()
    {
        _catalog.Album = Album();
        Assert.Contains("Venezuela, vers 1913.", BlockOf(Open(), "Ça raconte quoi ?").TextContent);

        _catalog.Album = Album() with { Summary = null, PersonalNotes = null };
        var block = BlockOf(Open(), "Ça raconte quoi ?");
        Assert.Equal(2, block.QuerySelectorAll(".not-set").Length);
    }

    [Fact]
    public void AlbumTargetedByAnIntentOnItself_IsMarkedAsSuch()
    {
        _catalog.Album = Album() with
        {
            Album = Summary() with { IsInCollection = false },
            Editions = [],
            PurchaseIntent = new PurchaseIntentItem(Guid.NewGuid(), Modified, Modified),
        };

        var page = Open();

        Assert.Equal("Intention d'achat", page.FindAll(".album-facts li")[^1].TextContent.Trim());
        Assert.Equal(("intent", "Intention d'achat"), (page.Find(".album-status").GetAttribute("data-status"), page.Find(".album-status strong").TextContent.Trim()));
    }

    [Fact]
    public void AlbumWhoseOnlyEditionIsTargetedByAnIntent_IsMarkedAsSuch()
    {
        _catalog.Album = Album() with { Album = Summary() with { IsInCollection = false }, Editions = [Intended()] };

        var page = Open();

        Assert.Equal("Intention d'achat", page.FindAll(".album-facts li")[^1].TextContent.Trim());
        Assert.Equal(("intent", "Intention d'achat"), (page.Find(".album-status").GetAttribute("data-status"), page.Find(".album-status strong").TextContent.Trim()));
    }

    [Fact]
    public void AlbumNeitherOwnedNorTargeted_IsOutOfTheCollection()
    {
        _catalog.Album = Album() with { Album = Summary() with { IsInCollection = false }, Editions = [] };

        var page = Open();

        Assert.Equal("Hors collection", page.FindAll(".album-facts li")[^1].TextContent.Trim());
        Assert.Equal(("out", "Hors collection"), (page.Find(".album-status").GetAttribute("data-status"), page.Find(".album-status strong").TextContent.Trim()));
    }

    [Fact]
    public void AlbumWithoutSeries_HasNoSeriesInItsBreadcrumb_NorInItsIdentity()
    {
        _catalog.Album = Album() with { Album = Summary() with { SeriesId = null, SeriesTitle = null } };

        var page = Open();

        Assert.Equal(2, page.FindAll("nav.breadcrumb li").Count);
        Assert.Empty(page.FindAll(".album-identity a.album-series"));
    }

    [Fact]
    public void Foot_DatesTheAlbum_InTheTimeZoneOfTheBrowser()
    {
        _catalog.Album = Album();

        var page = Open();

        Assert.Equal("Album créé le 02/02/2026, modifié le 10/10/2026.", page.Find(".album-dates").TextContent.Trim());
    }

    [Fact]
    public void Display_IsRecordedAsTheRecordOfTheAlbum_WithoutAction()
    {
        _catalog.Album = Album();

        var page = Open();

        var screen = page.FindComponent<AudienceScreen>().Instance;
        Assert.Equal(AudienceFunctions.AlbumRecord, screen.Function);
        Assert.Equal(new PresentedRecord(RecordType.Album, AlbumId), screen.Record);
        Assert.Equal(["album"], _audience.Functions);
    }

    [Fact]
    public void UnknownAlbum_PresentsTheFunctionalError()
    {
        _catalog.AlbumStatus = HttpStatusCode.NotFound;

        var page = Open();

        Assert.Equal("functional", page.Find("[role='alert']").GetAttribute("data-error-category"));
    }

    private IRenderedComponent<AlbumRecord> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.Album(AlbumId));
        return Render<AlbumRecord>(parameters => parameters.Add(p => p.Id, AlbumId).AddCascadingValue(_request.Context));
    }

    private static IElement BlockOf(IRenderedComponent<AlbumRecord> page, string title) =>
        page.FindAll("section.block").Single(block => block.QuerySelector(".block-title")!.TextContent.Trim() == title);

    private static AlbumSummary Summary() =>
        new(AlbumId, "Lointaines îles du vent", SeriesId, "Corto Maltese", AlbumType.Regular, false, 5, null, null, true);

    private static EditionDetail Owned(Guid id, int year) => new(
        new EditionSummary(id, Guid.NewGuid(), "Casterman", null, null, year, null),
        PublisherWebsite: "https://www.casterman.com", IsbnChecksumValid: null, Binding: BindingType.Hardcover, Orientation: null,
        ReadingDirection: null, Format: EditionFormat.Standard, PageCount: null, Category: null, IsDedicated: false, IsColor: true,
        Condition: EditionCondition.Excellent, AcquisitionMode: AcquisitionMode.Purchase, IsSecondHand: false,
        AcquisitionDate: new DateOnly(2006, 9, 9), AcquisitionAmount: 16.95m, AcquisitionCurrency: "EUR", IsFree: false,
        InitialValueAmount: null, InitialValueCurrency: null, PersonalReference: null, PersonalNotes: null, IsInCollection: true,
        EnteredCollectionAt: new DateTimeOffset(2026, 2, 2, 23, 0, 0, TimeSpan.Zero), PurchaseIntent: null, Visuals: [], CreatedAt: Created,
        ModifiedAt: Modified);

    private static EditionDetail Intended() => Owned(IntendedEditionId, 2023) with
    {
        AcquisitionMode = null,
        AcquisitionDate = null,
        AcquisitionAmount = null,
        AcquisitionCurrency = null,
        IsInCollection = false,
        EnteredCollectionAt = null,
        PurchaseIntent = new PurchaseIntentItem(Guid.NewGuid(), Modified, Modified),
    };

    private static AlbumDetail Album()
    {
        var pratt = new AuthorListItem(Guid.NewGuid(), "Pratt", "Hugo", null);
        var ferrand = new AuthorListItem(Guid.NewGuid(), "Ferrand", "Léa", null);
        var root = new UniverseListItem(Guid.NewGuid(), "Univers Hugo Pratt", null, null);
        var corto = new UniverseListItem(Guid.NewGuid(), "Corto Maltese", root.Id, root.Name);
        return new AlbumDetail(
            Summary(), 1970, 11, "Venezuela, vers 1913.", "Réunit les trois premiers récits.", null,
            [new GenreListItem(Guid.NewGuid(), "Aventure")],
            [new UniverseInHierarchy(new UniverseListItem(Guid.NewGuid(), "Les Antilles", corto.Id, corto.Name), [root, corto])],
            [
                new ContributionItem(Guid.NewGuid(), pratt, ContributionRole.Scenarist, Created, Created),
                new ContributionItem(Guid.NewGuid(), ferrand, ContributionRole.Scenarist, Created, Created),
                new ContributionItem(Guid.NewGuid(), pratt, ContributionRole.Illustrator, Created, Created),
            ],
            // Two editions in the collection, and one targeted by an intent, which is no edition of the collection.
            [Owned(FirstEditionId, 2001), Owned(SecondEditionId, 2010), Intended()],
            null,
            Created,
            Modified);
    }

    /// <summary>The catalog endpoints the record reads, answering as a test chooses; the audience ones, relayed.</summary>
    private sealed class FakeCatalogApi(FakeAudienceApi audience) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _audience = new(audience);

        public ApplicationSettings Settings { get; set; } = new(AlbumLabelFormat.TitleSeriesVolume, Contracts.Enums.DisplayCulture.FrenchFrance);

        public AlbumDetail? Album { get; set; }

        public HttpStatusCode AlbumStatus { get; set; } = HttpStatusCode.OK;

        public List<string> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            if (path.StartsWith("/audience/", StringComparison.Ordinal))
                return _audience.SendAsync(request, cancellationToken);

            Requested.Add(path);
            if (path == "/catalog/settings")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Settings) });
            if (AlbumStatus != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(AlbumStatus)
                {
                    Content = JsonContent.Create(new { type = ProblemTypes.Functional, status = (int)AlbumStatus },
                        mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Album) });
        }
    }
}
