using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the publishers of the administration (<c>GET /admin/publishers</c>, visuel/listes/liste-par-liste.md): what
/// each row presents, its search on the name and its order.
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its
/// own, and narrows the list to it by searching it.
/// </remarks>
public sealed class PublisherAdminListTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public PublisherAdminListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Rows_CarryWhatTheListPresents()
    {
        var publisher = await CreatePublisherAsync($"Dargaud {_marker}", "https://www.dargaud.com");
        await AddCollectionAsync(publisher.Id, "Poisson Pilote");
        await AddCollectionAsync(publisher.Id, "Long courrier");
        var form = await GetAsync<PublisherForm>($"/admin/publishers/{publisher.Id}");

        var row = Assert.Single(await ListAsync($"q={_marker}"));

        Assert.Equal(
            (publisher.Id, $"Dargaud {_marker}", "https://www.dargaud.com", form.Version),
            (row.Id, row.Name, row.Website, row.Version));
        // Its collections in their order, by name.
        Assert.Equal(["Long courrier", "Poisson Pilote"], row.Collections);
        Assert.NotEqual(default, row.ModifiedAt);
    }

    [Fact]
    public async Task Rows_CountTheEditionsInTheCollection_AnEditionTargetedByAnIntentBeingNone()
    {
        var publisher = await CreatePublisherAsync($"Casterman {_marker}");
        var other = await CreatePublisherAsync($"Dupuis {_marker}");
        var album = await CreateAlbumAsync($"Album {_marker}");
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        await CreateOwnedEditionAsync(album.Id, other.Id);
        var intended = await CreateAlbumAsync($"Album visé {_marker}");
        await CreateIntendedEditionAsync(intended.Id, publisher.Id);
        var empty = await CreatePublisherAsync($"Futuropolis {_marker}");

        var rows = await ListAsync($"q={_marker}");

        Assert.Equal(
            [(publisher.Id, 2, 0), (other.Id, 1, 0), (empty.Id, 0, 0)],
            rows.Select(r => (r.Id, r.EditionCount, r.Collections.Count)));
    }

    [Fact]
    public async Task List_SearchesTheName_InAnyCaseAndAccents_AndIsOrderedByName()
    {
        var glenat = await CreatePublisherAsync($"Glénat {_marker}");
        var delcourt = await CreatePublisherAsync($"Delcourt {_marker}");
        var lombard = await CreatePublisherAsync($"Le Lombard {_marker}");

        Assert.Equal([delcourt.Id, glenat.Id, lombard.Id], (await ListAsync($"q={_marker}")).Select(r => r.Id));
        Assert.Equal([glenat.Id], (await ListAsync($"q=GLENAT {_marker}")).Select(r => r.Id));
    }

    private async Task<IReadOnlyList<PublisherAdminListItem>> ListAsync(string query) =>
        (await GetAsync<Page<PublisherAdminListItem>>($"/admin/publishers?{query}")).Items;

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private Task<PublisherForm> CreatePublisherAsync(string name, string? website = null) =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent(name, website, []));

    private async Task AddCollectionAsync(Guid publisherId, string name)
    {
        var publisher = await GetAsync<PublisherForm>($"/admin/publishers/{publisherId}");
        await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisherId}/collections", new CreatePublisherCollectionRequest(name, publisher.Version));
    }

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task CreateOwnedEditionAsync(Guid albumId, Guid publisherId) =>
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

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) => (await GetAsync<AlbumForm>($"/admin/albums/{albumId}")).Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
