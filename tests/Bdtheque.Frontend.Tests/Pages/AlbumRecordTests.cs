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
/// The record of an album in the consultation (visuel/fiche-album/contenu.md): its visuals, its identity, its contributions by
/// role, its genres and universes, its summary, its editions in the collection and the detail of the one chosen, and its
/// dates, told apart by role, link and text, never by appearance.
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
        Services.AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>();
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
        Assert.Empty(page.FindAll(".collection-status"));
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
    public void Contributions_ListEachRoleThatHasAny_LeadingToTheAuthors_WithoutCount()
    {
        _catalog.Album = Album();

        var page = Open();

        // Colouring has no author: its line does not appear; nor does a count of the authors credited.
        var contributions = page.Find(".contributions");
        Assert.Equal(["Scénario", "Dessin"], contributions.QuerySelectorAll("dt .contributions-role").Select(r => r.TextContent.Trim()));
        var scenario = contributions.QuerySelectorAll("dd")[0];
        Assert.Equal(["Hugo Pratt", "Léa Ferrand"], scenario.QuerySelectorAll("a").Select(a => a.TextContent.Trim()));
        Assert.Equal(
            _catalog.Album.Contributions.Where(c => c.Role == ContributionRole.Scenarist).Select(c => AppPaths.AuthorRecord(c.Author.Id)),
            scenario.QuerySelectorAll("a").Select(a => a.GetAttribute("href")));
        Assert.DoesNotContain("crédité", contributions.TextContent);
    }

    [Fact]
    public void Contributions_OfAnAlbumWithoutAny_SaySo()
    {
        _catalog.Album = Album() with { Contributions = [] };

        var page = Open();

        Assert.Equal("Aucun auteur crédité", page.Find(".contributions").TextContent.Trim());
        Assert.Empty(page.FindAll(".contributions dt"));
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
        var row = page.Find(".contributions dd").QuerySelector("app-fold-row")!;
        Assert.Equal(authors.Select(a => a.LastName), row.QuerySelectorAll(".contributor").Select(a => a.TextContent.Trim()));
        var toggle = row.QuerySelector("details summary")!;
        Assert.Equal(("et {0} autre", "et {0} autres"),
            (toggle.QuerySelector(".contributions-more")!.GetAttribute("data-one"), toggle.QuerySelector(".contributions-more")!.GetAttribute("data-other")));
        Assert.Equal("Réduire", toggle.QuerySelector(".contributions-less")!.TextContent.Trim());
        Assert.Equal("(9)", page.Find(".contributions dt").QuerySelector(".contributions-count")!.TextContent.Trim());
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
        Assert.Equal(("intent", "Intention d'achat"), (page.Find(".collection-status").GetAttribute("data-status"), page.Find(".collection-status strong").TextContent.Trim()));
        // It has no edition: no edition nor visual, nor any mention in their place, and the generic visual as its cover.
        Assert.Empty(page.FindAll(".edition, .edition-visuals, .editions-none"));
        Assert.NotNull(page.Find(".album-cover svg.cover-generic"));
    }

    [Fact]
    public void AlbumWhoseOnlyEditionIsTargetedByAnIntent_IsMarkedAsSuch()
    {
        _catalog.Album = Album() with { Album = Summary() with { IsInCollection = false }, Editions = [Intended()] };

        var page = Open();

        Assert.Equal("Intention d'achat", page.FindAll(".album-facts li")[^1].TextContent.Trim());
        Assert.Equal(("intent", "Intention d'achat"), (page.Find(".collection-status").GetAttribute("data-status"), page.Find(".collection-status strong").TextContent.Trim()));
        // The edition targeted is an intent, never an edition of the collection, which has none, without any mention.
        Assert.Empty(BlockOf(page, "Dans quelles éditions ?").QuerySelectorAll(".edition, .editions-none"));
        Assert.Single(page.FindAll(".album-intents li"));
    }

    [Fact]
    public void AlbumNeitherOwnedNorTargeted_IsOutOfTheCollection()
    {
        _catalog.Album = Album() with { Album = Summary() with { IsInCollection = false }, Editions = [] };

        var page = Open();

        Assert.Equal("Hors collection", page.FindAll(".album-facts li")[^1].TextContent.Trim());
        Assert.Equal(("out", "Hors collection"), (page.Find(".collection-status").GetAttribute("data-status"), page.Find(".collection-status strong").TextContent.Trim()));
        // Neither edition nor intent: no block of the editions, which would hold nothing.
        Assert.DoesNotContain(page.FindAll("section.block .block-title"), t => t.TextContent.Trim() == "Dans quelles éditions ?");
    }

    [Fact]
    public void Editions_AreThoseOfTheCollection_AllAtOnce_EachWithItsDetail()
    {
        _catalog.Album = Album();

        var page = Open();

        // The edition targeted by an intent (2023) is not one of them.
        var editions = BlockOf(page, "Dans quelles éditions ?").QuerySelectorAll(".edition");
        Assert.Equal(["Casterman [2001] - ISBN\u00A02-203-34424-5", "Casterman (Univers d'auteurs) [2010]"],
            editions.Select(e => e.QuerySelector(".edition-label")!.TextContent.Trim()));
        Assert.All(editions, e => Assert.NotNull(e.QuerySelector(".edition-detail")));
        Assert.Equal("Offerte", Row(page, "Mode d'acquisition", edition: 1));
    }

    [Fact]
    public void Isbn_InTheLabelAndInTheDetailOfAnEdition_IsASingleBlock()
    {
        _catalog.Album = Album();

        var edition = BlockOf(Open(), "Dans quelles éditions ?").QuerySelectorAll(".edition")[0];

        // Never broken at the end of a line (fonctionnel/presentation/libelle-d-une-edition.md).
        Assert.Equal("ISBN\u00A02-203-34424-5", edition.QuerySelector(".edition-label .label-isbn")!.TextContent);
        Assert.Equal("2-203-34424-5", edition.QuerySelector("dd.edition-isbn")!.TextContent.Trim());
    }

    [Fact]
    public void Cover_IsTheFirstCoverOfTheOldestEditionThatHasOne()
    {
        _catalog.Album = Album();

        var page = Open();

        Assert.Equal(VisualMediaRoutes.Display(Visuals[0]), page.Find(".album-cover img").GetAttribute("src"));
        Assert.Equal("Couverture de Lointaines îles du vent", page.Find(".album-cover img").GetAttribute("alt"));
    }

    [Fact]
    public void Cover_IsTheGenericVisual_WhenNoEditionOfTheCollectionHasOne()
    {
        _catalog.Album = Album() with { Editions = [Owned(FirstEditionId, 2001)] };

        Assert.NotNull(Open().Find(".album-cover svg.cover-generic"));
    }

    [Fact]
    public void EditionDetail_PresentsTheEdition_ThePublisherLeadingToItsWebsite_WithoutWhatIsNotSet()
    {
        _catalog.Album = Album();

        var page = Open();

        var publisher = RowElement(page, "Éditeur").QuerySelector("a")!;
        Assert.Equal(("Casterman", "https://www.casterman.com", "_blank", "noopener noreferrer"),
            (publisher.TextContent.Trim(), publisher.GetAttribute("href"), publisher.GetAttribute("target"), publisher.GetAttribute("rel")));
        Assert.Equal("2001", Row(page, "Année d'édition"));
        Assert.Equal("Cartonnée", Row(page, "Reliure"));
        Assert.Equal("Normal (A4)", Row(page, "Format"));
        Assert.Equal("En couleur", Row(page, "Couleur"));
        // A data not set has no row, which would only weigh the detail down: no collection, no number of pages, no initial
        // value; nothing says « Non renseigné ».
        var labels = page.FindAll(".edition-detail")[0].QuerySelectorAll("dt").Select(dt => dt.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Collection", labels);
        Assert.DoesNotContain("Nombre de pages", labels);
        Assert.DoesNotContain("Valeur initiale", labels);
        Assert.Empty(page.FindAll(".edition-detail .not-set"));
        Assert.Equal("Univers d'auteurs", Row(page, "Collection", edition: 1));
    }

    [Fact]
    public void EditionDetail_PresentsTheCopy_WithTheLabelsOfItsMode_AndItsAmountsInEurosOfTheDayNotAvailableYet()
    {
        _catalog.Album = Album();

        var page = Open();

        Assert.Equal("Achat", Row(page, "Mode d'acquisition"));
        Assert.Equal("09/09/2006", Row(page, "Date d'achat"));
        var price = RowElement(page, "Prix d'achat");
        Assert.Contains("16,95", price.TextContent);
        Assert.Contains("en euros d'aujourd'hui : pas encore disponible", price.TextContent);
        Assert.Equal("Neuve", Row(page, "Neuve ou d'occasion"));
        Assert.Equal("Non", Row(page, "Dédicacée"));
    }

    [Fact]
    public void EditionDetail_WithoutAmount_PresentsItsEstimatedValue_NotAvailableYet_UnlessFree()
    {
        _catalog.Album = Album();

        var page = Open();
        Assert.Equal("25/12/2011", Row(page, "Date d'acquisition", edition: 1));
        Assert.Equal("Valeur estimée : pas encore disponible", Row(page, "Valeur d'acquisition", edition: 1));

        _catalog.Album = Album() with { Editions = [Owned(FirstEditionId, 2001) with { IsFree = true, AcquisitionMode = AcquisitionMode.Gift, AcquisitionAmount = null, AcquisitionCurrency = null }] };
        Assert.Equal("Gratuite", Row(Open(), "Valeur d'acquisition"));
    }

    [Fact]
    public void EditionDetail_IsDatedInTheTimeZoneOfTheBrowser()
    {
        _catalog.Album = Album();

        var page = Open();

        // Its date of entry into the collection, which the application sets alone, is not one of them: only the date of
        // acquisition, in the detail of the copy, tells when it was acquired.
        Assert.Equal("Édition créée le 02/02/2026, modifiée le 10/10/2026.",
            BlockOf(page, "Dans quelles éditions ?").QuerySelector(".edition-dates")!.TextContent.Trim());
    }

    [Fact]
    public void Visuals_OfAnEdition_AreThumbnailsInTheirOrder_EachOpeningTheViewer()
    {
        _catalog.Album = Album();

        var page = Open();

        var thumbnails = page.FindAll(".edition")[0].QuerySelectorAll(".visual-thumbnails a");
        Assert.Equal(["Couv.", "1", "2", "4e"], thumbnails.Select(t => t.TextContent.Trim()));
        Assert.Equal(["Couverture", "Planche 1", "Planche 2", "4e de couverture"], thumbnails.Select(t => t.GetAttribute("aria-label")));
        Assert.Equal(AlbumRecordQuery.ForVisual(FirstEditionId, 3).Href(AlbumId), thumbnails[2].GetAttribute("href"));
        Assert.Empty(page.FindAll("dialog"));
        Assert.Equal(["album"], _audience.Functions);
    }

    [Fact]
    public void Visuals_OfAnEditionWithoutAny_SaySo()
    {
        _catalog.Album = Album();

        var page = Open();

        Assert.Equal("Aucun visuel pour cette édition.", page.FindAll(".edition")[1].QuerySelector(".edition-visuals-none")!.TextContent.Trim());
    }

    [Fact]
    public void Viewer_OpenedByTheAddress_ShowsTheVisual_ItsTypeAndPosition_ItsNeighbours_AndTheOriginal()
    {
        _catalog.Album = Album();

        var page = Open($"?edition={FirstEditionId}&visual=3");

        var dialog = page.Find("app-modal-dialog dialog.visual-dialog");
        Assert.Equal("Planche 2", dialog.QuerySelector(".visual-dialog-title")!.TextContent.Trim());
        Assert.Equal("Casterman [2001] - ISBN\u00A02-203-34424-5", dialog.QuerySelector(".visual-dialog-edition")!.TextContent.Trim());
        Assert.Equal(VisualMediaRoutes.Display(Visuals[2]), dialog.QuerySelector("img")!.GetAttribute("src"));
        Assert.Equal("3 sur 4", dialog.QuerySelector(".visual-position")!.TextContent.Trim());
        Assert.Equal(AlbumRecordQuery.ForVisual(FirstEditionId, 2).Href(AlbumId), dialog.QuerySelector("a[aria-label='Visuel précédent']")!.GetAttribute("href"));
        Assert.Equal(AlbumRecordQuery.ForVisual(FirstEditionId, 4).Href(AlbumId), dialog.QuerySelector("a[aria-label='Visuel suivant']")!.GetAttribute("href"));
        // The original image in a new tab; the closing back to the record alone.
        var original = dialog.QuerySelector("a.visual-original")!;
        Assert.Equal((VisualMediaRoutes.Original(Visuals[2]), "_blank", "noopener noreferrer"),
            (original.GetAttribute("href"), original.GetAttribute("target"), original.GetAttribute("rel")));
        Assert.Equal(AppPaths.Album(AlbumId), dialog.QuerySelector("[data-dialog-close]")!.GetAttribute("href"));
        // The thumbnail of the visual open is marked as such.
        Assert.Equal("true", page.FindAll(".edition")[0].QuerySelectorAll(".visual-thumbnails a")[2].GetAttribute("aria-current"));
        Assert.Equal(["album.visual", "album"], _audience.Functions);
    }

    [Fact]
    public void Viewer_GoesRoundTheEdition()
    {
        _catalog.Album = Album();

        var dialog = Open($"?edition={FirstEditionId}&visual=4").Find("dialog.visual-dialog");

        Assert.Equal(AlbumRecordQuery.ForVisual(FirstEditionId, 1).Href(AlbumId), dialog.QuerySelector("a[aria-label='Visuel suivant']")!.GetAttribute("href"));
    }

    [Theory]
    [InlineData("?edition=0197a0b2-0000-7000-8000-0000000000e3&visual=1")]
    [InlineData("?edition=0197a0b2-0000-7000-8000-0000000000e1&visual=9")]
    [InlineData("?edition=0197a0b2-0000-7000-8000-0000000000e2&visual=1")]
    public void Viewer_StaysClosed_ForAVisualTheCollectionDoesNotHave(string query)
    {
        _catalog.Album = Album();

        Assert.Empty(Open(query).FindAll("dialog"));
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
    public void Intent_OnAnEdition_IsFramedUnderTheEditions_ByItsLabel_ItsFacts_AndTheDateItWasPosed()
    {
        _catalog.Album = Album();

        var page = Open();

        var intents = BlockOf(page, "Dans quelles éditions ?").QuerySelector(".album-intents")!;
        Assert.Equal("Intention d'achat", intents.QuerySelector(".album-intents-title")!.TextContent.Trim());
        var intent = Assert.Single(intents.QuerySelectorAll("li"));
        Assert.Equal("Casterman [2023]", intent.QuerySelector(".intent-label")!.TextContent.Trim());
        Assert.Equal("Édition spéciale · Normal (A4) · Cartonnée · 112 pages", intent.QuerySelector(".intent-facts")!.TextContent.Trim());
        // Posed at 23:30 universal time on 9 October: the 10th in Paris, the time zone of the browser.
        Assert.Equal("Posée le 10/10/2026", intent.QuerySelector(".intent-date")!.TextContent.Trim());
        // An intent is never one of the editions, which has its own detail.
        Assert.Equal(2, page.FindAll(".edition").Count);
    }

    [Fact]
    public void Intents_OnSeveralEditions_AreAllFramed_UnderAPluralMention_TheirFactsLeavingOutWhatIsNotSet()
    {
        var other = Intended() with
        {
            Edition = new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Casterman", null, "Corto Maltese en couleur", 2016, null),
            Category = null,
            Format = null,
            Binding = null,
            PageCount = null,
        };
        _catalog.Album = Album() with { Editions = [.. Album().Editions, other] };

        var page = Open();

        Assert.Equal("Intentions d'achat", page.Find(".album-intents-title").TextContent.Trim());
        Assert.Equal(["Casterman [2023]", "Casterman (Corto Maltese en couleur) [2016]"],
            page.FindAll(".album-intents li .intent-label").Select(l => l.TextContent.Trim()));
        Assert.Empty(page.FindAll(".album-intents li")[1].QuerySelectorAll(".intent-facts"));
    }

    [Fact]
    public void Intent_OnTheWholeAlbum_TargetsAnyEditionOfIt_WhichHasNone()
    {
        _catalog.Album = Album() with
        {
            Album = Summary() with { IsInCollection = false },
            Editions = [],
            PurchaseIntent = new PurchaseIntentItem(Guid.NewGuid(), Created, Created),
        };

        var page = Open();

        Assert.Empty(page.FindAll(".editions-none"));
        var intent = Assert.Single(page.FindAll(".album-intents li"));
        Assert.Equal("Toute édition de l'album", intent.QuerySelector(".intent-label")!.TextContent.Trim());
        Assert.Equal("Posée le 02/02/2026", intent.QuerySelector(".intent-date")!.TextContent.Trim());
    }

    [Fact]
    public void Intents_OfAnAlbumWithoutAny_AreNotFramed()
    {
        _catalog.Album = Album() with { Editions = [.. Album().Editions.Where(e => e.IsInCollection)] };

        var page = Open();

        Assert.Empty(page.FindAll(".album-intents"));
    }

    [Fact]
    public void RestOfTheSeries_ListsItsAlbumsInTheirOrder_EachWithItsVolumeAndMembership_TheAlbumShownPutForward()
    {
        _catalog.Album = Album();

        var page = Open();

        var series = BlockOf(page, "Et le reste de la série ?");
        Assert.Equal(AppPaths.SeriesRecord(SeriesId), series.QuerySelector("a.series-record")!.GetAttribute("href"));
        Assert.Equal("Voir la fiche de la série", series.QuerySelector("a.series-record")!.TextContent.Trim());
        Assert.NotNull(series.QuerySelector("details[open] > summary"));
        Assert.Equal("Corto Maltese 4 albums, dont 2 dans la collection",
            string.Join(' ', series.QuerySelectorAll("summary > span").Select(s => s.TextContent.Trim())));
        var albums = series.QuerySelectorAll("details ol > li");
        Assert.Equal(["T. 1", "T. 5", "T. 6", "HS 1"], albums.Select(a => a.QuerySelector(".series-album-volume")!.TextContent.Trim()));
        Assert.Equal(["La Jeunesse", "Lointaines îles du vent", "Les Celtiques", "Corto Maltese - HS 1"],
            albums.Select(a => a.QuerySelector(".series-album-title")!.TextContent.Trim()));
        Assert.Equal(["Dans la collection", "Dans la collection", "Intention d'achat", "Hors collection"],
            albums.Select(a => a.QuerySelector(".tag")!.TextContent.Trim()));
        // The album shown is no link to itself; the others lead to their records.
        Assert.Equal("page", albums[1].QuerySelector(".series-album-title")!.GetAttribute("aria-current"));
        Assert.Null(albums[1].QuerySelector("a"));
        Assert.Equal([AppPaths.Album(SeriesAlbumIds[0]), AppPaths.Album(SeriesAlbumIds[2]), AppPaths.Album(SeriesAlbumIds[3])],
            series.QuerySelectorAll("details ol a").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void RestOfTheSeries_OfAnAlbumWithoutSeries_IsNotPresented()
    {
        _catalog.Album = Album() with { Album = Summary() with { SeriesId = null, SeriesTitle = null }, SeriesAlbums = [] };

        var page = Open();

        Assert.DoesNotContain(page.FindAll("section.block .block-title"), t => t.TextContent.Trim() == "Et le reste de la série ?");
    }

    [Fact]
    public void UnknownAlbum_PresentsTheFunctionalError()
    {
        _catalog.AlbumStatus = HttpStatusCode.NotFound;

        var page = Open();

        Assert.Equal("functional", page.Find("[role='alert']").GetAttribute("data-error-category"));
    }

    private IRenderedComponent<AlbumRecord> Open(string query = "")
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"{AppPaths.Album(AlbumId)}{query}");
        return Render<AlbumRecord>(parameters => parameters.Add(p => p.Id, AlbumId).AddCascadingValue(_request.Context));
    }

    private static IElement BlockOf(IRenderedComponent<AlbumRecord> page, string title) =>
        page.FindAll("section.block").Single(block => block.QuerySelector(".block-title")!.TextContent.Trim() == title);

    // A row of the detail of an edition, the first one by default.
    private static IElement RowElement(IRenderedComponent<AlbumRecord> page, string label, int edition = 0) =>
        page.FindAll(".edition-detail")[edition].QuerySelectorAll("dl > div").Single(row => row.QuerySelector("dt")!.TextContent.Trim() == label).QuerySelector("dd")!;

    private static string Row(IRenderedComponent<AlbumRecord> page, string label, int edition = 0) => RowElement(page, label, edition).TextContent.Trim();

    private static readonly Guid[] Visuals = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

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

    // The albums of the series, the one shown second: owned, shown, wished for, catalogued only.
    private static readonly Guid[] SeriesAlbumIds = [Guid.NewGuid(), AlbumId, Guid.NewGuid(), Guid.NewGuid()];

    private static List<AlbumCard> SeriesAlbums() =>
    [
        new(Summary() with { Id = SeriesAlbumIds[0], Title = "La Jeunesse", VolumeNumber = 1 }, null, null, false),
        new(Summary(), null, null, false),
        new(Summary() with { Id = SeriesAlbumIds[2], Title = "Les Celtiques", VolumeNumber = 6, IsInCollection = false }, null, null, true),
        new(Summary() with { Id = SeriesAlbumIds[3], Title = null, IsSpecialIssue = true, VolumeNumber = 1, IsInCollection = false }, null, null, false),
    ];

    private static EditionDetail Intended() => Owned(IntendedEditionId, 2023) with
    {
        Category = EditionCategory.SpecialEdition,
        PageCount = 112,
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
        var first = Owned(FirstEditionId, 2001) with
        {
            Edition = new EditionSummary(FirstEditionId, Guid.NewGuid(), "Casterman", null, null, 2001, "2-203-34424-5"),
            Visuals =
            [
                new EditionVisualItem(Visuals[0], VisualType.Cover, 1, Created, Created),
                new EditionVisualItem(Visuals[1], VisualType.Plate, 1, Created, Created),
                new EditionVisualItem(Visuals[2], VisualType.Plate, 2, Created, Created),
                new EditionVisualItem(Visuals[3], VisualType.BackCover, 1, Created, Created),
            ],
        };
        var second = Owned(SecondEditionId, 2010) with
        {
            Edition = new EditionSummary(SecondEditionId, Guid.NewGuid(), "Casterman", Guid.NewGuid(), "Univers d'auteurs", 2010, null),
            AcquisitionMode = AcquisitionMode.Gift,
            AcquisitionDate = new DateOnly(2011, 12, 25),
            AcquisitionAmount = null,
            AcquisitionCurrency = null,
        };
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
            [first, second, Intended()],
            null,
            SeriesAlbums(),
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
