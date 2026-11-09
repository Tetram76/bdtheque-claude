using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the editions of the administration (<c>GET /admin/editions</c>, visuel/listes/liste-par-liste.md): the editions
/// of the collection only, what each row presents, its search and filters, those of the list of the consultation, and the
/// records it is filtered on.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test narrows it to a publisher, an album or an ISBN
/// of its own.
/// </remarks>
public sealed class EditionAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public EditionAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var publisher = await CreatePublisherAsync($"Glénat {_marker}");
        var album = await CreateAlbumAsync($"Les Lunes de Saturne {_marker}");
        var edition = await CreateOwnedEditionAsync(album.Id, publisher.Id, Edition(publisher.Id) with
        {
            PublicationYear = 2004,
            Isbn = "978-2-7234-4567-1",
            Category = EditionCategory.FirstEdition,
            AcquisitionMode = AcquisitionMode.Gift,
            AcquisitionDate = new DateOnly(2005, 3, 1),
        });
        var cover = await UploadCoverAsync(album.Id, edition.Id);
        var version = await GetAlbumVersionAsync(album.Id);

        var row = Assert.Single((await ListAsync($"albumId={album.Id}")).Editions.Items);

        Assert.Equal((album.Id, $"Les Lunes de Saturne {_marker}", true), (row.Album.Id, row.Album.Title, row.Album.IsInCollection));
        Assert.Equal(
            new EditionSummary(edition.Id, publisher.Id, $"Glénat {_marker}", null, null, 2004, "978-2-7234-4567-1"),
            row.Edition);
        Assert.Equal(cover, row.Cover);
        Assert.Equal((EditionCategory?)EditionCategory.FirstEdition, row.Category);
        Assert.Equal((AcquisitionMode.Gift, (int?)2005), (row.AcquisitionMode, row.AcquisitionYear));
        // The version of the album, which the deletion of its edition sends back.
        Assert.Equal(version, row.AlbumVersion);
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_WithoutCoverNorCategoryNorAcquisitionDate_CarryNone()
    {
        var publisher = await CreatePublisherAsync($"Dupuis {_marker}");
        var album = await CreateAlbumAsync($"Buddy Longway {_marker}");
        await CreateOwnedEditionAsync(album.Id, publisher.Id, Edition(publisher.Id) with { AcquisitionMode = AcquisitionMode.Trade });

        var row = Assert.Single((await ListAsync($"albumId={album.Id}")).Editions.Items);

        Assert.Equal(((AlbumCover?)null, (EditionCategory?)null, AcquisitionMode.Trade, (int?)null),
            (row.Cover, row.Category, row.AcquisitionMode, row.AcquisitionYear));
    }

    [Fact]
    public async Task List_HoldsOnlyTheEditionsOfTheCollection_AnEditionTargetedByAnIntentBeingOneOfTheIntents()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var owned = await CreateAlbumAsync($"Tintin {_marker}");
        var kept = await CreateOwnedEditionAsync(owned.Id, publisher.Id, Edition(publisher.Id));
        await CreateIntendedEditionAsync(owned.Id, publisher.Id);
        var wanted = await CreateAlbumAsync($"Blacksad {_marker}");
        await CreateIntendedEditionAsync(wanted.Id, publisher.Id);

        Assert.Equal([kept.Id], await IdsAsync($"publisherId={publisher.Id}"));
    }

    [Fact]
    public async Task List_IsInTheOrderOfTheAlbums_ThenByYear()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}");
        var second = await CreateAlbumAsync($"Zorglub {_marker}");
        var first = await CreateAlbumAsync($"Le Lotus bleu {_marker}");
        var recent = await CreateOwnedEditionAsync(first.Id, publisher.Id, Edition(publisher.Id) with { PublicationYear = 2007 });
        var old = await CreateOwnedEditionAsync(first.Id, publisher.Id, Edition(publisher.Id) with { PublicationYear = 1963 });
        var other = await CreateOwnedEditionAsync(second.Id, publisher.Id, Edition(publisher.Id) with { PublicationYear = 1950 });

        // "Lotus bleu … [Le]" < "Zorglub …", by sort key; the editions of an album by year.
        Assert.Equal([old.Id, recent.Id, other.Id], await IdsAsync($"publisherId={publisher.Id}"));
    }

    [Fact]
    public async Task List_FollowsTheSearchAndTheFiltersOfTheConsultation()
    {
        var publisher = await CreatePublisherAsync($"Delcourt {_marker}");
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Encrages {_marker}", publisher.Version));
        var album = await CreateAlbumAsync($"Lanfeust {_marker}");
        var digits = Random.Shared.NextInt64(1_000_000_000, 10_000_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var found = await CreateOwnedEditionAsync(album.Id, publisher.Id, Edition(publisher.Id) with
        {
            PublisherCollectionId = collection.Id,
            Isbn = $"978-{digits[..1]}-{digits[1..5]}-{digits[5..]}",
        });
        await CreateOwnedEditionAsync(album.Id, publisher.Id, Edition(publisher.Id));
        await CreateIntendedEditionAsync((await CreateAlbumAsync($"Trolls {_marker}")).Id, publisher.Id, collection.Id);

        // An ISBN searched as printed, with or without its separators.
        Assert.Equal([found.Id], await IdsAsync($"q=978{digits}"));
        Assert.Equal([found.Id], await IdsAsync($"q={Uri.EscapeDataString($"978 {digits}")}"));
        Assert.Equal([found.Id], await IdsAsync($"publisherCollectionId={collection.Id}"));
        // The editions of the list of the consultation, but those of the collection only.
        foreach (var query in new[] { $"publisherId={publisher.Id}", $"albumId={album.Id}", $"publisherCollectionId={collection.Id}" })
            Assert.Equal(await CatalogOwnedIdsAsync(query), await IdsAsync(query));
    }

    [Fact]
    public async Task Filters_NameTheRecordsTheListIsFilteredOn()
    {
        var publisher = await CreatePublisherAsync($"Éditeur {_marker}", "https://editeur.example");
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection {_marker}", publisher.Version));
        var album = await CreateAlbumAsync($"Album {_marker}");

        var filters = (await ListAsync($"albumId={album.Id}&publisherId={publisher.Id}&publisherCollectionId={collection.Id}")).Filters;

        Assert.Equal(
            new EditionListFilters(
                new AlbumSummary(album.Id, $"Album {_marker}", null, null, AlbumType.Regular, false, null, null, null, false),
                new PublisherListItem(publisher.Id, $"Éditeur {_marker}", "https://editeur.example"),
                new PublisherCollectionListItem(collection.Id, $"Collection {_marker}", publisher.Id, $"Éditeur {_marker}")),
            filters);
    }

    [Fact]
    public async Task Filters_OnARecordThatDoesNotExist_NameNothingAndLeaveTheListEmpty()
    {
        var list = await ListAsync($"albumId={Guid.NewGuid()}");

        Assert.Empty(list.Editions.Items);
        Assert.Equal(new EditionListFilters(null, null, null), list.Filters);
    }

    private async Task<EditionAdminList> ListAsync(string query) => await GetAsync<EditionAdminList>($"/admin/editions?{query}");

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Editions.Items.Select(r => r.Edition.Id)];

    private async Task<IReadOnlyList<Guid>> CatalogOwnedIdsAsync(string query) =>
    [
        .. (await GetAsync<Page<EditionListItem>>($"/catalog/editions?{query}")).Items
            .Where(e => e.IsInCollection)
            .Select(e => e.Edition.Id),
    ];

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website = null) =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest(name, website));

    private static EditionContent Edition(Guid publisherId) =>
        new(publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);

    private async Task<EditionForm> CreateOwnedEditionAsync(Guid albumId, Guid publisherId, EditionContent content) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(content with { PublisherId = publisherId }, await GetAlbumVersionAsync(albumId)));

    private async Task CreateIntendedEditionAsync(Guid albumId, Guid publisherId, Guid? collectionId = null) =>
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, collectionId, null, null, null, null, null, null, null, null, true),
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
