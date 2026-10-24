using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The albums as cards — cover, rating, membership of the collection, purchase intent — and the series
/// represented by their number of albums and the covers of their first ones, in the record of a series,
/// the bibliography of an author and the works of a genre (visuel/fiche-serie.md, visuel/fiche-auteur.md).
/// </summary>
public sealed class CatalogAlbumCardTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;

    public CatalogAlbumCardTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Cover_IsThatOfTheOldestEditionWithACover_OwnedOrNot()
    {
        var publisher = await CreatePublisherAsync("Dargaud");
        var series = await CreateSeriesAsync("Blueberry");
        var album = await CreateAlbumAsync("Fort Navajo", series.Id, volume: 1);
        var recent = await CreateOwnedEditionAsync(album.Id, publisher.Id, 2010);
        await UploadAsync(album.Id, recent, VisualType.Cover);
        // Older, but without a cover: a plate only.
        var withoutCover = await CreateOwnedEditionAsync(album.Id, publisher.Id, 1970);
        await UploadAsync(album.Id, withoutCover, VisualType.Plate);
        // Without year: after those with one.
        var undated = await CreateOwnedEditionAsync(album.Id, publisher.Id, null);
        await UploadAsync(album.Id, undated, VisualType.Cover);
        // The oldest with a cover, though not owned (fonctionnel/regles-metier.md § Couverture d'un album: no filter here).
        var intended = await CreateIntendedEditionAsync(album.Id, publisher.Id, 1985);
        var expected = await UploadAsync(album.Id, intended, VisualType.Cover);

        var card = Assert.Single((await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}")).Albums);

        Assert.Equal(expected, card.Cover);
    }

    [Fact]
    public async Task Cover_AmongEditionsOfTheSameYear_IsThatOfTheFirstCreated()
    {
        var publisher = await CreatePublisherAsync("Casterman");
        var series = await CreateSeriesAsync("Corto Maltese");
        var album = await CreateAlbumAsync("La Ballade de la mer salée", series.Id, volume: 1);
        var first = await CreateOwnedEditionAsync(album.Id, publisher.Id, 1975);
        var second = await CreateOwnedEditionAsync(album.Id, publisher.Id, 1975);
        await UploadAsync(album.Id, second, VisualType.Cover);
        var expected = await UploadAsync(album.Id, first, VisualType.Cover);

        var card = Assert.Single((await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}")).Albums);

        Assert.Equal(expected, card.Cover);
    }

    [Fact]
    public async Task Cover_OfTheEditionRetained_IsItsFirstCoverInTheOrderOfItsVisuals()
    {
        var publisher = await CreatePublisherAsync("Glénat");
        var series = await CreateSeriesAsync("Les Passagers du vent");
        var album = await CreateAlbumAsync("La Fille sous la dunette", series.Id, volume: 1);
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, 1980);
        var jacket = await UploadAsync(album.Id, edition, VisualType.Cover);
        var underneath = await UploadAsync(album.Id, edition, VisualType.Cover);
        // The user puts the second cover first (fonctionnel/regles-metier.md § Ordre des visuels d'une édition).
        var visuals = await GetAsync<EditionVisualsForm>(VisualsUri(album.Id, edition));
        var arrangement = visuals.Visuals.OrderBy(v => v.Id == underneath.VisualId ? 0 : 1).Select(v => new VisualArrangement(v.Id, v.Type));
        (await _client.PutAsJsonAsync(VisualsUri(album.Id, edition), new ArrangeVisualsRequest(arrangement.ToList(), visuals.AlbumVersion)))
            .EnsureSuccessStatusCode();

        var card = Assert.Single((await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}")).Albums);

        Assert.Equal(underneath, card.Cover);
        Assert.NotEqual(jacket, card.Cover);
    }

    [Fact]
    public async Task Card_WithoutAnyCover_HasNone()
    {
        var publisher = await CreatePublisherAsync("Lombard");
        var series = await CreateSeriesAsync("Thorgal");
        var withEdition = await CreateAlbumAsync("La Magicienne trahie", series.Id, volume: 1);
        var edition = await CreateOwnedEditionAsync(withEdition.Id, publisher.Id, 1980);
        await UploadAsync(withEdition.Id, edition, VisualType.BackCover);
        await CreateAlbumAsync("L'Île des mers gelées", series.Id, volume: 2);

        var detail = await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}");

        Assert.Equal(new AlbumCover?[] { null, null }, detail.Albums.Select(a => a.Cover));
    }

    [Fact]
    public async Task Card_CarriesTheRatingTheMembershipOfTheCollectionAndWhetherAnIntentTargetsTheAlbum()
    {
        var publisher = await CreatePublisherAsync("Dupuis");
        var series = await CreateSeriesAsync("Gaston");
        var owned = await CreateAlbumAsync("Gala de gaffes", series.Id, volume: 1, rating: AlbumRating.VeryGood);
        await CreateOwnedEditionAsync(owned.Id, publisher.Id, 1963);
        var albumIntended = await CreateAlbumAsync("Gaffes en gros", series.Id, volume: 2);
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumIntended.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, albumIntended.Version));
        var editionIntended = await CreateAlbumAsync("Gare aux gaffes", series.Id, volume: 3, rating: AlbumRating.Poor);
        await CreateIntendedEditionAsync(editionIntended.Id, publisher.Id, 1965);
        await CreateAlbumAsync("Gaffes à gogo", series.Id, volume: 4);

        var detail = await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}");

        Assert.Equal(
            [
                ((AlbumRating?)AlbumRating.VeryGood, true, false),
                ((AlbumRating?)null, false, true),
                ((AlbumRating?)AlbumRating.Poor, false, true),
                (null, false, false),
            ],
            detail.Albums.Select(a => (a.Rating, a.Album.IsInCollection, a.HasPurchaseIntent)));
    }

    [Fact]
    public async Task Series_IsRepresentedByTheCoversOfItsFirstRegularVolumes_ItsCountTakingEveryAlbum()
    {
        var publisher = await CreatePublisherAsync("Soleil");
        var series = await CreateSeriesAsync("Lanfeust de Troy");
        // In the order of the series: volume 1, the omnibus of volumes 1 to 4, volume 2, then a special issue.
        var volume1 = await CreateAlbumAsync("L'Ivoire du Magohamoth", series.Id, volume: 1);
        var omnibus = await CreateAlbumAsync("Intégrale", series.Id, type: AlbumType.Omnibus, start: 1, end: 4);
        var volume2 = await CreateAlbumAsync("Thanos l'incongru", series.Id, volume: 2);
        var special = await CreateAlbumAsync("Les Trolls", series.Id, volume: 1, specialIssue: true);
        var cover1 = await UploadAsync(volume1.Id, await CreateOwnedEditionAsync(volume1.Id, publisher.Id, 1994), VisualType.Cover);
        await UploadAsync(omnibus.Id, await CreateOwnedEditionAsync(omnibus.Id, publisher.Id, 2005), VisualType.Cover);
        var cover2 = await UploadAsync(volume2.Id, await CreateOwnedEditionAsync(volume2.Id, publisher.Id, 1995), VisualType.Cover);
        await UploadAsync(special.Id, await CreateOwnedEditionAsync(special.Id, publisher.Id, 2000), VisualType.Cover);

        var overview = (await GetAsync<SeriesDetail>($"/catalog/series/{series.Id}")).Overview;

        // The pile of the mockups shows the first regular volumes only, a missing one being a generic card
        // (visuel/fiche-serie.md): neither the omnibus nor the special issue stands in for the third.
        Assert.Equal(4, overview.AlbumCount);
        Assert.Equal([cover1, cover2], overview.FirstVolumeCovers);
    }

    [Fact]
    public async Task Bibliography_PresentsTheAlbumsAsCards_AndEachSeriesWithItsAlbumCountAndTheCoversOfItsFirstVolumes()
    {
        var publisher = await CreatePublisherAsync("Delcourt");
        var author = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest("Sfar", "Joann", null, null, null));
        var other = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest("Trondheim", "Lewis", null, null, null));
        var credited = new ContributionContent[] { new(author.Id, ContributionRole.Scenarist) };
        // Credited on the series itself only: four albums credited to another author (sparing the copy of the
        // contributions of the series), created out of their order, the second without cover.
        var others = new ContributionContent[] { new(other.Id, ContributionRole.Illustrator) };
        var donjon = await CreateSeriesAsync("Donjon", contributions: credited);
        var donjon3 = await CreateAlbumAsync("Le Roi de la bagarre", donjon.Id, volume: 3, contributions: others);
        var donjon1 = await CreateAlbumAsync("Cœur de canard", donjon.Id, volume: 1, contributions: others);
        await CreateAlbumAsync("La Princesse des barbares", donjon.Id, volume: 4, contributions: others);
        await CreateAlbumAsync("Le Garde du cœur", donjon.Id, volume: 2, contributions: others);
        var cover3 = await UploadAsync(donjon3.Id, await CreateOwnedEditionAsync(donjon3.Id, publisher.Id, 2000), VisualType.Cover);
        var cover1 = await UploadAsync(donjon1.Id, await CreateOwnedEditionAsync(donjon1.Id, publisher.Id, 1998), VisualType.Cover);
        // Credited on one of its two albums.
        var chat = await CreateSeriesAsync("Le Chat du rabbin");
        var chat1 = await CreateAlbumAsync("La Bar-Mitsva", chat.Id, volume: 1, rating: AlbumRating.Good, contributions: credited);
        await CreateAlbumAsync("Le Malka des lions", chat.Id, volume: 2);
        var chatCover = await UploadAsync(chat1.Id, await CreateOwnedEditionAsync(chat1.Id, publisher.Id, 2002), VisualType.Cover);
        // An album without series.
        var oneShot = await CreateAlbumAsync("Klezmer", null, contributions: credited);

        var bibliography = (await GetAsync<AuthorDetail>($"/catalog/authors/{author.Id}")).Bibliography;

        // "Chat du rabbin [Le]" < "Donjon" < "Klezmer".
        var chatEntry = bibliography[0].Series!;
        Assert.Equal(2, chatEntry.Overview.AlbumCount);
        Assert.Equal([chatCover, null], chatEntry.Overview.FirstVolumeCovers);
        var chatCard = Assert.Single(chatEntry.Albums).Album;
        Assert.Equal((chat1.Id, (AlbumRating?)AlbumRating.Good, (AlbumCover?)chatCover, true, false),
            (chatCard.Album.Id, chatCard.Rating, chatCard.Cover, chatCard.Album.IsInCollection, chatCard.HasPurchaseIntent));

        var donjonEntry = bibliography[1].Series!;
        Assert.Empty(donjonEntry.Albums);
        Assert.Equal(4, donjonEntry.Overview.AlbumCount);
        // The first three, in the order of the series.
        Assert.Equal([cover1, null, cover3], donjonEntry.Overview.FirstVolumeCovers);

        var oneShotCard = bibliography[2].Album!.Album;
        Assert.Equal((oneShot.Id, (AlbumCover?)null), (oneShotCard.Album.Id, oneShotCard.Cover));
    }

    [Fact]
    public async Task Works_PresentTheAlbumsAsCards_AndEachSeriesWithItsAlbumCountAndTheCoversOfItsFirstVolumes()
    {
        var publisher = await CreatePublisherAsync("Futuropolis");
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Chronique {Guid.NewGuid():N}"));
        // A series of which one album only is of the genre.
        var series = await CreateSeriesAsync("Le Combat ordinaire");
        var first = await CreateAlbumAsync("Le Combat ordinaire", series.Id, volume: 1);
        var second = await CreateAlbumAsync("Les Quantités négligeables", series.Id, volume: 2, genreIds: [genre.Id]);
        var firstCover = await UploadAsync(first.Id, await CreateOwnedEditionAsync(first.Id, publisher.Id, 2003), VisualType.Cover);
        var secondCover = await UploadAsync(second.Id, await CreateOwnedEditionAsync(second.Id, publisher.Id, 2004), VisualType.Cover);
        var oneShot = await CreateAlbumAsync("Une métamorphose iranienne", null, rating: AlbumRating.Average, genreIds: [genre.Id]);

        var works = (await GetAsync<GenreWorks>($"/catalog/genres/{genre.Id}/works")).Works.Items;

        // "Combat ordinaire [Le]" < "métamorphose iranienne [Une]".
        var seriesEntry = works[0].Series!;
        Assert.Equal(2, seriesEntry.Overview.AlbumCount);
        Assert.Equal([firstCover, secondCover], seriesEntry.Overview.FirstVolumeCovers);
        Assert.Equal([(second.Id, (AlbumCover?)secondCover)], seriesEntry.Albums.Select(a => (a.Album.Id, a.Cover)));
        Assert.Equal((oneShot.Id, (AlbumRating?)AlbumRating.Average, (AlbumCover?)null), (works[1].Album!.Album.Id, works[1].Album!.Rating, works[1].Album!.Cover));
    }

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private static string VisualsUri(Guid albumId, Guid editionId) => $"/admin/albums/{albumId}/editions/{editionId}/visuals";

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"{name} {Guid.NewGuid():N}", null));

    private Task<SeriesForm> CreateSeriesAsync(string title, ContributionContent[]? contributions = null) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, NoTemplate, [], [], contributions ?? []));

    private Task<AlbumForm> CreateAlbumAsync(
        string title, Guid? seriesId, int? volume = null, AlbumRating? rating = null, Guid[]? genreIds = null,
        ContributionContent[]? contributions = null, AlbumType type = AlbumType.Regular, bool specialIssue = false, int? start = null,
        int? end = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, type, specialIssue, volume, start, end, null, null, null, null, rating, genreIds ?? [], [],
            contributions ?? []));

    private async Task<Guid> CreateOwnedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear) =>
        (await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, publicationYear, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)))).Id;

    /// <returns>The edition targeted by the intent, not owned.</returns>
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear)
    {
        var intents = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, publicationYear, null, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        return intents.Intents.Single(i => i.EditionId is not null).EditionId!.Value;
    }

    /// <returns>The visual uploaded, as a cover.</returns>
    private async Task<AlbumCover> UploadAsync(Guid albumId, Guid editionId, VisualType type)
    {
        var before = (await GetAsync<EditionVisualsForm>(VisualsUri(albumId, editionId))).Visuals.Select(v => v.Id).ToHashSet();
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(type.ToString()), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync(VisualsUri(albumId, editionId), form);
        response.EnsureSuccessStatusCode();
        var visual = (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals.Single(v => !before.Contains(v.Id));
        return new AlbumCover(visual.Id);
    }

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) =>
        (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
