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
/// The record of an author in the consultation (visuel/fiche-auteur/contenu.md): its identity, its biography, its whole
/// bibliography in a single list, and its dates, told apart by role, link and text, never by appearance.
/// </summary>
public sealed class AuthorRecordTests : BunitContext, IDisposable
{
    private static readonly Guid AuthorId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000c1");
    private static readonly Guid SangreId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000d1");
    private static readonly Guid YthaqId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000d2");
    private static readonly Guid TroyId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000d3");
    private static readonly Guid SketchbookId = Guid.Parse("0197a0b2-0000-7000-8000-0000000000a9");
    private static readonly DateTimeOffset Created = new(2026, 2, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Modified = new(2026, 10, 9, 23, 30, 0, TimeSpan.Zero);

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly FakeAudienceApi _audience = new();
    private readonly FakeCatalogApi _catalog;
    // A request of a visit of the browser, which the audience measurement records.
    private readonly TestRequest _request = new(
        new Dictionary<string, string> { [BrowserVisit.CookieName] = new BrowserVisit(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3)).Format() });

    public AuthorRecordTests()
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
    public void Record_ReadsTheAuthorOfItsAddress()
    {
        _catalog.Author = Author();

        Open();

        Assert.Contains($"/catalog/authors/{AuthorId}", _catalog.Requested);
    }

    [Fact]
    public void Breadcrumb_LeadsToTheAuthors_ThenNamesTheAuthor()
    {
        _catalog.Author = Author();

        var page = Open();

        Assert.Equal(["Auteurs", "Adrien Floch"], page.FindAll("nav.breadcrumb li").Select(l => l.TextContent.Trim()));
        Assert.Equal([AppPaths.Authors], page.FindAll("nav.breadcrumb a").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void Identity_IsTheMainIdentifier_WithItsMonogram_WithoutANationalityNotSet()
    {
        _catalog.Author = Author();

        var page = Open();

        Assert.Equal("Adrien Floch", page.Find("h1").TextContent.Trim());
        Assert.Equal("AF", page.Find(".author-head .monogram").TextContent.Trim());
        // Known by its own name, no other name follows it.
        Assert.Empty(page.FindAll(".author-real-name"));
        // Data not set is not shown.
        Assert.Empty(page.FindAll(".author-nationality"));
    }

    [Fact]
    public void Identity_UnderAPseudonym_IsFollowedByTheFirstAndLastName_AndTellsTheNationality()
    {
        _catalog.Author = Author() with { LastName = "Giraud", FirstName = "Jean", Pseudonym = "Moebius", Nationality = "Française" };

        var page = Open();

        Assert.Equal("Moebius", page.Find("h1").TextContent.Trim());
        Assert.Equal("Jean Giraud", page.Find(".author-real-name").TextContent.Trim());
        Assert.Equal("Française", page.Find(".author-nationality dd").TextContent.Trim());
    }

    [Fact]
    public void Identity_UnderAPseudonymAlone_IsFollowedByNothing()
    {
        _catalog.Author = Author() with { LastName = null, FirstName = null, Pseudonym = "Kador" };

        var page = Open();

        Assert.Equal("Kador", page.Find("h1").TextContent.Trim());
        Assert.Empty(page.FindAll(".author-real-name"));
    }

    [Fact]
    public void Facts_TellTheAlbumsCredited_ThenTheirNumberByRole()
    {
        _catalog.Author = Author();

        var page = Open();

        // 1 album without series, 6 of « Sangre », 7 of « Les Naufragés d'Ythaq »; « Les Terres de Troy », credited on the
        // series only, has no album credited.
        Assert.Equal(["Crédité sur 14 albums", "Scénario · 1", "Dessin · 14", "Couleurs · 3"],
            page.FindAll(".author-facts li").Select(f => f.TextContent.Trim()));
    }

    [Fact]
    public void Facts_OfAnAuthorCreditedOnOneAlbum_AreInTheSingular()
    {
        _catalog.Author = Author() with { Bibliography = [new BibliographyEntry(null, Sketchbook())] };

        var page = Open();

        Assert.Equal(["Crédité sur 1 album", "Scénario · 1", "Dessin · 1"], page.FindAll(".author-facts li").Select(f => f.TextContent.Trim()));
    }

    [Fact]
    public void Biography_IsPresented_AndItsBlockLeftOutWithout()
    {
        _catalog.Author = Author() with { Biography = "Né à Brest en 1981." };
        Assert.Equal("Né à Brest en 1981.", BlockOf(Open(), "Qui est-ce ?").QuerySelector(".author-biography")!.TextContent.Trim());

        _catalog.Author = Author();
        // Data not set is not shown: no block « Qui est-ce ? » either.
        Assert.DoesNotContain(Open().FindAll("section.block .block-title"), title => title.TextContent.Trim() == "Qui est-ce ?");
    }

    [Fact]
    public void Bibliography_IsASingleList_OfTheSeriesAndAlbumsWithoutSeries_InTheOrderOfTheApi()
    {
        _catalog.Author = Author();

        var page = Open();

        var entries = Entries(page);
        Assert.Equal(["Carnet de croquis", "Les Naufragés d'Ythaq", "Sangre", "Les Terres de Troy"],
            entries.Select(e => e.QuerySelector(".entry-row .entry-title")!.TextContent.Trim()));
        Assert.Equal(["album", "series", "series", "series"], entries.Select(e => e.GetAttribute("data-kind")));
        Assert.Equal("3 séries et 1 album sans série, rangés par ordre alphabétique.",
            page.Find(".bibliography-summary").TextContent.Trim());
    }

    [Fact]
    public void AlbumWithoutSeries_HasItsCover_ItsTitle_ItsRoles_ItsRating_AndItsMembership()
    {
        _catalog.Author = Author();

        var page = Open();

        var album = Entries(page)[0];
        Assert.NotNull(album.QuerySelector(".entry-row .cover-slot"));
        Assert.Empty(album.QuerySelectorAll(".cover-pile"));
        Assert.Equal(AppPaths.Album(SketchbookId), album.QuerySelector("a.entry-title")!.GetAttribute("href"));
        // Without series, nothing places it: no mention « Album sans série », which says nothing useful.
        Assert.Null(album.QuerySelector(".entry-meta"));
        Assert.Equal(["Scénario", "Dessin"], album.QuerySelectorAll(".role").Select(r => r.TextContent.Trim()));
        Assert.Equal(["scenarist", "illustrator"], album.QuerySelectorAll(".role").Select(r => r.GetAttribute("data-role")));
        Assert.Equal("Appréciation : Bien, 4 sur 5", album.QuerySelector(".rating-stars[role='img']")!.GetAttribute("aria-label"));
        Assert.Equal("Hors collection", album.QuerySelector(".tag")!.TextContent.Trim());
        // An album has nothing to unfold.
        Assert.Empty(album.QuerySelectorAll("details"));
    }

    [Fact]
    public void Album_TellsItsDateOfFirstPublication_OnlyWhenSet()
    {
        _catalog.Author = Author() with
        {
            Bibliography =
            [
                new BibliographyEntry(null, Sketchbook() with { FirstPublicationYear = 2019, FirstPublicationMonth = 3 }),
                new BibliographyEntry(Series(SangreId, "Sangre", 6, [],
                    [SangreAlbum(1), SangreAlbum(2) with { FirstPublicationYear = 2004 }]), null),
            ],
        };

        var page = Open();

        Assert.Equal("mars 2019", Entries(page)[0].QuerySelector(".entry-meta")!.TextContent.Trim());
        var albums = Entries(page)[1].QuerySelectorAll("details.entry-albums ol > li");
        Assert.Null(albums[0].QuerySelector(".entry-meta"));
        Assert.Equal("T. 2 · 2004", albums[1].QuerySelector(".entry-meta")!.TextContent.Trim());
    }

    [Fact]
    public void Series_HasItsPileOfCovers_LeadsToItsRecord_AndTellsItsAlbumsCredited_ThoseOfTheCollection_AndItsOwnRoles()
    {
        _catalog.Author = Author();

        var page = Open();

        var sangre = Entries(page)[2];
        Assert.Equal(3, sangre.QuerySelectorAll(".entry-row .cover-pile .cover-slot").Length);
        Assert.Equal(AppPaths.SeriesRecord(SangreId), sangre.QuerySelector(".entry-row a.entry-title")!.GetAttribute("href"));
        Assert.Equal("Crédité sur 6 albums sur 6 · 5 dans la collection", sangre.QuerySelector(".entry-row .entry-meta")!.TextContent.Trim());
        var seriesRoles = sangre.QuerySelector(".entry-row .entry-series-roles")!;
        Assert.StartsWith("Sur la série :", seriesRoles.TextContent.Trim(), StringComparison.Ordinal);
        Assert.Equal(["Dessin"], seriesRoles.QuerySelectorAll(".role").Select(r => r.TextContent.Trim()));
    }

    [Fact]
    public void Series_CreditedOnAlbumsOnly_HasNoRoleOfItsOwn_AndNoneInTheCollection_SaysSo()
    {
        // The sixth volume of « Sangre » is not in the collection.
        _catalog.Author = Author() with { Bibliography = [new BibliographyEntry(Series(SangreId, "Sangre", 3, [], [SangreAlbum(6)]), null)] };

        var page = Open();

        var sangre = Entries(page)[0];
        Assert.Equal("Crédité sur 1 album sur 3 · aucun dans la collection", sangre.QuerySelector(".entry-row .entry-meta")!.TextContent.Trim());
        Assert.Empty(sangre.QuerySelectorAll(".entry-row .entry-series-roles"));
    }

    [Fact]
    public void Series_CreditedOnItselfOnly_HasItsPile_WithoutAnythingToUnfold()
    {
        _catalog.Author = Author();

        var page = Open();

        var troy = Entries(page)[3];
        Assert.Equal(3, troy.QuerySelectorAll(".cover-pile .cover-slot").Length);
        Assert.Equal("Série · crédité sur la série seulement, sur aucun de ses 4 albums", troy.QuerySelector(".entry-meta")!.TextContent.Trim());
        Assert.Empty(troy.QuerySelectorAll("details"));
    }

    [Fact]
    public void Series_UnfoldsOnItsAlbumsCredited_InTheOrderOfTheSeries_FramedUnderIt()
    {
        _catalog.Author = Author();

        var page = Open();

        var albums = Entries(page)[2].QuerySelectorAll("details.entry-albums ol > li");
        Assert.Equal(["Tome 1", "Le Sang du dragon", "Tome 3", "Tome 4", "Tome 5", "Tome 6"],
            albums.Select(a => a.QuerySelector(".entry-title")!.TextContent.Trim()));
        // Under its series, an album does not repeat it; its volume, only when its title does not already say it.
        Assert.Equal("T. 2", albums[1].QuerySelector(".entry-meta")!.TextContent.Trim());
        Assert.Null(albums[0].QuerySelector(".entry-meta"));
        Assert.Equal(AppPaths.Album(SangreAlbumId(2)), albums[1].QuerySelector("a.entry-title")!.GetAttribute("href"));
        Assert.Equal(["Dessin", "Couleurs"], albums[0].QuerySelectorAll(".role").Select(r => r.TextContent.Trim()));
        Assert.Equal("Dans la collection", albums[0].QuerySelector(".tag")!.TextContent.Trim());
        Assert.Empty(albums[1].QuerySelectorAll(".rating-stars"));
        // The disclosure is named after its series, for assistive technologies.
        Assert.Equal("Albums de la série Sangre", Entries(page)[2].QuerySelector("details.entry-albums > summary")!.TextContent.Trim());
    }

    [Fact]
    public void Series_OfMoreThanSixAlbumsCredited_IsFoldedOnArrival_TheOthersUnfolded()
    {
        _catalog.Author = Author();

        var page = Open();

        var entries = Entries(page);
        Assert.False(entries[1].QuerySelector("details.entry-albums")!.HasAttribute("open"));
        Assert.True(entries[2].QuerySelector("details.entry-albums")!.HasAttribute("open"));
    }

    [Fact]
    public void UnfoldAndFoldAll_ActOnTheWholeList()
    {
        _catalog.Author = Author();

        var page = Open();

        var folds = page.Find("app-bibliography-folds");
        Assert.Equal(["Tout déplier", "Tout replier"], folds.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
        Assert.All(folds.QuerySelectorAll("button"), b => Assert.Equal("button", b.GetAttribute("type")));
        Assert.Equal(["unfold", "fold"], folds.QuerySelectorAll("button").Select(b => b.GetAttribute("data-fold-all")));
        Assert.NotNull(folds.QuerySelector("ul.bibliography-entries"));
    }

    [Fact]
    public void Bibliography_WithoutAnySeriesToUnfold_HasNoUnfoldAll()
    {
        _catalog.Author = Author() with { Bibliography = [new BibliographyEntry(null, Sketchbook())] };

        var page = Open();

        Assert.Empty(page.FindAll("[data-fold-all]"));
        Assert.Equal("Aucune série et 1 album sans série, rangés par ordre alphabétique.",
            page.Find(".bibliography-summary").TextContent.Trim());
    }

    [Fact]
    public void AuthorWithoutAnyCredit_SaysSoInItsFacts_WithoutABibliography()
    {
        _catalog.Author = Author() with { Bibliography = [] };

        var page = Open();

        // A number of albums of zero is a value, said in the facts; an empty bibliography is not shown: no block « Sur quels albums ? ».
        Assert.Equal(["Crédité sur aucun album"], page.FindAll(".author-facts li").Select(f => f.TextContent.Trim()));
        Assert.Empty(page.FindAll("[data-part='bibliography']"));
        Assert.DoesNotContain(page.FindAll("section.block .block-title"), title => title.TextContent.Trim() == "Sur quels albums ?");
    }

    [Fact]
    public void Foot_DatesTheAuthor_InTheTimeZoneOfTheBrowser()
    {
        _catalog.Author = Author();

        var page = Open();

        Assert.Equal("Auteur créé le 02/02/2026, modifié le 10/10/2026.", page.Find(".author-dates").TextContent.Trim());
    }

    [Fact]
    public void Display_IsRecordedAsTheRecordOfTheAuthor()
    {
        _catalog.Author = Author();

        var page = Open();

        var screen = page.FindComponent<AudienceScreen>().Instance;
        Assert.Equal(AudienceFunctions.AuthorRecord, screen.Function);
        Assert.Equal(new PresentedRecord(RecordType.Author, AuthorId), screen.Record);
        Assert.Equal(["author"], _audience.Functions);
    }

    [Fact]
    public void Author_ThatNoLongerExists_IsAnError_OfItsCategory()
    {
        _catalog.AuthorStatus = HttpStatusCode.NotFound;

        var page = Open();

        Assert.Equal("functional", page.Find("[role='alert']").GetAttribute("data-error-category"));
        Assert.Empty(page.FindAll(".bibliography"));
    }

    private IRenderedComponent<AuthorRecord> Open()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.AuthorRecord(AuthorId));
        return Render<AuthorRecord>(parameters => parameters.Add(p => p.Id, AuthorId).AddCascadingValue(_request.Context));
    }

    private static IElement BlockOf(IRenderedComponent<AuthorRecord> page, string title) =>
        page.FindAll("section.block").Single(block => block.QuerySelector(".block-title")!.TextContent.Trim() == title);

    private static IReadOnlyList<IElement> Entries(IRenderedComponent<AuthorRecord> page) => page.FindAll("ul.bibliography-entries > li");

    private static Guid SangreAlbumId(int volume) => Guid.Parse($"0197a0b2-0000-7000-8000-0000000001{volume:00}");

    private static AlbumSummary Summary(Guid seriesId, string seriesTitle, int volume, string? title = null) =>
        new(seriesId == SangreId ? SangreAlbumId(volume) : Guid.NewGuid(), title, seriesId, seriesTitle, AlbumType.Regular, false, volume, null, null, true);

    private static AlbumCard Card(Guid seriesId, string seriesTitle, int volume, string? title = null) =>
        new(Summary(seriesId, seriesTitle, volume, title), null, new AlbumCover(Guid.NewGuid(), new CoverPalette("#2F5BA8", "#14295A", "#D8342B")), false);

    // Volumes of « Sangre »: the second has its own title, the sixth is not in the collection.
    private static AlbumBibliographyItem SangreAlbum(int volume)
    {
        var card = Card(SangreId, "Sangre", volume, volume == 2 ? "Le Sang du dragon" : null);
        return new AlbumBibliographyItem(
            card with { Album = card.Album with { IsInCollection = volume != 6 } },
            volume == 1 ? [ContributionRole.Illustrator, ContributionRole.Colorist] : [ContributionRole.Illustrator]);
    }

    private static AlbumBibliographyItem Sketchbook() => new(
        new AlbumCard(new AlbumSummary(SketchbookId, "Carnet de croquis", null, null, AlbumType.Regular, false, null, null, null, false),
            AlbumRating.Good, null, false),
        [ContributionRole.Scenarist, ContributionRole.Illustrator]);

    private static SeriesBibliographyItem Series(Guid id, string title, int albumCount, IReadOnlyList<ContributionRole> roles, IReadOnlyList<AlbumBibliographyItem> albums) =>
        new(new SeriesListItem(id, title), new SeriesOverview(albumCount, [null]), roles, albums);

    private static AuthorDetail Author() => new(
        AuthorId, "Floch", "Adrien", null, null, null,
        [
            new BibliographyEntry(null, Sketchbook()),
            // Seven albums credited: folded on arrival.
            new BibliographyEntry(Series(YthaqId, "Les Naufragés d'Ythaq", 18, [],
                [.. Enumerable.Range(1, 7).Select(volume => new AlbumBibliographyItem(Card(YthaqId, "Les Naufragés d'Ythaq", volume),
                    volume <= 2 ? [ContributionRole.Illustrator, ContributionRole.Colorist] : [ContributionRole.Illustrator]))]), null),
            new BibliographyEntry(Series(SangreId, "Sangre", 6, [ContributionRole.Illustrator], [.. Enumerable.Range(1, 6).Select(SangreAlbum)]), null),
            new BibliographyEntry(Series(TroyId, "Les Terres de Troy", 4, [ContributionRole.Illustrator], []), null),
        ],
        Created,
        Modified);

    /// <summary>The catalog endpoints the record reads, answering as a test chooses; the audience ones, relayed.</summary>
    private sealed class FakeCatalogApi(FakeAudienceApi audience) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _audience = new(audience);

        public AuthorDetail? Author { get; set; }

        public HttpStatusCode AuthorStatus { get; set; } = HttpStatusCode.OK;

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
            if (AuthorStatus != HttpStatusCode.OK)
                return Task.FromResult(new HttpResponseMessage(AuthorStatus)
                {
                    Content = JsonContent.Create(new { type = ProblemTypes.Functional, status = (int)AuthorStatus },
                        mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
                });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Author) });
        }
    }
}
