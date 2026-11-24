using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the series of the consultation (<c>/catalog/series</c>): what a row carries, the filter of the possession that
/// limits the list to the collection, the same as in every list (fonctionnel/regles-metier/perimetre-de-la-consultation.md),
/// and the covers of the pile of a series, taken among the editions the list presents
/// (fonctionnel/regles-metier/couverture-d-un-album.md).
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its series with a marker of its own, and
/// narrows the list to it by searching it.
/// </remarks>
public sealed class CatalogSeriesListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public CatalogSeriesListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Row_CarriesTheTitleThePileOfCoversTheAlbumsAndThoseInTheCollection()
    {
        var publisher = await CreatePublisherAsync("Dupuis");
        var series = await CreateSeriesAsync($"Gaston {_marker}");
        var first = await CreateAlbumAsync(series.Id, 1);
        var cover = await UploadCoverAsync(first.Id, await CreateOwnedEditionAsync(first.Id, publisher.Id, 1960));
        await CreateIntendedEditionAsync((await CreateAlbumAsync(series.Id, 2)).Id, publisher.Id, 1961);
        await CreateAlbumAsync(series.Id, 3);

        var row = Assert.Single((await ListAsync($"q={_marker}")).Series.Items);

        Assert.Equal((series.Id, $"Gaston {_marker}"), (row.Id, row.Title));
        Assert.Equal(3, row.Overview.AlbumCount);
        Assert.Equal([cover, null, null], row.Overview.FirstVolumeCovers);
        // An album only targeted by an intent is no album of the collection.
        Assert.Equal(1, row.AlbumsInCollectionCount);
    }

    [Fact]
    public async Task Possession_KeepsEachSeriesByThePossessionOfItsAlbums()
    {
        var publisher = await CreatePublisherAsync("Casterman");
        var owned = await CreateSeriesAsync($"A possédée {_marker}");
        await CreateOwnedEditionAsync((await CreateAlbumAsync(owned.Id, 1)).Id, publisher.Id, 1990);
        var intended = await CreateSeriesAsync($"B visée {_marker}");
        await CreateIntendedEditionAsync((await CreateAlbumAsync(intended.Id, 1)).Id, publisher.Id, 1990);
        var outside = await CreateSeriesAsync($"C sans album {_marker}");

        Assert.Equal([owned.Id, intended.Id, outside.Id], await IdsAsync($"q={_marker}"));
        Assert.Equal([owned.Id], await IdsAsync($"q={_marker}&state=InCollection"));
        Assert.Equal([intended.Id], await IdsAsync($"q={_marker}&state=PurchaseIntent"));
        Assert.Equal([outside.Id], await IdsAsync($"q={_marker}&state=OutsideCollection"));
    }

    [Fact]
    public async Task Covers_AreTakenAmongTheEditionsTheListPresents()
    {
        var publisher = await CreatePublisherAsync("Dargaud");
        var series = await CreateSeriesAsync($"Blueberry {_marker}");
        var first = await CreateAlbumAsync(series.Id, 1);
        var ownedCover = await UploadCoverAsync(first.Id, await CreateOwnedEditionAsync(first.Id, publisher.Id, 2010));
        // Older, but only targeted by an intent: the list of all the series presents it, not that of the owned ones.
        var intendedCover = await UploadCoverAsync(first.Id, await CreateIntendedEditionAsync(first.Id, publisher.Id, 1965));
        // Not owned at all: the list limited to the collection presents none of its editions, hence the generic visual.
        var second = await CreateAlbumAsync(series.Id, 2);
        var secondCover = await UploadCoverAsync(second.Id, await CreateIntendedEditionAsync(second.Id, publisher.Id, 1966));

        Assert.Equal([intendedCover, secondCover], Assert.Single((await ListAsync($"q={_marker}")).Series.Items).Overview.FirstVolumeCovers);
        Assert.Equal([ownedCover, null], Assert.Single((await ListAsync($"q={_marker}&state=InCollection")).Series.Items).Overview.FirstVolumeCovers);
    }

    [Fact]
    public async Task Filters_AreNamedAsTheListsNameThem()
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Western {_marker}"));

        var list = await ListAsync($"genreId={genre.Id}");

        Assert.Equal(new SeriesListFilters(null, null, null, new GenreListItem(genre.Id, $"Western {_marker}"), null), list.Filters);
    }

    private async Task<SeriesCatalogList> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<SeriesCatalogList>($"/catalog/series?{query}"))!;

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Series.Items.Select(r => r.Id)];

    private static string VisualsUri(Guid albumId, Guid editionId) => $"/admin/albums/{albumId}/editions/{editionId}/visuals";

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"{name} {Guid.NewGuid():N}", null, []));

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(title, null, null, null, false, false, false, null, null, NoTemplate, [], [], []));

    private Task<AlbumForm> CreateAlbumAsync(Guid seriesId, int volumeNumber) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            null, null, seriesId, AlbumType.Regular, false, volumeNumber, null, null, null, null, null, null, null, [], [], []));

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

    /// <returns>The visual uploaded as a cover of the edition.</returns>
    private async Task<AlbumCover> UploadCoverAsync(Guid albumId, Guid editionId)
    {
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync(VisualsUri(albumId, editionId), form);
        response.EnsureSuccessStatusCode();
        var visual = (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals.Single(v => v.Type == VisualType.Cover);
        return new AlbumCover(visual.Id, TestImages.UploadedPalette);
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
