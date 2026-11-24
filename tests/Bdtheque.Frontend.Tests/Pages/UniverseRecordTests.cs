using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AngleSharp.Dom;
using Bdtheque.Contracts.Admin;
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
/// The record of a universe in the consultation (visuel/fiche-univers/contenu.md): its parents, its identity, its description,
/// the tree of its sub-universes, its series and albums presented as the bibliography of an author without roles, and its dates, told apart
/// by role, link and text, never by appearance.
/// </summary>
public sealed class UniverseRecordTests : BunitContext, IDisposable
{
    private static readonly Guid UniverseId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e1");
    private static readonly Guid ParentId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e0");
    private static readonly Guid ChildId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e2");
    private static readonly Guid GrandChildId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e3");
    private static readonly Guid SecondChildId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000e4");
    private static readonly Guid PhareId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000d1");
    private static readonly Guid PasseursId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000d2");
    private static readonly Guid LivreId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000a1");
    private static readonly DateTimeOffset Created = new(2026, 2, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Modified = new(2026, 10, 9, 23, 30, 0, TimeSpan.Zero);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeCatalogApi _catalog;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() });

    public UniverseRecordTests()
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
    public void Record_ReadsTheUniverseOfItsAddress()
    {
        _catalog.Universe = Universe();

        Open();

        Assert.Contains($"/catalog/universes/{UniverseId}", _catalog.Requested);
    }

    [Fact]
    public void Breadcrumb_StartsWithUniverses_LeadingNowhere_ThenLeadsToItsParents_ThenNamesTheUniverse()
    {
        _catalog.Universe = Universe();

        var page = Open();

        Assert.Equal(["Univers", "Royaumes d'Ombrelune", "Archipel des Brumes"], page.FindAll("nav.breadcrumb li").Select(l => l.TextContent.Trim()));
        Assert.Equal([AppPaths.UniverseRecord(ParentId)], page.FindAll("nav.breadcrumb a").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void Head_IsUnderTheMentionOfItsKind_OverItsVisual_TheIdentityBesideIt()
    {
        _catalog.Universe = Universe();

        var page = Open();

        Assert.Equal(["record-visual", "universe-identity"], page.Find(".universe-head").Children.Select(child => child.ClassName));
        Assert.Equal("Univers", page.Find(".universe-head > .record-visual .record-kind").TextContent);
        Assert.Equal("Archipel des Brumes", page.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void Identity_CountsItsSeriesAndAlbums_ThenItsSubUniversesAtEveryLevel()
    {
        _catalog.Universe = Universe();

        var page = Open();

        // Two series, and their three albums with the album without series; three sub-universes, on two levels.
        Assert.Equal(["2 séries", "4 albums", "3 sous-univers"], page.FindAll(".universe-facts li").Select(f => f.TextContent.Trim()));
    }

    [Fact]
    public void SubUniverses_AreTheirOwnBlock_BetweenTheDescriptionAndTheWorks()
    {
        _catalog.Universe = Universe();

        var page = Open();

        Assert.Equal(["description", "sub-universes", "bibliography"], page.FindAll("[data-part]").Select(b => b.GetAttribute("data-part")));
        var block = page.Find("[data-part='sub-universes']");
        Assert.Equal("Quels sous-univers ?", block.QuerySelector(".block-title")!.TextContent.Trim());
        Assert.Equal("Arborescence", block.QuerySelector(".block-kicker")!.TextContent.Trim());
        Assert.Equal(
            "3 sous-univers, sur 2 niveaux, chacun sous son parent, frères par ordre alphabétique.",
            block.QuerySelector(".sub-universes-summary")!.TextContent.Trim());
    }

    [Fact]
    public void SubUniverses_FormTheirTree_EachUnderItsParent_LeadingToItsRecord_WithWhatIsAttachedToIt()
    {
        _catalog.Universe = Universe();

        var page = Open();

        // Their tree, as in the list of the universes of the administration (visuel/fiche-univers/contenu.md).
        var tree = page.Find(".sub-universes");
        Assert.Equal(
            [("Phare de Kerdran", AppPaths.UniverseRecord(ChildId)), ("Tour de la Vigie", AppPaths.UniverseRecord(SecondChildId))],
            tree.QuerySelectorAll(":scope > li > .sub-universe > a").Select(a => (a.TextContent.Trim(), a.GetAttribute("href"))));
        Assert.Equal(
            [("Ordre des Gardiens", AppPaths.UniverseRecord(GrandChildId))],
            tree.QuerySelectorAll(":scope > li > ul > li > .sub-universe > a").Select(a => (a.TextContent.Trim(), a.GetAttribute("href"))));
        // What is attached to it, a number at zero left out; nothing at all said as in the facts.
        Assert.Equal(
            ["2 séries · 7 albums", "1 album", "Aucun album rattaché"],
            tree.QuerySelectorAll(".sub-universe-works").Select(w => w.TextContent.Trim()));
    }

    [Fact]
    public void UniverseWithNothingAttached_SaysSoInItsFacts_WithoutSubUniversesNorDescriptionNorBibliography()
    {
        _catalog.Universe = Universe() with { Description = null, SubUniverses = [], Works = [] };

        var page = Open();

        Assert.Equal(["Aucun album rattaché"], page.FindAll(".universe-facts li").Select(f => f.TextContent.Trim()));
        // Data not set is not shown: no sub-universe, neither fact nor block.
        Assert.Empty(page.FindAll("[data-part='sub-universes']"));
        Assert.Empty(page.FindAll("[data-part='description']"));
        Assert.Empty(page.FindAll("[data-part='bibliography']"));
    }

    [Fact]
    public void Description_IsItsOwnBlock()
    {
        _catalog.Universe = Universe();

        var page = Open();

        var block = page.Find("[data-part='description']");
        Assert.Equal("De quoi s'agit-il ?", block.QuerySelector(".block-title")!.TextContent.Trim());
        Assert.Equal("Un chapelet d'îles battues par les vents.", block.QuerySelector(".universe-description")!.TextContent.Trim());
    }

    [Fact]
    public void Works_ArePresentedAsABibliography_WithoutRoles()
    {
        _catalog.Universe = Universe();

        var page = Open();

        var block = page.Find("[data-part='bibliography']");
        Assert.Equal("Quelles séries, quels albums ?", block.QuerySelector(".block-title")!.TextContent.Trim());
        Assert.Null(page.Find(".bibliography").GetAttribute("data-roles"));
        Assert.Empty(page.FindAll(".entry-roles"));
        // Every album line has its two columns, the rating empty without one (#636).
        Assert.All(page.FindAll("li.entry-row, .bibliography-entry[data-kind='album'] > .entry-row"), line => Assert.Equal(
            ["entry-rating", "entry-state"],
            line.Children.Select(c => c.ClassName ?? "").Where(c => c.StartsWith("entry-", StringComparison.Ordinal) && c != "entry-text")));
    }

    [Fact]
    public void Series_AttachedItself_TellsItsAlbumsInTheCollectionOutOfAllOfThem()
    {
        _catalog.Universe = Universe();

        var page = Open();

        var phare = Entries(page)[1];
        Assert.Equal(AppPaths.SeriesRecord(PhareId), phare.QuerySelector(".entry-row a.entry-title")!.GetAttribute("href"));
        Assert.Equal("1 album sur 2 dans la collection", phare.QuerySelector(".entry-row .entry-meta")!.TextContent.Trim());
    }

    [Fact]
    public void Series_AttachedThroughSomeOfItsAlbums_TellsThoseOutOfAll_ThenThoseOfTheCollection()
    {
        _catalog.Universe = Universe();

        var page = Open();

        Assert.Equal("1 album rattaché sur 5 · aucun dans la collection", Entries(page)[2].QuerySelector(".entry-row .entry-meta")!.TextContent.Trim());
    }

    [Fact]
    public void Series_AttachedItselfWithoutAlbum_HasNothingToSayUnderItsTitle()
    {
        _catalog.Universe = Universe() with { Works = [new BibliographyEntry(Series(PhareId, "Les Gardiens du phare", 0, true, []), null)] };

        var page = Open();

        Assert.Empty(Entries(page)[0].QuerySelectorAll(".entry-meta"));
    }

    [Fact]
    public void Foot_DatesTheUniverse_InTheTimeZoneOfTheBrowser()
    {
        _catalog.Universe = Universe();

        var page = Open();

        Assert.Equal("Univers créé le 02/02/2026, modifié le 10/10/2026.", page.Find(".universe-dates").TextContent.Trim());
    }

    [Fact]
    public void Display_IsRecordedAsTheRecordOfTheUniverse()
    {
        _catalog.Universe = Universe();

        var page = Open();

        var screen = page.FindComponent<AudienceScreen>().Instance;
        Assert.Equal(AudienceFunctions.UniverseRecord, screen.Function);
        Assert.Equal(new PresentedRecord(RecordType.Universe, UniverseId), screen.Record);
        Assert.Equal(["universe"], _audience.Functions);
    }

    [Fact]
    public void Universe_ThatNoLongerExists_IsAnError_OfItsCategory()
    {
        _catalog.UniverseStatus = HttpStatusCode.NotFound;

        var page = Open();

        Assert.Equal("functional", page.Find("[role='alert']").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".bibliography"));
    }

    private IRenderedComponent<UniverseRecord> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.UniverseRecord(UniverseId));
        return Render<UniverseRecord>(parameters => parameters.Add(p => p.Id, UniverseId).AddCascadingValue(_request.Context));
    }

    private static IReadOnlyList<IElement> Entries(IRenderedComponent<UniverseRecord> page) => page.FindAll("ul.bibliography-entries > li");

    private static AlbumBibliographyItem Album(Guid id, Guid? seriesId, string? seriesTitle, int? volume, string? title, bool owned, AlbumRating? rating = null) =>
        new(new AlbumCard(
            new AlbumSummary(id, title, seriesId, seriesTitle, AlbumType.Regular, false, volume, null, null, owned), rating, null,
            owned ? AlbumCollectionState.InCollection : AlbumCollectionState.OutsideCollection), []);

    private static SeriesBibliographyItem Series(Guid id, string title, int albumCount, bool onSeries, IReadOnlyList<AlbumBibliographyItem> albums) =>
        new(new SeriesListItem(id, title), new SeriesOverview(albumCount, [null]), onSeries, [], albums);

    private static UniverseDetail Universe() => new(
        UniverseId, "Archipel des Brumes", "Un chapelet d'îles battues par les vents.",
        [new UniverseListItem(ParentId, "Royaumes d'Ombrelune", null, null)],
        [
            new SubUniverse(ChildId, "Phare de Kerdran", UniverseId, 2, 7),
            new SubUniverse(GrandChildId, "Ordre des Gardiens", ChildId, 0, 1),
            new SubUniverse(SecondChildId, "Tour de la Vigie", UniverseId, 0, 0),
        ],
        [
            new BibliographyEntry(null, Album(LivreId, null, null, null, "Le Grand Livre d'Ombrelune", true, AlbumRating.Good)),
            new BibliographyEntry(Series(PhareId, "Les Gardiens du phare", 2, true,
                [Album(Guid.NewGuid(), PhareId, "Les Gardiens du phare", 1, "La Lanterne noire", true), Album(Guid.NewGuid(), PhareId, "Les Gardiens du phare", 2, null, false)]), null),
            new BibliographyEntry(Series(PasseursId, "Les Passeurs de brume", 5, false,
                [Album(Guid.NewGuid(), PasseursId, "Les Passeurs de brume", 4, null, false)]), null),
        ],
        Created,
        Modified);

    /// <summary>The catalog endpoints the record reads, answering as a test chooses; the audience ones, relayed.</summary>
    private sealed class FakeCatalogApi(FakeAudienceApi audience) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _audience = new(audience);

        public UniverseDetail? Universe { get; set; }

        public HttpStatusCode UniverseStatus { get; set; } = HttpStatusCode.OK;

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
            if (UniverseStatus != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(UniverseStatus)
                {
                    Content = JsonContent.Create(new { type = ProblemTypes.Functional, status = (int)UniverseStatus },
                        mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Universe) });
        }
    }
}
