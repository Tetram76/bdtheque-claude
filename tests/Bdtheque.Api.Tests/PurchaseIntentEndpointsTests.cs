using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Administration of the purchase intents (<c>/admin/albums/{albumId}/purchase-intents</c>), children
/// of the aggregate of their album, and confirmation of the purchase of an edition targeted.
/// </summary>
public sealed class PurchaseIntentEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PurchaseIntentEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task New_ForAnAlbumOfASeries_TakesTheTemplateOfTheSeriesWithoutTheTraitsOfTheCopy()
    {
        var publisher = await CreatePublisherAsync();
        var template = new SeriesEditionTemplate(
            publisher.Id, null, EditionCategory.LimitedEdition, EditionCondition.Good, BindingType.Paperback, null, null, null, false);
        var series = await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Série {Guid.NewGuid():N}", null, null, null, false, false, false, null, null, template, [], [], []));
        var album = await CreateAlbumAsync(series.Id);

        var form = await _client.GetFromJsonAsync<NewPurchaseIntentForm>($"{IntentsUri(album.Id)}/new");

        Assert.Equal(album.Id, form!.AlbumId);
        Assert.Equal(album.Version, form.AlbumVersion);
        Assert.Equal(
            new PurchaseIntentEditionContent(
                publisher.Id, null, null, null, BindingType.Paperback, null, null, null, null, EditionCategory.LimitedEdition, false),
            form.Edition);
    }

    [Fact]
    public async Task Create_OnTheWholeAlbum_ReturnsTheIntentsAndChangesTheAlbumVersion()
    {
        var album = await CreateAlbumAsync();

        var response = await _client.PostAsJsonAsync(IntentsUri(album.Id), new CreatePurchaseIntentRequest(null, album.Version));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var intents = (await response.Content.ReadFromJsonAsync<PurchaseIntentsForm>())!;
        Assert.Equal(album.Id, intents.AlbumId);
        var intent = Assert.Single(intents.Intents);
        Assert.Null(intent.EditionId);
        Assert.NotEqual(album.Version, intents.AlbumVersion);
        Assert.Equal(intents.AlbumVersion, (await GetAlbumAsync(album.Id)).Version);
        AssertSameIntents(intents, await GetIntentsAsync(album.Id));
    }

    [Fact]
    public async Task Create_OnANewEdition_CreatesTheEditionNotOwnedWithTheFieldsEntered()
    {
        var album = await CreateAlbumAsync();
        var publisher = await CreatePublisherAsync();
        var collection = await CreateCollectionAsync(publisher);
        var edition = new PurchaseIntentEditionContent(
            publisher.Id, collection.Id, 2024, " 2-205-00217-1 ", BindingType.Paperback, BookOrientation.Landscape,
            ReadingDirection.RightToLeft, EditionFormat.Large, 64, EditionCategory.FirstEdition, false);

        var intents = await CreateAsync(album, edition);

        var editionId = Assert.Single(intents.Intents).EditionId!.Value;
        var created = await _client.GetFromJsonAsync<EditionForm>($"/admin/albums/{album.Id}/editions/{editionId}");
        Assert.Equal(
            new EditionContent(
                publisher.Id, collection.Id, 2024, "2-205-00217-1", BindingType.Paperback, BookOrientation.Landscape,
                ReadingDirection.RightToLeft, EditionFormat.Large, 64, EditionCategory.FirstEdition, false, false, null,
                null, false, null, null, null, false, null, null, null, null),
            created!.Content);
    }

    [Fact]
    public async Task Create_OnSeveralNewEditions_KeepsEachIntent()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        var first = await CreateAsync(album, Edition(publisherId));

        var intents = await CreateAsync(album with { Version = first.AlbumVersion }, Edition(publisherId));

        Assert.Equal(2, intents.Intents.Count);
        Assert.All(intents.Intents, i => Assert.NotNull(i.EditionId));
    }

    [Fact]
    public async Task Create_OnTheWholeAlbumAlreadyTargeted_IsABusinessError()
    {
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, null);

        var response = await _client.PostAsJsonAsync(IntentsUri(album.Id), new CreatePurchaseIntentRequest(null, intents.AlbumVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PurchaseIntentAlbumAlreadyTargeted);
    }

    [Fact]
    public async Task Create_OnANewEditionWhileTheWholeAlbumIsTargeted_IsABusinessErrorAndCreatesNoEdition()
    {
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, null);

        var response = await _client.PostAsJsonAsync(
            IntentsUri(album.Id), new CreatePurchaseIntentRequest(Edition((await CreatePublisherAsync()).Id), intents.AlbumVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PurchaseIntentAlbumAlreadyTargeted);
        await AssertEditionCountAsync(album.Id, 0);
    }

    [Fact]
    public async Task Create_OnANewEditionWithoutPublisher_IsABusinessError()
    {
        var album = await CreateAlbumAsync();

        var response = await _client.PostAsJsonAsync(IntentsUri(album.Id), new CreatePurchaseIntentRequest(Edition(null), album.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionPublisherRequired);
    }

    [Fact]
    public async Task Create_FromAStaleAlbumVersion_IsAFunctionalError()
    {
        var album = await CreateAlbumAsync();
        await CreateAsync(album, Edition((await CreatePublisherAsync()).Id));

        var response = await _client.PostAsJsonAsync(IntentsUri(album.Id), new CreatePurchaseIntentRequest(null, album.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_ForAnUnknownAlbum_IsAFunctionalError()
    {
        var response = await _client.GetAsync(IntentsUri(Guid.CreateVersion7()));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task DeletionImpact_OfAnIntentOnAnEdition_AnnouncesTheEditionWithItsVisuals()
    {
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, Edition((await CreatePublisherAsync()).Id));
        var intent = Assert.Single(intents.Intents);
        await SeedVisualsAsync(intent.EditionId!.Value, 2);

        var impact = await GetImpactAsync(album.Id, intent.Id);

        Assert.Empty(impact.BlockedBy);
        Assert.Empty(impact.AssociationsRemoved);
        Assert.Equal([new ImpactCount(EntityKind.Edition, 1), new ImpactCount(EntityKind.EditionVisual, 2)], impact.DeletedWith);
        Assert.Empty(impact.LeavingCollection);
    }

    [Fact]
    public async Task DeletionImpact_OfAnIntentOnTheWholeAlbum_AnnouncesNothing()
    {
        var album = await CreateAlbumAsync();
        var intent = Assert.Single((await CreateAsync(album, null)).Intents);

        var impact = await GetImpactAsync(album.Id, intent.Id);

        Assert.Empty(impact.DeletedWith);
    }

    [Fact]
    public async Task DeletionImpact_OfTheIntentOfAnotherAlbum_IsAFunctionalError()
    {
        var intent = Assert.Single((await CreateAsync(await CreateAlbumAsync(), null)).Intents);
        var otherAlbum = await CreateAlbumAsync();

        var response = await _client.GetAsync($"{IntentsUri(otherAlbum.Id)}/{intent.Id}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_AnIntentOnAnEdition_RemovesTheEditionWithItsVisualsAndKeepsTheOthers()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        var first = await CreateAsync(album, Edition(publisherId));
        var intents = await CreateAsync(album with { Version = first.AlbumVersion }, Edition(publisherId));
        var deleted = Assert.Single(first.Intents);
        await SeedVisualsAsync(deleted.EditionId!.Value, 2);
        var impact = await GetImpactAsync(album.Id, deleted.Id);

        var response = await _client.DeleteAsync(DeleteUri(album.Id, deleted.Id, intents.AlbumVersion, impact.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = await GetIntentsAsync(album.Id);
        Assert.Equal(intents.Intents.Where(i => i.Id != deleted.Id), remaining.Intents);
        Assert.NotEqual(intents.AlbumVersion, remaining.AlbumVersion);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Editions.AnyAsync(e => e.Id == deleted.EditionId));
        Assert.False(await context.EditionVisuals.AnyAsync(v => v.EditionId == deleted.EditionId));
    }

    [Fact]
    public async Task Delete_AnIntentOnTheWholeAlbum_KeepsTheEditions()
    {
        var album = await CreateAlbumAsync();
        var owned = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions",
            new CreateEditionRequest(OwnedContent((await CreatePublisherAsync()).Id), album.Version));
        var intents = await CreateAsync(album with { Version = owned.AlbumVersion }, null);
        var intent = Assert.Single(intents.Intents);
        var impact = await GetImpactAsync(album.Id, intent.Id);

        var response = await _client.DeleteAsync(DeleteUri(album.Id, intent.Id, intents.AlbumVersion, impact.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await GetIntentsAsync(album.Id)).Intents);
        await AssertEditionCountAsync(album.Id, 1);
    }

    [Fact]
    public async Task Delete_FromAStaleAlbumVersion_IsAFunctionalError()
    {
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, null);
        var intent = Assert.Single(intents.Intents);
        var impact = await GetImpactAsync(album.Id, intent.Id);

        var response = await _client.DeleteAsync(DeleteUri(album.Id, intent.Id, album.Version, impact.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task ConvertToEdition_ReplacesTheIntentOnTheWholeAlbumByAnIntentOnANewEdition()
    {
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, null);
        var publisherId = (await CreatePublisherAsync()).Id;

        var converted = await PostAsync<PurchaseIntentsForm>(
            $"{IntentsUri(album.Id)}/{Assert.Single(intents.Intents).Id}/to-edition",
            new ConvertPurchaseIntentToEditionRequest(Edition(publisherId) with { PageCount = 48 }, intents.AlbumVersion));

        var editionId = Assert.Single(converted.Intents).EditionId!.Value;
        var edition = await _client.GetFromJsonAsync<EditionForm>($"/admin/albums/{album.Id}/editions/{editionId}");
        Assert.Equal((publisherId, 48, (AcquisitionMode?)null), (edition!.Content.PublisherId, edition.Content.PageCount, edition.Content.AcquisitionMode));
        AssertSameIntents(converted, await GetIntentsAsync(album.Id));
    }

    [Fact]
    public async Task ConvertToAlbum_ReplacesTheIntentOnTheEditionAndDeletesTheEdition()
    {
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, Edition((await CreatePublisherAsync()).Id));
        var intent = Assert.Single(intents.Intents);
        await SeedVisualsAsync(intent.EditionId!.Value, 1);
        var impact = await GetImpactAsync(album.Id, intent.Id);

        var converted = await PostAsync<PurchaseIntentsForm>(
            $"{IntentsUri(album.Id)}/{intent.Id}/to-album", new ConvertPurchaseIntentToAlbumRequest(intents.AlbumVersion, impact.Fingerprint));

        Assert.Null(Assert.Single(converted.Intents).EditionId);
        AssertSameIntents(converted, await GetIntentsAsync(album.Id));
        await AssertEditionCountAsync(album.Id, 0);
    }

    [Fact]
    public async Task ConvertToAlbum_WhileAnotherEditionIsTargeted_IsABusinessErrorAndKeepsEverything()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        var first = await CreateAsync(album, Edition(publisherId));
        var intents = await CreateAsync(album with { Version = first.AlbumVersion }, Edition(publisherId));
        var intent = Assert.Single(first.Intents);
        var impact = await GetImpactAsync(album.Id, intent.Id);

        var response = await _client.PostAsJsonAsync(
            $"{IntentsUri(album.Id)}/{intent.Id}/to-album", new ConvertPurchaseIntentToAlbumRequest(intents.AlbumVersion, impact.Fingerprint));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PurchaseIntentEditionsAlreadyTargeted);
        AssertSameIntents(intents, await GetIntentsAsync(album.Id));
        await AssertEditionCountAsync(album.Id, 2);
    }

    [Fact]
    public async Task ConvertToAlbum_WithAnImpactChangedSinceConfirmed_IsAFunctionalErrorAndKeepsTheEdition()
    {
        // A visual added since the confirmation would be deleted without the user knowing.
        var album = await CreateAlbumAsync();
        var intents = await CreateAsync(album, Edition((await CreatePublisherAsync()).Id));
        var intent = Assert.Single(intents.Intents);
        var impact = await GetImpactAsync(album.Id, intent.Id);
        await SeedVisualsAsync(intent.EditionId!.Value, 1);

        var response = await _client.PostAsJsonAsync(
            $"{IntentsUri(album.Id)}/{intent.Id}/to-album", new ConvertPurchaseIntentToAlbumRequest(intents.AlbumVersion, impact.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(intents.Intents, (await GetIntentsAsync(album.Id)).Intents);
    }

    [Fact]
    public async Task Acquire_AnEditionTargeted_RecordsTheAcquisitionAndRealizesOnlyItsIntent()
    {
        var album = await CreateAlbumAsync(firstPublicationYear: 1978);
        var publisherId = (await CreatePublisherAsync()).Id;
        var first = await CreateAsync(album, Edition(publisherId));
        var intents = await CreateAsync(album with { Version = first.AlbumVersion }, Edition(publisherId));
        var bought = Assert.Single(first.Intents);
        var content = OwnedContent(publisherId) with
        {
            Condition = EditionCondition.VeryGood, IsSecondHand = true, AcquisitionDate = new DateOnly(2024, 6, 1),
            AcquisitionAmount = 12.5m, AcquisitionCurrency = "EUR", PersonalReference = "A-002",
        };

        var acquired = await PostAsync<EditionForm>(
            $"/admin/albums/{album.Id}/editions/{bought.EditionId}/acquisition", new AcquireEditionRequest(content, intents.AlbumVersion));

        Assert.Equal(content, acquired.Content);
        var remaining = await GetIntentsAsync(album.Id);
        Assert.Equal(intents.Intents.Where(i => i.Id != bought.Id), remaining.Intents);
        Assert.Equal(acquired.AlbumVersion, remaining.AlbumVersion);
    }

    [Fact]
    public async Task Acquire_WithoutAcquisitionMode_IsABusinessErrorAndKeepsTheIntent()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        var intents = await CreateAsync(album, Edition(publisherId));
        var intent = Assert.Single(intents.Intents);

        var response = await _client.PostAsJsonAsync(
            $"/admin/albums/{album.Id}/editions/{intent.EditionId}/acquisition",
            new AcquireEditionRequest(OwnedContent(publisherId) with { AcquisitionMode = null }, intents.AlbumVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionAcquisitionModeRequired);
        AssertSameIntents(intents, await GetIntentsAsync(album.Id));
    }

    [Fact]
    public async Task Acquire_AnEditionAlreadyOwned_IsABusinessError()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        var owned = await PostAsync<EditionForm>(
            $"/admin/albums/{album.Id}/editions", new CreateEditionRequest(OwnedContent(publisherId), album.Version));

        var response = await _client.PostAsJsonAsync(
            $"/admin/albums/{album.Id}/editions/{owned.Id}/acquisition", new AcquireEditionRequest(owned.Content, owned.AlbumVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionAlreadyOwned);
    }

    private static PurchaseIntentEditionContent Edition(Guid? publisherId) =>
        new(publisherId, null, null, null, null, null, null, null, null, null, true);

    // The minimal content of an owned edition: a purchase, without any optional field.
    private static EditionContent OwnedContent(Guid publisherId) =>
        new(publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);

    // A record compares its list by reference: the intents are compared one by one.
    private static void AssertSameIntents(PurchaseIntentsForm expected, PurchaseIntentsForm actual)
    {
        Assert.Equal((expected.AlbumId, expected.AlbumVersion), (actual.AlbumId, actual.AlbumVersion));
        Assert.Equal(expected.Intents, actual.Intents);
    }

    private static string IntentsUri(Guid albumId) => $"/admin/albums/{albumId}/purchase-intents";

    private static string DeleteUri(Guid albumId, Guid intentId, uint albumVersion, string fingerprint) =>
        $"{IntentsUri(albumId)}/{intentId}?version={albumVersion}&fingerprint={fingerprint}";

    private async Task<PurchaseIntentsForm> GetIntentsAsync(Guid albumId) =>
        (await _client.GetFromJsonAsync<PurchaseIntentsForm>(IntentsUri(albumId)))!;

    private async Task<DeletionImpact> GetImpactAsync(Guid albumId, Guid intentId) =>
        (await _client.GetFromJsonAsync<DeletionImpact>($"{IntentsUri(albumId)}/{intentId}/deletion-impact"))!;

    private Task<PurchaseIntentsForm> CreateAsync(AlbumForm album, PurchaseIntentEditionContent? edition) =>
        PostAsync<PurchaseIntentsForm>(IntentsUri(album.Id), new CreatePurchaseIntentRequest(edition, album.Version));

    private async Task AssertEditionCountAsync(Guid albumId, int expected)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.Equal(expected, await context.Editions.CountAsync(e => e.AlbumId == albumId));
    }

    private Task<AlbumForm> CreateAlbumAsync(Guid? seriesId = null, int? firstPublicationYear = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            seriesId is null ? $"Album {Guid.NewGuid():N}" : null, null, seriesId, AlbumType.Regular, false, null, null, null,
            firstPublicationYear, null, null, null, null, [], [], []));

    private async Task<AlbumForm> GetAlbumAsync(Guid id) => (await _client.GetFromJsonAsync<AlbumForm>($"/admin/albums/{id}"))!;

    private Task<PublisherForm> CreatePublisherAsync() =>
        PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest($"Éditeur {Guid.NewGuid():N}", null));

    private Task<PublisherCollectionForm> CreateCollectionAsync(PublisherForm publisher) =>
        PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest($"Collection {Guid.NewGuid():N}", publisher.Version));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }

    // Seeded through the domain: these visuals need no file.
    private async Task SeedVisualsAsync(Guid editionId, int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var edition = await context.Editions.SingleAsync(e => e.Id == editionId);
        for (var order = 0; order < count; order++)
            context.Add(edition.AddVisual(DomainEnums.VisualType.Plate, $"{Guid.NewGuid():N}.jpg", order));
        await context.SaveChangesAsync();
    }
}

/// <summary>
/// Public list of the purchase intents (<c>/catalog/purchase-intents</c>), on a database of its own:
/// the list covers every intent of the base.
/// </summary>
public sealed class PurchaseIntentCatalogTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PurchaseIntentCatalogTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task List_PaginatesTheIntentsInTheOrderOfTheAlbumsWithWhatTheirLabelsNeed()
    {
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new CreatePublisherRequest("Casterman", null));
        var collection = await PostAsync<PublisherCollectionForm>(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest("Petit format", publisher.Version));
        var series = await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            "Les Aventures de Tintin", null, null, null, false, false, false, null, null,
            new SeriesEditionTemplate(null, null, null, null, null, null, null, null, null), [], [], []));
        // Sorted by the sort key of the album, or that of its series without a title of its own:
        // "Aventures de Tintin [Les]" < "Lotus bleu [Le]" < "Zorglub".
        var zorglub = await CreateAlbumAsync("Zorglub", null, null);
        var lotus = await CreateAlbumAsync("Le Lotus bleu", series.Id, 5);
        var untitled = await CreateAlbumAsync(null, series.Id, 2);
        var owned = await PostAsync<EditionForm>(
            $"/admin/albums/{lotus.Id}/editions",
            new CreateEditionRequest(
                new EditionContent(publisher.Id, null, null, null, null, null, null, null, null, null, false, true, null,
                    AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
                lotus.Version));
        var zorglubIntent = Assert.Single((await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{zorglub.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, zorglub.Version))).Intents);
        var lotusIntent = Assert.Single((await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{lotus.Id}/purchase-intents",
            new CreatePurchaseIntentRequest(
                new PurchaseIntentEditionContent(publisher.Id, collection.Id, 2024, "2-203-00105-1", null, null, null, null, null, null, true),
                owned.AlbumVersion))).Intents);
        var untitledIntent = Assert.Single((await PostAsync<PurchaseIntentsForm>(
            $"/admin/albums/{untitled.Id}/purchase-intents", new CreatePurchaseIntentRequest(null, untitled.Version))).Intents);

        var first = await _client.GetFromJsonAsync<Page<PurchaseIntentListItem>>("/catalog/purchase-intents?page=1&pageSize=2");
        var second = await _client.GetFromJsonAsync<Page<PurchaseIntentListItem>>("/catalog/purchase-intents?page=2&pageSize=2");

        Assert.Equal((1, 2, 3), (first!.Number, first.Size, first.TotalCount));
        Assert.Equal((2, 2, 3), (second!.Number, second.Size, second.TotalCount));
        Assert.Equal(
            [
                new PurchaseIntentListItem(
                    untitledIntent.Id,
                    new AlbumSummary(untitled.Id, null, series.Id, "Les Aventures de Tintin", AlbumType.Regular, false, 2, null, null, false),
                    null),
                new PurchaseIntentListItem(
                    lotusIntent.Id,
                    new AlbumSummary(lotus.Id, "Le Lotus bleu", series.Id, "Les Aventures de Tintin", AlbumType.Regular, false, 5, null, null, true),
                    new EditionSummary(lotusIntent.EditionId!.Value, publisher.Id, "Casterman", collection.Id, "Petit format", 2024, "2-203-00105-1")),
                new PurchaseIntentListItem(
                    zorglubIntent.Id,
                    new AlbumSummary(zorglub.Id, "Zorglub", null, null, AlbumType.Regular, false, null, null, null, false),
                    null),
            ],
            first.Items.Concat(second.Items));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_WithAPageOutOfBounds_IsATechnicalError(int page, int pageSize)
    {
        // The frontend builds the paging, never the user.
        var response = await _client.GetAsync($"/catalog/purchase-intents?page={page}&pageSize={pageSize}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.InternalServerError, ProblemTypes.Technical);
    }

    private Task<AlbumForm> CreateAlbumAsync(string? title, Guid? seriesId, int? volumeNumber) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, seriesId, AlbumType.Regular, false, volumeNumber, null, null, null, null, null, null, null, [], [], []));

    private async Task<TForm> PostAsync<TForm>(string uri, object request)
    {
        var response = await _client.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
