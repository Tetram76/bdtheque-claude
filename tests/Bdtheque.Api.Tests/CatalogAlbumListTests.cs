using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the albums of the consultation (<c>GET /catalog/albums</c>): what each row presents, the filter on the
/// possession (fonctionnel/regles-metier/perimetre-de-la-consultation.md), and the cover of each album, taken among the
/// editions the filter shows (fonctionnel/regles-metier/couverture-d-un-album.md).
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class CatalogAlbumListTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public CatalogAlbumListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var writer = await CreateAuthorAsync("Goscinny", "René");
        var artist = await CreateAuthorAsync("Uderzo", "Albert");
        var publisher = await CreatePublisherAsync();
        // Credited twice, the writer is one of the authors once; by role, the writer comes before the artist.
        var album = await CreateAlbumAsync(
            $"Astérix le Gaulois {_marker}", 1961, 10,
            [new(artist.Id, ContributionRole.Illustrator), new(writer.Id, ContributionRole.Scenarist), new(writer.Id, ContributionRole.Colorist)]);
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var cover = await UploadCoverAsync(album.Id, edition);

        var row = Assert.Single(await ListAsync($"q={_marker}"));

        Assert.Equal(album.Id, row.Album.Id);
        Assert.Equal(cover, row.Cover);
        Assert.Equal([writer.Id, artist.Id], row.Authors.Select(a => a.Id));
        Assert.Equal(new AuthorListItem(writer.Id, $"Goscinny {_marker}", "René", null), row.Authors[0]);
        Assert.Equal((1961, 10), (row.FirstPublicationYear, row.FirstPublicationMonth));
        Assert.Equal(AlbumCollectionState.InCollection, row.State);
    }

    [Fact]
    public async Task Possession_KeepsTheAlbumsInTheStateChosen_AllOfThemWithout()
    {
        var publisher = await CreatePublisherAsync();
        var owned = await CreateAlbumAsync($"A possédé {_marker}");
        await CreateOwnedEditionAsync(owned.Id, publisher.Id);
        await CreateIntendedEditionAsync(owned.Id, publisher.Id);
        var intendedEdition = await CreateAlbumAsync($"B visé par une édition {_marker}");
        await CreateIntendedEditionAsync(intendedEdition.Id, publisher.Id);
        var intendedAlbum = await CreateAlbumAsync($"C visé en entier {_marker}");
        await CreateAlbumIntentAsync(intendedAlbum.Id);
        var outside = await CreateAlbumAsync($"D non possédé {_marker}");

        Assert.Equal(
            [
                (owned.Id, AlbumCollectionState.InCollection),
                (intendedEdition.Id, AlbumCollectionState.PurchaseIntent),
                (intendedAlbum.Id, AlbumCollectionState.PurchaseIntent),
                (outside.Id, AlbumCollectionState.OutsideCollection),
            ],
            (await ListAsync($"q={_marker}")).Select(r => (r.Album.Id, r.State)));
        Assert.Equal([owned.Id], await IdsAsync($"q={_marker}&state=InCollection"));
        Assert.Equal([intendedEdition.Id, intendedAlbum.Id], await IdsAsync($"q={_marker}&state=PurchaseIntent"));
        Assert.Equal([outside.Id], await IdsAsync($"q={_marker}&state=OutsideCollection"));
    }

    [Fact]
    public async Task Cover_IsTakenAmongTheEditionsTheFilterShows()
    {
        var publisher = await CreatePublisherAsync();
        // Owned, with an older edition still targeted by an intent: that one is the cover of the documentary base, the owned
        // one that of the collection.
        var owned = await CreateAlbumAsync($"A possédé {_marker}");
        var targeted = await CreateIntendedEditionAsync(owned.Id, publisher.Id, 1990);
        var targetedCover = await UploadCoverAsync(owned.Id, targeted);
        var ownedEdition = await CreateOwnedEditionAsync(owned.Id, publisher.Id, 2000);
        var ownedCover = await UploadCoverAsync(owned.Id, ownedEdition);
        // Targeted on one of its editions, which gives it its cover; targeted as a whole, it has none.
        var intendedEdition = await CreateAlbumAsync($"B visé par une édition {_marker}");
        var intendedCover = await UploadCoverAsync(intendedEdition.Id, await CreateIntendedEditionAsync(intendedEdition.Id, publisher.Id));
        var intendedAlbum = await CreateAlbumAsync($"C visé en entier {_marker}");
        await CreateAlbumIntentAsync(intendedAlbum.Id);

        Assert.Equal(
            [(owned.Id, targetedCover), (intendedEdition.Id, intendedCover), (intendedAlbum.Id, null)],
            (await ListAsync($"q={_marker}")).Select(r => (r.Album.Id, r.Cover)));
        Assert.Equal(ownedCover, Assert.Single(await ListAsync($"q={_marker}&state=InCollection")).Cover);
        Assert.Equal(
            [(intendedEdition.Id, intendedCover), (intendedAlbum.Id, null)],
            (await ListAsync($"q={_marker}&state=PurchaseIntent")).Select(r => (r.Album.Id, r.Cover)));
    }

    [Fact]
    public async Task Possession_ThatIsNotOneOfTheStates_IsATechnicalError()
    {
        var response = await _client.GetAsync("/catalog/albums?state=Owned");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, ProblemTypes.Technical);
    }

    private async Task<IReadOnlyList<AlbumListItem>> ListAsync(string query) =>
        (await GetAsync<Page<AlbumListItem>>($"/catalog/albums?{query}")).Items;

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Select(r => r.Album.Id)];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<AlbumForm> CreateAlbumAsync(
        string title, int? firstPublicationYear = null, int? firstPublicationMonth = null, ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, firstPublicationYear, firstPublicationMonth,
            null, null, null, [], [], contributions ?? []));

    private Task<AuthorForm> CreateAuthorAsync(string lastName, string firstName) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"{lastName} {_marker}", firstName, null, null, null));

    private Task<PublisherForm> CreatePublisherAsync() =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {_marker}", null, []));

    /// <returns>The identifier of the owned edition.</returns>
    private async Task<Guid> CreateOwnedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear = null) =>
        (await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, publicationYear, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)))).Id;

    /// <returns>The identifier of the edition the intent targets, not owned.</returns>
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? publicationYear = null) =>
        (await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, publicationYear, null, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)))).Intents.Single(i => i.EditionId is not null).EditionId!.Value;

    private async Task CreateAlbumIntentAsync(Guid albumId) =>
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(albumId)));

    private async Task<AlbumCover> UploadCoverAsync(Guid albumId, Guid editionId)
    {
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync($"/admin/albums/{albumId}/editions/{editionId}/visuals", form);
        response.EnsureSuccessStatusCode();
        return new AlbumCover(Assert.Single((await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals).Id, TestImages.UploadedPalette);
    }

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) => (await GetAsync<AlbumForm>($"/admin/albums/{albumId}")).Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
