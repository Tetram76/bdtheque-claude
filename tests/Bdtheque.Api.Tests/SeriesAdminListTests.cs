using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the series of the administration (<c>GET /admin/series</c>, visuel/listes/liste-par-liste.md): what each row
/// presents, its search, navigation and filters, those of the list of the consultation, and the records it is filtered on.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class SeriesAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public SeriesAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var series = await CreateSeriesAsync($"Barracuda {_marker}", SeriesStatus.Completed, isComplete: true);
        var first = await CreateAlbumAsync(null, series.Id, 1);
        var edition = await CreateOwnedEditionAsync(first.Id, publisher.Id);
        var cover = await UploadCoverAsync(first.Id, edition.Id);
        await CreateAlbumAsync(null, series.Id, 2);
        var form = await GetAsync<SeriesForm>($"/admin/series/{series.Id}");

        var row = Assert.Single((await ListAsync($"q={_marker}")).Series.Items);

        Assert.Equal(series.Id, row.Id);
        Assert.Equal($"Barracuda {_marker}", row.Title);
        Assert.Equal(SeriesStatus.Completed, row.Status);
        Assert.True(row.IsComplete);
        // The pile of covers and the number of albums are those of the consultation (SeriesOverview).
        Assert.Equal(2, row.Overview.AlbumCount);
        Assert.Equal([cover, null], row.Overview.FirstVolumeCovers);
        Assert.Equal(form.Version, row.Version);
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_CountTheAlbumsInTheCollection_AnAlbumOnlyIntendedBeingNone()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var series = await CreateSeriesAsync($"Tintin {_marker}");
        var owned = await CreateAlbumAsync(null, series.Id, 1);
        // Two owned editions make one album of the collection.
        await CreateOwnedEditionAsync(owned.Id, publisher.Id);
        await CreateOwnedEditionAsync(owned.Id, publisher.Id);
        var intended = await CreateAlbumAsync(null, series.Id, 2);
        await CreateIntendedEditionAsync(intended.Id, publisher.Id);
        await CreateAlbumAsync(null, series.Id, 3);
        var empty = await CreateSeriesAsync($"Zorro, série sans album {_marker}");

        var rows = (await ListAsync($"q={_marker}")).Series.Items;

        Assert.Equal(
            [(series.Id, 3, 1, (SeriesStatus?)null, false), (empty.Id, 0, 0, null, false)],
            rows.Select(r => (r.Id, r.Overview.AlbumCount, r.AlbumsInCollectionCount, r.Status, r.IsComplete)));
    }

    [Fact]
    public async Task List_FollowsTheSearchTheNavigationAndTheFiltersOfTheConsultation()
    {
        var author = await CreateAuthorAsync($"Auteur {_marker}");
        var lotus = await CreateSeriesAsync($"Le Lotus bleu {_marker}", contributions: [new(author.Id, ContributionRole.Scenarist)]);
        var zorglub = await CreateSeriesAsync($"Zorglub {_marker}");
        await CreateAlbumAsync(null, zorglub.Id, 1, [new(author.Id, ContributionRole.Illustrator)]);
        var other = await CreateSeriesAsync($"Autre {_marker}");

        // "Autre …" < "Lotus bleu … [Le]" < "Zorglub …", by sort key.
        Assert.Equal([other.Id, lotus.Id, zorglub.Id], await IdsAsync($"q={_marker}"));
        Assert.Equal([lotus.Id], await IdsAsync($"q={_marker}&entry=L"));
        // An author credited on the series itself, or on one of its albums, as in the list of the consultation.
        Assert.Equal([lotus.Id, zorglub.Id], await IdsAsync($"q={_marker}&authorId={author.Id}"));
        Assert.Equal(await CatalogIdsAsync($"q={_marker}&authorId={author.Id}"), await IdsAsync($"q={_marker}&authorId={author.Id}"));
    }

    [Fact]
    public async Task Filters_NameTheRecordsTheListIsFilteredOn()
    {
        var author = await CreateAuthorAsync($"Auteur {_marker}");
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}", "https://editeur.example");
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection {_marker}", publisher.Version));
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        var parent = await PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"Parent {_marker}", null, null));
        var universe = await PostAsync<UniverseForm>("/admin/universes", new CreateUniverseRequest($"Univers {_marker}", null, parent.Id));

        var filters = (await ListAsync(
            $"authorId={author.Id}&publisherId={publisher.Id}&publisherCollectionId={collection.Id}&genreId={genre.Id}&universeId={universe.Id}")).Filters;

        Assert.Equal(
            new SeriesListFilters(
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
        var list = await ListAsync($"authorId={Guid.NewGuid()}");

        Assert.Empty(list.Series.Items);
        Assert.Equal(new SeriesListFilters(null, null, null, null, null), list.Filters);
    }

    [Fact]
    public async Task Entry_ThatIsNotOneOfTheNavigation_IsATechnicalError()
    {
        var response = await _client.GetAsync("/admin/series?entry=AB");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<SeriesAdminList> ListAsync(string query) => await GetAsync<SeriesAdminList>($"/admin/series?{query}");

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Series.Items.Select(r => r.Id)];

    private async Task<IReadOnlyList<Guid>> CatalogIdsAsync(string query) =>
        [.. (await GetAsync<Page<SeriesListItem>>($"/catalog/series?{query}")).Items.Select(s => s.Id)];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<SeriesForm> CreateSeriesAsync(
        string title, SeriesStatus? status = null, bool isComplete = false, ContributionContent[]? contributions = null) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, status, null, isComplete, false, false, null, null, NoTemplate, [], [], contributions ?? []));

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid seriesId, int volumeNumber, ContributionContent[]? contributions = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volumeNumber, null, null, null, null, null, null, null, [], [],
            contributions ?? []));

    private Task<AuthorForm> CreateAuthorAsync(string pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(null, null, pseudonym, null, null));

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website = null) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, website, []));

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
