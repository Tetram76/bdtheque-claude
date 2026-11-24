using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The list of the authors of the consultation (<c>/catalog/authors</c>): what a row carries, and the filter of the possession,
/// by which it limits itself to the collection, an author being kept when one of the albums it is credited on is in the state
/// chosen (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
/// <remarks>
/// The list covers the whole base, shared by the tests of the class: each test names its records with a marker of its own, and
/// narrows the list to it by searching it.
/// </remarks>
public sealed class CatalogAuthorListTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly SeriesEditionTemplate NoTemplate = new(null, null, null, null, null, null, null, null, null);

    private readonly HttpClient _client;
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    public CatalogAuthorListTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Row_CarriesTheIdentityTheNationalityTheAlbumsCreditedThoseInTheCollectionAndTheRoles()
    {
        var publisher = await CreatePublisherAsync();
        var author = await PostAsync<AuthorForm>(
            "/admin/authors", new CreateAuthorRequest($"Gotlieb {_marker}", "Marcel", "Gotlib", null, "Française"));
        // Two roles on one album make one album credited; the roles held on a series count as well.
        var series = await CreateSeriesAsync([new(author.Id, ContributionRole.Colorist)]);
        var owned = await CreateAlbumAsync(series.Id, [new(author.Id, ContributionRole.Illustrator), new(author.Id, ContributionRole.Scenarist)]);
        await CreateOwnedEditionAsync(owned.Id, publisher.Id);
        await CreateAlbumAsync(null, [new(author.Id, ContributionRole.Illustrator)]);

        var row = Assert.Single(await ListAsync($"q={_marker}"));

        Assert.Equal(
            (author.Id, $"Gotlieb {_marker}", "Marcel", "Gotlib", "Française", 2, 1),
            (row.Id, row.LastName, row.FirstName, row.Pseudonym, row.Nationality, row.CreditedAlbumCount, row.CreditedAlbumsInCollectionCount));
        Assert.Equal([ContributionRole.Scenarist, ContributionRole.Illustrator, ContributionRole.Colorist], row.Roles);
    }

    [Fact]
    public async Task Possession_KeepsTheAuthorsCreditedOnAnAlbumInTheStateChosen_AllWithoutIt()
    {
        var publisher = await CreatePublisherAsync();
        // An owned album and another, neither owned nor targeted: under both states.
        var owner = await CreateAuthorAsync("A");
        var ownedAlbum = await CreateAlbumAsync(null, [new(owner.Id, ContributionRole.Scenarist)]);
        await CreateOwnedEditionAsync(ownedAlbum.Id, publisher.Id);
        await CreateAlbumAsync(null, [new(owner.Id, ContributionRole.Scenarist)]);
        // An album targeted by an intent.
        var intender = await CreateAuthorAsync("B");
        var intendedAlbum = await CreateAlbumAsync(null, [new(intender.Id, ContributionRole.Scenarist)]);
        await CreateIntentAsync(intendedAlbum.Id);
        // Credited on a series alone, whose album is owned, or on nothing: no album, under no state.
        var seriesOnly = await CreateAuthorAsync("D");
        var series = await CreateSeriesAsync([new(seriesOnly.Id, ContributionRole.Scenarist)]);
        var ownedInSeries = await CreateAlbumAsync(series.Id, []);
        await CreateOwnedEditionAsync(ownedInSeries.Id, publisher.Id);
        var uncredited = await CreateAuthorAsync("E");

        Assert.Equal([owner.Id, intender.Id, seriesOnly.Id, uncredited.Id], await IdsAsync($"q={_marker}"));
        Assert.Equal([owner.Id], await IdsAsync($"q={_marker}&state=InCollection"));
        Assert.Equal([intender.Id], await IdsAsync($"q={_marker}&state=PurchaseIntent"));
        Assert.Equal([owner.Id], await IdsAsync($"q={_marker}&state=OutsideCollection"));
    }

    [Fact]
    public async Task Possession_KeepsTheSearchAndTheNavigation()
    {
        var publisher = await CreatePublisherAsync();
        var vanHamme = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest($"Van Hamme {_marker}", "Jean", null, null, null));
        var album = await CreateAlbumAsync(null, [new(vanHamme.Id, ContributionRole.Scenarist)]);
        await CreateOwnedEditionAsync(album.Id, publisher.Id);
        var other = await CreateAuthorAsync("Moebius");
        var otherAlbum = await CreateAlbumAsync(null, [new(other.Id, ContributionRole.Illustrator)]);
        await CreateOwnedEditionAsync(otherAlbum.Id, publisher.Id);

        Assert.Equal([vanHamme.Id], await IdsAsync($"q={_marker}&entry=V&state=InCollection"));
        Assert.Equal([other.Id, vanHamme.Id], await IdsAsync($"q={_marker}&state=InCollection"));
    }

    private async Task<IReadOnlyList<AuthorCatalogListItem>> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<Page<AuthorCatalogListItem>>($"/catalog/authors?{query}"))!.Items;

    private async Task<IReadOnlyList<Guid>> IdsAsync(string query) => [.. (await ListAsync(query)).Select(r => r.Id)];

    private Task<AuthorForm> CreateAuthorAsync(string pseudonym) =>
        PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest(null, null, $"{pseudonym} {_marker}", null, null));

    private Task<PublisherForm> CreatePublisherAsync() =>
        PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {Guid.NewGuid():N}", null, []));

    private Task<SeriesForm> CreateSeriesAsync(ContributionContent[] contributions) =>
        PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Série {Guid.NewGuid():N}", null, null, null, false, false, false, null, null, NoTemplate, [], [], contributions));

    private Task<AlbumForm> CreateAlbumAsync(Guid? seriesId, ContributionContent[] contributions) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            seriesId is null ? $"Album {Guid.NewGuid():N}" : null, null, seriesId, AlbumType.Regular, false, seriesId is null ? null : 1, null,
            null, null, null, null, null, null, [], [], contributions));

    private async Task CreateOwnedEditionAsync(Guid albumId, Guid publisherId) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(
                    publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    // An intent on the album itself, any edition.
    private async Task CreateIntentAsync(Guid albumId) =>
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(albumId)));

    private async Task<uint> GetAlbumVersionAsync(Guid albumId) =>
        (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{albumId}"))!.Version;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
