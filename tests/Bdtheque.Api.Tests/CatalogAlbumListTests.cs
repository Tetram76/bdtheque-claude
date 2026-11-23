using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the albums of the consultation (<c>/catalog/albums</c>): what a row carries, the filter of the possession that
/// limits the list to the collection, list by list (fonctionnel/regles-metier/perimetre-de-la-consultation.md), and the
/// cover of an album, taken among the editions the list presents (fonctionnel/regles-metier/couverture-d-un-album.md).
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its albums with a marker of its own, and
/// narrows the list to it by searching it.
/// </remarks>
public sealed class CatalogAlbumListTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public CatalogAlbumListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Row_CarriesTheCoverTheAuthorsEachOnceTheDateTheRatingAndThePossession()
    {
        var publisher = await CreatePublisherAsync("Dupuis");
        var franquin = await CreateAuthorAsync("Franquin");
        var greg = await CreateAuthorAsync("Greg");
        var album = await CreateAlbumAsync(
            $"QRN sur Bretzelburg {_marker}", rating: AlbumRating.VeryGood, year: 1966, month: 3,
            contributions: [new(greg.Id, ContributionRole.Scenarist), new(franquin.Id, ContributionRole.Scenarist), new(franquin.Id, ContributionRole.Illustrator)]);
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, 1966);
        var cover = await UploadCoverAsync(album.Id, edition);

        var page = await ListAsync($"q={_marker}");

        var row = Assert.Single(page.Albums.Items);
        Assert.Equal(album.Id, row.Album.Id);
        Assert.Equal(cover, row.Cover);
        // By role then by sort key, an author credited with several roles once, at the place of their first role.
        Assert.Equal([franquin.Id, greg.Id], row.Authors.Select(a => a.Id));
        Assert.Equal((1966, 3), (row.FirstPublicationYear, row.FirstPublicationMonth));
        Assert.Equal(AlbumRating.VeryGood, row.Rating);
        Assert.Equal(AlbumCollectionState.InCollection, row.State);
    }

    [Fact]
    public async Task Possession_KeepsTheAlbumsOfTheStateChosen_AllWithoutIt()
    {
        var publisher = await CreatePublisherAsync("Casterman");
        var owned = await CreateAlbumAsync($"Possédé {_marker}");
        await CreateOwnedEditionAsync(owned.Id, publisher.Id, 1990);
        var intended = await CreateAlbumAsync($"Visé {_marker}");
        await CreateIntendedEditionAsync(intended.Id, publisher.Id, 1990);
        var outside = await CreateAlbumAsync($"Catalogué {_marker}");

        Assert.Equal([outside.Id, owned.Id, intended.Id], (await ListAsync($"q={_marker}")).Albums.Items.Select(r => r.Album.Id));
        Assert.Equal([owned.Id], (await ListAsync($"q={_marker}&state=InCollection")).Albums.Items.Select(r => r.Album.Id));
        Assert.Equal([intended.Id], (await ListAsync($"q={_marker}&state=PurchaseIntent")).Albums.Items.Select(r => r.Album.Id));
        Assert.Equal([outside.Id], (await ListAsync($"q={_marker}&state=OutsideCollection")).Albums.Items.Select(r => r.Album.Id));
    }

    [Fact]
    public async Task Cover_IsTakenAmongTheEditionsTheListPresents()
    {
        var publisher = await CreatePublisherAsync("Dargaud");
        var album = await CreateAlbumAsync($"Fort Navajo {_marker}");
        var owned = await CreateOwnedEditionAsync(album.Id, publisher.Id, 2010);
        var ownedCover = await UploadCoverAsync(album.Id, owned);
        // Older, but only targeted by an intent: the list of all the albums presents it, not that of the owned ones.
        var intended = await CreateIntendedEditionAsync(album.Id, publisher.Id, 1985);
        var intendedCover = await UploadCoverAsync(album.Id, intended);

        Assert.Equal(intendedCover, Assert.Single((await ListAsync($"q={_marker}")).Albums.Items).Cover);
        Assert.Equal(ownedCover, Assert.Single((await ListAsync($"q={_marker}&state=InCollection")).Albums.Items).Cover);
    }

    [Fact]
    public async Task Filters_AreNamedAsTheListsNameThem()
    {
        var series = await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Spirou {_marker}", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []));

        var page = await ListAsync($"seriesId={series.Id}");

        Assert.Equal(new SeriesListItem(series.Id, $"Spirou {_marker}"), page.Filters.Series);
    }

    private async Task<AlbumCatalogList> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<AlbumCatalogList>($"/catalog/albums?{query}"))!;

    private static string VisualsUri(Guid albumId, Guid editionId) => $"/admin/albums/{albumId}/editions/{editionId}/visuals";

    private Task<PublisherForm> CreatePublisherAsync(string name) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"{name} {Guid.NewGuid():N}", null, []));

    private Task<AuthorForm> CreateAuthorAsync(string pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(null, null, $"{pseudonym} {_marker}", null, null));

    private Task<AlbumForm> CreateAlbumAsync(
        string title, AlbumRating? rating = null, int? year = null, int? month = null, ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, year, month, null, null, rating, [], [], contributions ?? []));

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
        var before = (await _client.GetFromJsonAsync<EditionVisualsForm>(VisualsUri(albumId, editionId)))!.Visuals.Select(v => v.Id).ToHashSet();
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync(VisualsUri(albumId, editionId), form);
        response.EnsureSuccessStatusCode();
        var visual = (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals.Single(v => !before.Contains(v.Id));
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
