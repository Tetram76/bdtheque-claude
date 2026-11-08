using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the albums of the administration (<c>GET /admin/albums</c>, visuel/liste-albums/contenu.md): what each
/// row presents, where each album stands with regard to the collection, and the records the list is filtered on.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class AlbumAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public AlbumAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var writer = await CreateAuthorAsync("Goscinny", "René");
        var artist = await CreateAuthorAsync("Uderzo", "Albert");
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        // Credited twice, the writer is one of the authors once; by role, the writer comes before the artist.
        var album = await CreateAlbumAsync(
            $"Astérix le Gaulois {_marker}", firstPublicationYear: 1961, firstPublicationMonth: 10,
            contributions: [new(artist.Id, ContributionRole.Illustrator), new(writer.Id, ContributionRole.Scenarist), new(writer.Id, ContributionRole.Colorist)]);
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var cover = await UploadCoverAsync(album.Id, edition.Id);
        var form = await GetAsync<AlbumForm>($"/admin/albums/{album.Id}");

        var row = Assert.Single((await ListAsync($"q={_marker}")).Albums.Items);

        Assert.Equal(album.Id, row.Album.Id);
        Assert.Equal(cover, row.Cover);
        Assert.Equal([writer.Id, artist.Id], row.Authors.Select(a => a.Id));
        Assert.Equal(new AuthorListItem(writer.Id, $"Goscinny {_marker}", "René", null), row.Authors[0]);
        Assert.Equal((1961, 10), (row.FirstPublicationYear, row.FirstPublicationMonth));
        Assert.Equal(form.Version, row.Version);
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_CountTheOwnedEditionsAndThePurchaseIntentsApart_AndTellWhereEachAlbumStands()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var owned = await CreateAlbumAsync($"A possédé {_marker}");
        await CreateOwnedEditionAsync(owned.Id, publisher.Id);
        await CreateOwnedEditionAsync(owned.Id, publisher.Id);
        await CreateIntendedEditionAsync(owned.Id, publisher.Id);
        var intendedEdition = await CreateAlbumAsync($"B visé par une édition {_marker}");
        await CreateIntendedEditionAsync(intendedEdition.Id, publisher.Id);
        await CreateIntendedEditionAsync(intendedEdition.Id, publisher.Id);
        var intendedAlbum = await CreateAlbumAsync($"C visé en entier {_marker}");
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{intendedAlbum.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, intendedAlbum.Version));
        var outside = await CreateAlbumAsync($"D hors collection {_marker}");

        var rows = (await ListAsync($"q={_marker}")).Albums.Items;

        Assert.Equal(
            [
                (owned.Id, 2, 1, AlbumCollectionState.InCollection),
                (intendedEdition.Id, 0, 2, AlbumCollectionState.PurchaseIntent),
                (intendedAlbum.Id, 0, 1, AlbumCollectionState.PurchaseIntent),
                (outside.Id, 0, 0, AlbumCollectionState.OutsideCollection),
            ],
            rows.Select(r => (r.Album.Id, r.EditionCount, r.PurchaseIntentCount, r.State)));
        Assert.Equal([owned.Id], await IdsAsync($"q={_marker}&state=InCollection"));
        Assert.Equal([intendedEdition.Id, intendedAlbum.Id], await IdsAsync($"q={_marker}&state=PurchaseIntent"));
        Assert.Equal([outside.Id], await IdsAsync($"q={_marker}&state=OutsideCollection"));
    }

    [Fact]
    public async Task List_FollowsTheSearchTheNavigationAndTheFiltersOfTheConsultation()
    {
        var series = await CreateSeriesAsync($"Les Aventures de Tintin {_marker}");
        var untitled = await CreateAlbumAsync(null, series.Id, 1);
        var lotus = await CreateAlbumAsync($"Le Lotus bleu {_marker}", series.Id, 5);
        var zorglub = await CreateAlbumAsync($"Zorglub {_marker}");

        // "Aventures de Tintin … [Les]" < "Lotus bleu … [Le]" < "Zorglub …".
        Assert.Equal([untitled.Id, lotus.Id, zorglub.Id], await IdsAsync($"q={_marker}"));
        Assert.Equal([lotus.Id], await IdsAsync($"q={_marker}&entry=L"));
        Assert.Equal([untitled.Id, lotus.Id], await IdsAsync($"q={_marker}&seriesId={series.Id}"));
    }

    [Fact]
    public async Task Filters_NameTheRecordsTheListIsFilteredOn()
    {
        var series = await CreateSeriesAsync($"Série {_marker}");
        var author = await CreateAuthorAsync(null, null, $"Auteur {_marker}");
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}", "https://editeur.example");
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection {_marker}", publisher.Version));
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        var parent = await PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"Parent {_marker}", null, null));
        var universe = await PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"Univers {_marker}", null, parent.Id));

        var filters = (await ListAsync(
            $"seriesId={series.Id}&authorId={author.Id}&publisherId={publisher.Id}&publisherCollectionId={collection.Id}" +
            $"&genreId={genre.Id}&universeId={universe.Id}")).Filters;

        Assert.Equal(
            new AlbumListFilters(
                new SeriesListItem(series.Id, $"Série {_marker}"),
                new AuthorListItem(author.Id, null, null, $"Auteur {_marker}"),
                new PublisherListItem(publisher.Id, $"Éditeur {_marker}", "https://editeur.example"),
                new PublisherCollectionListItem(collection.Id, $"Collection {_marker}", publisher.Id, $"Éditeur {_marker}"),
                new GenreListItem(genre.Id, $"Genre {_marker}"),
                new UniverseListItem(universe.Id, $"Univers {_marker}", parent.Id, $"Parent {_marker}")),
            filters);
    }

    [Fact]
    public async Task Filters_OnARecordThatDoesNotExist_NameNothingAndLeaveTheListEmpty()
    {
        var list = await ListAsync($"seriesId={Guid.NewGuid()}");

        Assert.Empty(list.Albums.Items);
        Assert.Equal(new AlbumListFilters(null, null, null, null, null, null), list.Filters);
    }

    [Fact]
    public async Task State_ThatIsNotOneOfTheStates_IsATechnicalError()
    {
        var response = await _client.GetAsync("/admin/albums?state=Owned");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.BadRequest, ProblemTypes.Technical);
    }

    private async Task<AlbumAdminList> ListAsync(string query) => await GetAsync<AlbumAdminList>($"/admin/albums?{query}");

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) =>
        [.. (await ListAsync(query)).Albums.Items.Select(r => r.Album.Id)];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, NoTemplate, [], [], []));

    private Task<AlbumForm> CreateAlbumAsync(
        string? title, Guid? seriesId = null, int? volumeNumber = null, int? firstPublicationYear = null,
        int? firstPublicationMonth = null, ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volumeNumber, null, null, firstPublicationYear, firstPublicationMonth,
            null, null, null, [], [], contributions ?? []));

    private Task<AuthorForm> CreateAuthorAsync(string? lastName, string? firstName, string? pseudonym = null) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(
            lastName is null ? null : $"{lastName} {_marker}", firstName, pseudonym, null, null));

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website = null) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest(name, website));

    private async Task<EditionForm> CreateOwnedEditionAsync(Guid albumId, Guid publisherId) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    private async Task CreateIntendedEditionAsync(Guid albumId, Guid publisherId) =>
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, null, null, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));

    private async Task<AlbumCover> UploadCoverAsync(Guid albumId, Guid editionId)
    {
        var uri = $"/admin/albums/{albumId}/editions/{editionId}/visuals";
        var form = new MultipartFormDataContent
        {
            { new ByteArrayContent(TestImages.Encode(8, 8, SKEncodedImageFormat.Jpeg)), UploadVisualFields.File, "visual.jpg" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent((await GetAlbumVersionAsync(albumId)).ToString()), UploadVisualFields.AlbumVersion },
        };
        var response = await _client.PostAsync(uri, form);
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
