using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the purchase intents of the administration (<c>GET /admin/purchase-intents</c>, visuel/listes/liste-par-liste.md):
/// what each row presents, its search on the title of the album or of its series, and its order, that of the albums.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test narrows it by searching a marker of its own,
/// which the titles of its albums or series carry.
/// </remarks>
public sealed class PurchaseIntentAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public PurchaseIntentAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Row_OfAnIntentOnTheAlbum_CarriesTheAlbumAloneAndCanTargetAnEdition()
    {
        var album = await CreateAlbumAsync($"Blacksad {_marker}");
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(album.Id)));
        var intent = await SingleIntentAsync(album.Id);
        var version = await GetAlbumVersionAsync(album.Id);

        var row = Assert.Single(await ListAsync(_marker));

        Assert.Equal((intent, album.Id, false), (row.Id, row.Album.Id, row.Album.IsInCollection));
        Assert.Null(row.Edition);
        // An album without an edition has no cover: the generic visual stands for it.
        Assert.Null(row.Cover);
        // An intent on the album always turns into one on an edition (fonctionnel/regles-metier/intention-d-achat.md).
        Assert.True(row.IsConvertible);
        // The version of the album, which the removal of its intent sends back.
        Assert.Equal(version, row.AlbumVersion);
        Assert.NotEqual(default, row.CreatedAt);
    }

    [Fact]
    public async Task Row_OfAnIntentOnAnEdition_CarriesTheEditionAndTheCoverOfTheAlbum()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var album = await CreateAlbumAsync($"Tintin {_marker}");
        var owned = await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var cover = await UploadCoverAsync(album.Id, owned.Id);
        var targeted = await CreateIntendedEditionAsync(album.Id, publisher.Id, 2013);

        var row = Assert.Single(await ListAsync(_marker));

        Assert.True(row.Album.IsInCollection);
        Assert.Equal(new EditionSummary(targeted, publisher.Id, $"Casterman {_marker}", null, null, 2013, null), row.Edition);
        Assert.Equal(cover, row.Cover);
        // The album has another edition: the intent cannot turn into one on the whole album.
        Assert.False(row.IsConvertible);
    }

    [Fact]
    public async Task Row_OfAnIntentOnTheOnlyEditionOfItsAlbum_CanTargetTheAlbum()
    {
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await CreateIntendedEditionAsync(album.Id, publisher.Id);

        var row = Assert.Single(await ListAsync(_marker));

        Assert.False(row.Album.IsInCollection);
        Assert.True(row.IsConvertible);
    }

    [Fact]
    public async Task Search_FindsTheTitleOfTheAlbumOrOfItsSeries()
    {
        var series = await CreateSeriesAsync($"Le Château des étoiles {_marker}");
        var inSeries = await CreateAlbumAsync(null, series.Id, 4);
        var titled = await CreateAlbumAsync($"Les Vieux Fourneaux {_marker}");
        var intendedInSeries = await CreateAlbumIntentAsync(inSeries.Id);
        var intendedTitled = await CreateAlbumIntentAsync(titled.Id);

        Assert.Equal([intendedInSeries], await IdsAsync($"Château des étoiles {_marker}"));
        Assert.Equal([intendedTitled], await IdsAsync($"vieux fourneaux {_marker}"));
    }

    [Fact]
    public async Task List_IsInTheOrderOfTheAlbums()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var second = await CreateAlbumAsync($"Zorglub {_marker}");
        var first = await CreateAlbumAsync($"Le Lotus bleu {_marker}");
        var last = await CreateAlbumIntentAsync(second.Id);
        await CreateIntendedEditionAsync(first.Id, publisher.Id);
        var firstIntent = await SingleIntentAsync(first.Id);

        // "Lotus bleu … [Le]" < "Zorglub …", by sort key.
        Assert.Equal([firstIntent, last], await IdsAsync(_marker));
    }

    private async Task<IReadOnlyList<PurchaseIntentAdminListItem>> ListAsync(string text) =>
        (await GetAsync<Page<PurchaseIntentAdminListItem>>($"/admin/purchase-intents?q={Uri.EscapeDataString(text)}")).Items;

    private async Task<IReadOnlyList<Guid>> IdsAsync(string text) => [.. (await ListAsync(text)).Select(r => r.Id)];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId = null, int? volume = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volume, null, null, null, null, null, null, null, [], [], []));

    private Task<SeriesForm> CreateSeriesAsync(string title) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            title, null, null, null, false, false, false, null, null, new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null),
            [], [], []));

    private Task<PublisherForm> CreatePublisherAsync(string name) => PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, null, []));

    private async Task<EditionForm> CreateOwnedEditionAsync(Guid albumId, Guid publisherId) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    private async Task<Guid> CreateAlbumIntentAsync(Guid albumId)
    {
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(albumId)));
        return await SingleIntentAsync(albumId);
    }

    // The edition targeted by the new intent: the one the intents of the album did not target yet.
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? year = null)
    {
        var form = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, year, null, null, null, null, null, null, null, true),
                await GetAlbumVersionAsync(albumId)));
        return Assert.Single(form.Intents).EditionId!.Value;
    }

    private async Task<Guid> SingleIntentAsync(Guid albumId) =>
        Assert.Single((await GetAsync<PurchaseIntentsForm>($"/admin/albums/{albumId}/purchase-intents")).Intents).Id;

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
