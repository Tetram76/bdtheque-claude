using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The page of the editions and purchase intents of an album (<c>GET /admin/albums/{id}/editions-and-purchase-intents</c>,
/// visuel/editions-album/contenu.md): the album in brief, its editions of the collection, and its intents, an edition targeted
/// by an intent being one of the intents, never one of the editions (fonctionnel/regles-metier/intention-d-achat.md).
/// </summary>
public sealed class AlbumEditionsPageTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AlbumEditionsPageTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Page_OfAnAlbumWithoutEditionNorIntent_HasNeither()
    {
        var album = await CreateAlbumAsync("Le Château des étoiles");

        var page = await GetPageAsync(album.Id);

        Assert.Equal((album.Id, AlbumCollectionState.OutsideCollection), (page.Album.Album.Id, page.Album.State));
        Assert.Empty(page.Editions);
        Assert.Empty(page.PurchaseIntents);
        // The version of the album, which any deletion of its editions or intents sends back.
        Assert.Equal(album.Version, page.Album.Version);
        Assert.Equal(album.CreatedAt, page.CreatedAt);
        Assert.Equal(album.ModifiedAt, page.Album.ModifiedAt);
    }

    [Fact]
    public async Task Page_CarriesTheAlbumInBrief()
    {
        var author = await PostAsync<AuthorForm>("/admin/authors", new CreateAuthorRequest("Alice", "Alex", null, null, null));
        var album = await PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            "Les Bijoux de la Castafiore", null, null, AlbumType.Regular, false, 21, null, null, 1963, 4, null, null, null,
            [], [],
            [new ContributionContent(author.Id, ContributionRole.Scenarist), new ContributionContent(author.Id, ContributionRole.Illustrator)]));

        var page = await GetPageAsync(album.Id);

        Assert.Equal(("Les Bijoux de la Castafiore", 1963, 4), (page.Album.Album.Title, page.Album.FirstPublicationYear, page.Album.FirstPublicationMonth));
        // An author credited with several roles is one of the authors once.
        Assert.Equal([author.Id], page.Album.Authors.Select(a => a.Id));
    }

    [Fact]
    public async Task Editions_AreTheOwnedOnesByYear_AndTheTargetedOneIsAnIntent()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Casterman {Guid.NewGuid():N}", null, []));
        var album = await CreateAlbumAsync("Les Bijoux de la Castafiore");
        var recent = await CreateOwnedEditionAsync(album.Id, publisher.Id, 2007, AcquisitionMode.Gift, null, null, null, null);
        var original = await CreateOwnedEditionAsync(
            album.Id, publisher.Id, 1963, AcquisitionMode.Purchase, new DateOnly(1987, 5, 2), EditionCategory.FirstEdition,
            BindingType.Hardcover, 62);
        var cover = await UploadCoverAsync(album.Id, original.Id);
        var targeted = await CreateIntendedEditionAsync(album.Id, publisher.Id, 2013, EditionCategory.SpecialEdition, BindingType.Hardcover);

        var page = await GetPageAsync(album.Id);

        Assert.Equal(AlbumCollectionState.InCollection, page.Album.State);
        Assert.Equal([original.Id, recent.Id], page.Editions.Select(e => e.Edition.Id));
        var first = page.Editions[0];
        Assert.Equal(
            (cover, EditionCategory.FirstEdition, BindingType.Hardcover, 62, AcquisitionMode.Purchase, 1987, 1),
            (first.Cover, first.Category, first.Binding, first.PageCount, first.AcquisitionMode, first.AcquisitionYear, first.VisualCount));
        var second = page.Editions[1];
        Assert.Equal(
            ((AlbumCover?)null, (EditionCategory?)null, (int?)null, AcquisitionMode.Gift, 0),
            (second.Cover, second.Category, second.AcquisitionYear, second.AcquisitionMode, second.VisualCount));

        var intent = Assert.Single(page.PurchaseIntents);
        Assert.Equal(targeted, intent.Edition?.Id);
        Assert.Equal((EditionCategory.SpecialEdition, BindingType.Hardcover), (intent.Category, intent.Binding));
        // The album has other editions: the intent cannot turn into one on the whole album.
        Assert.False(intent.IsConvertible);
        Assert.NotEqual(default, intent.CreatedAt);
    }

    [Fact]
    public async Task Intent_OnTheAlbum_TargetsNoEditionAndCanTargetOne()
    {
        var album = await CreateAlbumAsync("Blacksad");
        await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{album.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, await GetAlbumVersionAsync(album.Id)));

        var page = await GetPageAsync(album.Id);

        Assert.Equal(AlbumCollectionState.PurchaseIntent, page.Album.State);
        var intent = Assert.Single(page.PurchaseIntents);
        Assert.Null(intent.Edition);
        Assert.True(intent.IsConvertible);
    }

    [Fact]
    public async Task Intent_OnTheOnlyEditionOfItsAlbum_CanTargetTheAlbum()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Dupuis {Guid.NewGuid():N}", null, []));
        var album = await CreateAlbumAsync("Buddy Longway");
        await CreateIntendedEditionAsync(album.Id, publisher.Id, null, null, null);

        var page = await GetPageAsync(album.Id);

        Assert.Empty(page.Editions);
        Assert.True(Assert.Single(page.PurchaseIntents).IsConvertible);
    }

    [Fact]
    public async Task Page_OfAnUnknownAlbum_IsNotFound()
    {
        var response = await _client.GetAsync($"/admin/albums/{Guid.NewGuid()}/editions-and-purchase-intents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<AlbumEditionsPage> GetPageAsync(Guid albumId) =>
        (await _client.GetFromJsonAsync<AlbumEditionsPage>($"/admin/albums/{albumId}/editions-and-purchase-intents"))!;

    private Task<AlbumForm> CreateAlbumAsync(string title) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, [], [], []));

    private async Task<EditionForm> CreateOwnedEditionAsync(
        Guid albumId, Guid publisherId, int year, AcquisitionMode mode, DateOnly? acquired, EditionCategory? category, BindingType? binding,
        int? pages) =>
        await PostAsync<EditionForm>(
            $"/admin/albums/{albumId}/editions",
            new CreateEditionRequest(
                new EditionContent(publisherId, null, year, null, binding, null, null, null, pages, category, false, true, null,
                    mode, false, acquired, null, null, false, null, null, null, null),
                await GetAlbumVersionAsync(albumId)));

    // The edition targeted by the new intent: the one the intents of the album did not target yet.
    private async Task<Guid> CreateIntendedEditionAsync(Guid albumId, Guid publisherId, int? year, EditionCategory? category, BindingType? binding)
    {
        var before = (await GetAsync<PurchaseIntentsForm>($"/admin/albums/{albumId}/purchase-intents")).Intents.Select(i => i.EditionId).ToHashSet();
        var form = await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{albumId}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisherId, null, year, null, binding, null, null, null, null, category, true),
                await GetAlbumVersionAsync(albumId)));
        return Assert.Single(form.Intents, i => !before.Contains(i.EditionId)).EditionId!.Value;
    }

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

    private async Task<T> GetAsync<T>(string uri) => (await _client.GetFromJsonAsync<T>(uri))!;

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
