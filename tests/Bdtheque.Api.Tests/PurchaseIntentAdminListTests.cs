using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
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
public sealed class PurchaseIntentAdminListTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateApiClient();
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Row_OfAnIntentOnTheAlbum_CarriesTheAlbumAloneAndCanTargetAnEdition()
    {
        var album = await CreateAlbumAsync($"Blacksad {_marker}");
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(album.Id)));
        var intent = await SingleIntentAsync(album.Id);
        var version = await GetAlbumVersionAsync(album.Id);

        var row = Assert.Single(await ListAsync(_marker));

        // Without an owned edition, the album is wished for, never outside the collection
        // (fonctionnel/regles-metier/appartenance-a-la-collection.md#états-dun-album).
        Assert.Equal((intent, album.Id, AlbumCollectionState.PurchaseIntent), (row.Id, row.Album.Id, row.State));
        Assert.Null(row.Edition);
        // An album without an edition has no cover: the generic visual stands for it.
        Assert.Null(row.Cover);
        // An intent on the album always turns into one on an edition (fonctionnel/regles-metier/intention-d-acquisition.md).
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

        Assert.Equal(AlbumCollectionState.InCollection, row.State);
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

        Assert.Equal(AlbumCollectionState.PurchaseIntent, row.State);
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

    // The search of a record to modify finds the edition an intent targets by its ISBN, through the intent.
    [Fact]
    public async Task Search_FindsTheIsbnOfTheTargetedEdition()
    {
        var isbn = $"978{Random.Shared.NextInt64(1_000_000_000, 9_999_999_999)}";
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var album = await CreateAlbumAsync($"Spirou {_marker}");
        await CreateIntendedEditionAsync(album.Id, publisher.Id, 2001, isbn);
        var intent = await SingleIntentAsync(album.Id);

        Assert.Equal([intent], await IdsAsync($"{isbn[..3]} {isbn[3..]}"));
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

    [Fact]
    public async Task List_FollowsTheNavigationByInitialOfTheAlbums()
    {
        // An intent is filed under the entry of its album (visuel/listes/liste-par-liste.md), an album without a title of its
        // own under that of its series (modele-metier/album.md).
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var untitled = await CreateAlbumAsync(null, (await CreateSeriesAsync($"Blacksad {_marker}")).Id, 1);
        var lotus = await CreateAlbumAsync($"Le Lotus bleu {_marker}");
        var untitledIntent = await CreateAlbumIntentAsync(untitled.Id);
        await CreateIntendedEditionAsync(lotus.Id, publisher.Id);
        var lotusIntent = await SingleIntentAsync(lotus.Id);

        Assert.Equal([untitledIntent], await IdsAsync(_marker, "B"));
        Assert.Equal([lotusIntent], await IdsAsync(_marker, "L"));
        Assert.Empty(await IdsAsync(_marker, "@"));
    }

    [Fact]
    public async Task Entry_ThatIsNotOneOfTheNavigation_IsATechnicalError()
    {
        var response = await _client.GetAsync("/admin/purchase-intents?entry=AB");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private async Task<IReadOnlyList<PurchaseIntentAdminListItem>> ListAsync(string text, string? entry = null) =>
        (await GetAsync<Page<PurchaseIntentAdminListItem>>(
            $"/admin/purchase-intents?q={Uri.EscapeDataString(text)}{(entry is null ? "" : $"&entry={Uri.EscapeDataString(entry)}")}")).Items;

    private async Task<IReadOnlyList<Guid>> IdsAsync(string text, string? entry = null) => [.. (await ListAsync(text, entry)).Select(r => r.Id)];

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
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? year = null, string? isbn = null)
    {
        var form = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, year, isbn, null, null, null, null, null, null, true),
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
