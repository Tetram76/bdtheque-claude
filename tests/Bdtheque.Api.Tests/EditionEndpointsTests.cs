using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Administration of the editions (<c>/admin/albums/{albumId}/editions</c>), children of the
/// aggregate of their album, including their deletion.
/// </summary>
public sealed class EditionEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditionEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task New_ForAnAlbumWithoutSeries_ProposesTheDefaultValues()
    {
        var album = await CreateAlbumAsync();

        var form = await _client.GetFromJsonAsync<NewEditionForm>($"/admin/albums/{album.Id}/editions/new");

        Assert.Equal(album.Id, form!.AlbumId);
        Assert.Equal(album.Version, form.AlbumVersion);
        Assert.Equal(
            Content(null) with
            {
                Condition = EditionCondition.Excellent,
                Binding = BindingType.Hardcover,
                Orientation = BookOrientation.Portrait,
                ReadingDirection = ReadingDirection.LeftToRight,
                Format = EditionFormat.Standard,
                AcquisitionMode = null,
            },
            form.Content);
    }

    [Fact]
    public async Task New_ForAnAlbumOfASeries_TakesTheTemplateOfTheSeries()
    {
        var publisher = await CreatePublisherAsync();
        var collection = await CreateCollectionAsync(publisher);
        var template = new SeriesEditionTemplate(
            publisher.Id, collection.Id, EditionCategory.LimitedEdition, null, BindingType.Paperback, null, null, EditionFormat.Large, false);
        var series = await PostAsync<SeriesForm>("/admin/series", new SeriesContent(
            $"Série {Guid.NewGuid():N}", null, null, null, false, false, false, null, null, template, [], [], []));
        var album = await CreateAlbumAsync(series.Id);

        var form = await _client.GetFromJsonAsync<NewEditionForm>($"/admin/albums/{album.Id}/editions/new");

        // A field the template leaves empty is not pre-filled, not even with the default value.
        Assert.Equal(
            Content(publisher.Id) with
            {
                PublisherCollectionId = collection.Id,
                Category = EditionCategory.LimitedEdition,
                Binding = BindingType.Paperback,
                Format = EditionFormat.Large,
                IsColor = false,
                AcquisitionMode = null,
            },
            form!.Content);
    }

    [Fact]
    public async Task New_ForAnUnknownAlbum_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/albums/{Guid.CreateVersion7()}/editions/new");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Create_WithEveryField_ReturnsThemAsStoredAndChangesTheAlbumVersion()
    {
        var album = await CreateAlbumAsync();
        var publisher = await CreatePublisherAsync();
        var collection = await CreateCollectionAsync(publisher);
        var content = new EditionContent(
            publisher.Id, collection.Id, 1978, " 2-205-00217-1 ", BindingType.Paperback, BookOrientation.Landscape,
            ReadingDirection.RightToLeft, EditionFormat.Pocket, 48, EditionCategory.FirstEdition, true, false, EditionCondition.Good,
            AcquisitionMode.Purchase, true, new DateOnly(2020, 3, 15), 9.9m, "EUR", false, 35m, "FRF", " A-001 ", " Notes ");

        var response = await _client.PostAsJsonAsync($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(content, album.Version));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EditionForm>();
        Assert.Equal(album.Id, created!.AlbumId);
        Assert.Equal(content with { Isbn = "2-205-00217-1", PersonalReference = "A-001", PersonalNotes = "Notes" }, created.Content);
        Assert.True(created.IsbnChecksumValid);
        Assert.Equal($"/admin/albums/{album.Id}/editions/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.NotEqual(album.Version, created.AlbumVersion);
        Assert.Equal(created.AlbumVersion, (await GetAlbumAsync(album.Id)).Version);
        Assert.Equal(created, await GetAsync(created));
    }

    [Fact]
    public async Task Create_WithAnInconsistentIsbn_StoresItAndWarns()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id) with { Isbn = "2-205-00217-0" });

        Assert.Equal("2-205-00217-0", created.Content.Isbn);
        Assert.False(created.IsbnChecksumValid);
    }

    [Fact]
    public async Task Create_WithoutIsbn_HasNoCheckResult()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id));

        Assert.Null(created.IsbnChecksumValid);
    }

    [Fact]
    public async Task Create_RealizesThePurchaseIntentOnTheAlbum()
    {
        var album = await CreateAlbumAsync();
        await SeedAlbumIntentAsync(album.Id);
        album = await GetAlbumAsync(album.Id);

        await CreateAsync(album, Content((await CreatePublisherAsync()).Id) with { AcquisitionMode = AcquisitionMode.Gift });

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.PurchaseIntents.AnyAsync(p => p.AlbumId == album.Id));
    }

    [Fact]
    public async Task Create_WithoutAcquisitionMode_IsABusinessError()
    {
        // Only a purchase intent creates an edition not owned (fonctionnel.md § Appartenance à la collection).
        var album = await CreateAlbumAsync();

        var response = await PostCreateAsync(album, Content((await CreatePublisherAsync()).Id) with { AcquisitionMode = null });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionAcquisitionModeRequired);
        await AssertNoEditionAsync(album.Id);
    }

    [Fact]
    public async Task Create_WithoutPublisher_IsABusinessError()
    {
        var album = await CreateAlbumAsync();

        var response = await PostCreateAsync(album, Content(null));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionPublisherRequired);
    }

    [Fact]
    public async Task Create_WithTheCollectionOfAnotherPublisher_IsABusinessError()
    {
        var collection = await CreateCollectionAsync(await CreatePublisherAsync());

        var response = await PostCreateAsync(
            await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id) with { PublisherCollectionId = collection.Id });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNotOfPublisher);
    }

    [Fact]
    public async Task Create_FreePurchase_IsABusinessError()
    {
        var response = await PostCreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id) with { IsFree = true });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionPurchaseCannotBeFree);
    }

    [Fact]
    public async Task Create_WithAPriceButNoReferenceDate_IsABusinessError()
    {
        var response = await PostCreateAsync(
            await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id) with { AcquisitionAmount = 10m, AcquisitionCurrency = "EUR" });

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionAcquisitionPriceReferenceDateRequired);
    }

    [Fact]
    public async Task Create_WithAPriceDatedByTheAlbum_Succeeds()
    {
        var album = await CreateAlbumAsync(firstPublicationYear: 1955);

        var created = await CreateAsync(
            album, Content((await CreatePublisherAsync()).Id) with { AcquisitionAmount = 500m, AcquisitionCurrency = "QZF" });

        Assert.Equal((500m, "QZF"), (created.Content.AcquisitionAmount, created.Content.AcquisitionCurrency));
    }

    [Theory]
    [InlineData("publisher")]
    [InlineData("collection")]
    public async Task Create_ReferencingAnUnknownRecord_IsAFunctionalError(string reference)
    {
        var publisher = await CreatePublisherAsync();
        var content = reference == "publisher"
            ? Content(Guid.CreateVersion7())
            : Content(publisher.Id) with { PublisherCollectionId = Guid.CreateVersion7() };

        var response = await PostCreateAsync(await CreateAlbumAsync(), content);

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Create_FromAStaleAlbumVersion_IsAFunctionalError()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        await CreateAsync(album, Content(publisherId));

        var response = await PostCreateAsync(album, Content(publisherId));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Create_OnAnUnknownAlbum_IsAFunctionalError()
    {
        var response = await _client.PostAsJsonAsync(
            $"/admin/albums/{Guid.CreateVersion7()}/editions", new CreateEditionRequest(Content((await CreatePublisherAsync()).Id), 1));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_AtTheCurrentAlbumVersion_ReplacesEveryField()
    {
        var album = await CreateAlbumAsync();
        var created = await CreateAsync(album, Content((await CreatePublisherAsync()).Id) with { Isbn = "2-205-00217-1" });
        var otherPublisher = await CreatePublisherAsync();
        var collection = await CreateCollectionAsync(otherPublisher);
        var content = new EditionContent(
            otherPublisher.Id, collection.Id, 2001, "978-2-205-01234-7", BindingType.Paperback, BookOrientation.Landscape,
            ReadingDirection.RightToLeft, EditionFormat.Special, 62, EditionCategory.LimitedEdition, true, false, EditionCondition.Poor,
            AcquisitionMode.Trade, true, new DateOnly(2021, 5, 2), 12.345m, "KWD", false, 15m, "USD", "B-002", "Échangée");

        var updated = await UpdateAsync(created, content);

        Assert.Equal(content, updated.Content);
        Assert.NotEqual(created.AlbumVersion, updated.AlbumVersion);
        Assert.Equal(updated, await GetAsync(updated));
    }

    [Fact]
    public async Task Update_ClearingEveryOptionalField_EmptiesThem()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), FullContent((await CreatePublisherAsync()).Id));
        var content = Content(created.Content.PublisherId) with { AcquisitionMode = AcquisitionMode.Gift, IsColor = false };

        var updated = await UpdateAsync(created, content);

        Assert.Equal(content, updated.Content);
        Assert.Null(updated.IsbnChecksumValid);
    }

    [Fact]
    public async Task Update_SwappingTheReferenceDateOfThePrice_SucceedsInASinglePut()
    {
        // The edition year and the acquisition date replace each other as reference date of the
        // price: the form is applied as a whole, whatever the order of its fields.
        var created = await CreateAsync(
            await CreateAlbumAsync(),
            Content((await CreatePublisherAsync()).Id) with { PublicationYear = 2019, AcquisitionAmount = 10m, AcquisitionCurrency = "EUR" });

        var updated = await UpdateAsync(created, created.Content with { PublicationYear = null, AcquisitionDate = new DateOnly(2020, 1, 1) });

        Assert.Null(updated.Content.PublicationYear);
        Assert.Equal(new DateOnly(2020, 1, 1), updated.Content.AcquisitionDate);
    }

    [Fact]
    public async Task Update_BecomingFreeWithTheAmountsEmptied_SucceedsInASinglePut()
    {
        var created = await CreateAsync(
            await CreateAlbumAsync(),
            Content((await CreatePublisherAsync()).Id) with
            {
                AcquisitionMode = AcquisitionMode.Gift, PublicationYear = 2019, AcquisitionAmount = 10m, AcquisitionCurrency = "EUR",
                InitialValueAmount = 12m, InitialValueCurrency = "EUR",
            });

        var updated = await UpdateAsync(
            created,
            created.Content with
            {
                IsFree = true, AcquisitionAmount = null, AcquisitionCurrency = null, InitialValueAmount = null, InitialValueCurrency = null,
            });

        Assert.True(updated.Content.IsFree);
    }

    [Fact]
    public async Task Update_ClearingTheAcquisitionMode_IsABusinessErrorAndKeepsTheEditionUntouched()
    {
        // An owned edition stays owned until deleted (choix-implementation.md § Intention d'achat :
        // agrégat porté par l'album).
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id));

        var response = await _client.PutAsJsonAsync(
            EditionUri(created), new UpdateEditionRequest(created.Content with { AcquisitionMode = null }, created.AlbumVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.EditionAcquisitionModeRequired);
        Assert.Equal(created, await GetAsync(created));
    }

    [Fact]
    public async Task Update_FromAStaleAlbumVersion_IsAFunctionalErrorAndKeepsTheOtherModification()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id));
        var updated = await UpdateAsync(created, created.Content with { PageCount = 48 });

        var response = await _client.PutAsJsonAsync(
            EditionUri(created), new UpdateEditionRequest(created.Content with { PageCount = 62 }, created.AlbumVersion));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(updated, await GetAsync(created));
    }

    [Fact]
    public async Task Update_TheEditionOfAnotherAlbum_IsAFunctionalError()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id));
        var otherAlbum = await CreateAlbumAsync();

        var response = await _client.PutAsJsonAsync(
            $"/admin/albums/{otherAlbum.Id}/editions/{created.Id}", new UpdateEditionRequest(created.Content, otherAlbum.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_TheEditionOfAnotherAlbum_IsAFunctionalError()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id));
        var otherAlbum = await CreateAlbumAsync();

        var response = await _client.GetAsync($"/admin/albums/{otherAlbum.Id}/editions/{created.Id}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Theory]
    [InlineData("2-205-00217-1", true)]
    [InlineData("978-2-205-01234-7", true)]
    [InlineData("2-205-00217-0", false)]
    public async Task IsbnCheck_ReportsTheConsistencyOfTheCheckDigit(string isbn, bool expected)
    {
        var check = await _client.GetFromJsonAsync<IsbnCheck>($"/admin/editions/isbn-check?isbn={Uri.EscapeDataString(isbn)}");

        Assert.Equal(expected, check!.IsChecksumValid);
    }

    [Fact]
    public async Task DeletionImpact_OfTheLastOwnedEdition_AnnouncesThatTheAlbumLeavesTheCollection()
    {
        var album = await CreateAlbumAsync();
        var created = await CreateAsync(album, Content((await CreatePublisherAsync()).Id));
        await SeedVisualsAsync(created.Id, 2);

        var impact = await GetImpactAsync(created);

        Assert.Empty(impact.BlockedBy);
        Assert.Empty(impact.AssociationsRemoved);
        Assert.Equal([new ImpactCount(EntityKind.EditionVisual, 2)], impact.DeletedWith);
        Assert.Equal([new ImpactCount(EntityKind.Album, 1)], impact.LeavingCollection);
    }

    [Fact]
    public async Task DeletionImpact_OfAnOwnedEditionAmongOthers_KeepsTheAlbumInTheCollection()
    {
        var album = await CreateAlbumAsync();
        var publisherId = (await CreatePublisherAsync()).Id;
        var first = await CreateAsync(album, Content(publisherId));
        await CreateAsync(await GetAlbumAsync(album.Id), Content(publisherId));

        var impact = await GetImpactAsync(first);

        Assert.Empty(impact.LeavingCollection);
    }

    [Fact]
    public async Task DeletionImpact_OfAWishedEdition_AnnouncesItsIntentAndKeepsTheCollectionAsItIs()
    {
        // The only owned edition of the album stays: the album remains in the collection.
        var album = await CreateAlbumAsync();
        await CreateAsync(album, Content((await CreatePublisherAsync()).Id));
        var wishedId = await SeedWishedEditionAsync(album.Id);

        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/albums/{album.Id}/editions/{wishedId}/deletion-impact");

        Assert.Equal([new ImpactCount(EntityKind.PurchaseIntent, 1)], impact!.DeletedWith);
        Assert.Empty(impact.LeavingCollection);
    }

    [Fact]
    public async Task DeletionImpact_OfTheEditionOfAnotherAlbum_IsAFunctionalError()
    {
        var created = await CreateAsync(await CreateAlbumAsync(), Content((await CreatePublisherAsync()).Id));
        var otherAlbum = await CreateAlbumAsync();

        var response = await _client.GetAsync($"/admin/albums/{otherAlbum.Id}/editions/{created.Id}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_RemovesTheEditionWithItsVisualsAndKeepsTheAlbum()
    {
        var album = await CreateAlbumAsync();
        var created = await CreateAsync(album, Content((await CreatePublisherAsync()).Id));
        await SeedVisualsAsync(created.Id, 2);
        var impact = await GetImpactAsync(created);

        var response = await _client.DeleteAsync(DeleteUri(created, created.AlbumVersion, impact.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await ProblemAssert.IsProblemAsync(await _client.GetAsync(EditionUri(created)), HttpStatusCode.NotFound, ProblemTypes.Functional);
        Assert.NotEqual(created.AlbumVersion, (await GetAlbumAsync(album.Id)).Version);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.EditionVisuals.AnyAsync(v => v.EditionId == created.Id));
    }

    [Fact]
    public async Task Delete_AWishedEdition_RemovesItsIntent()
    {
        var album = await CreateAlbumAsync();
        var wishedId = await SeedWishedEditionAsync(album.Id);
        var uri = $"/admin/albums/{album.Id}/editions/{wishedId}";
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"{uri}/deletion-impact");

        var response = await _client.DeleteAsync($"{uri}?version={(await GetAlbumAsync(album.Id)).Version}&fingerprint={impact!.Fingerprint}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.PurchaseIntents.AnyAsync(p => p.AlbumId == album.Id));
        Assert.False(await context.Editions.AnyAsync(e => e.AlbumId == album.Id));
    }

    [Fact]
    public async Task Delete_FromAStaleAlbumVersion_IsAFunctionalError()
    {
        var album = await CreateAlbumAsync();
        var created = await CreateAsync(album, Content((await CreatePublisherAsync()).Id));
        var impact = await GetImpactAsync(created);
        await UpdateAsync(created, created.Content with { PageCount = 48 });

        var response = await _client.DeleteAsync(DeleteUri(created, created.AlbumVersion, impact.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    // The minimal content of an owned edition: a purchase, without any optional field.
    private static EditionContent Content(Guid? publisherId) =>
        new(publisherId, null, null, null, null, null, null, null, null, null, false, true, null,
            AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null);

    private static EditionContent FullContent(Guid publisherId) =>
        new(publisherId, null, 1978, "2-205-00217-1", BindingType.Hardcover, BookOrientation.Portrait, ReadingDirection.LeftToRight,
            EditionFormat.Standard, 48, EditionCategory.FirstEdition, true, true, EditionCondition.VeryGood,
            AcquisitionMode.Purchase, true, new DateOnly(2020, 3, 15), 9.9m, "EUR", false, 35m, "FRF", "A-001", "Notes");

    private static string EditionUri(EditionForm edition) => $"/admin/albums/{edition.AlbumId}/editions/{edition.Id}";

    private static string DeleteUri(EditionForm edition, uint albumVersion, string fingerprint) =>
        $"{EditionUri(edition)}?version={albumVersion}&fingerprint={fingerprint}";

    private async Task<EditionForm> GetAsync(EditionForm edition) => (await _client.GetFromJsonAsync<EditionForm>(EditionUri(edition)))!;

    private async Task<DeletionImpact> GetImpactAsync(EditionForm edition) =>
        (await _client.GetFromJsonAsync<DeletionImpact>($"{EditionUri(edition)}/deletion-impact"))!;

    private Task<HttpResponseMessage> PostCreateAsync(AlbumForm album, EditionContent content) =>
        _client.PostAsJsonAsync($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(content, album.Version));

    private async Task<EditionForm> CreateAsync(AlbumForm album, EditionContent content)
    {
        var response = await PostCreateAsync(album, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionForm>())!;
    }

    private async Task<EditionForm> UpdateAsync(EditionForm edition, EditionContent content)
    {
        var response = await _client.PutAsJsonAsync(EditionUri(edition), new UpdateEditionRequest(content, edition.AlbumVersion));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionForm>())!;
    }

    private async Task AssertNoEditionAsync(Guid albumId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Editions.AnyAsync(e => e.AlbumId == albumId));
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

    // Seeded through the domain: intents have no endpoint yet, and these visuals need no file.
    private async Task SeedVisualsAsync(Guid editionId, int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var edition = await context.Editions.SingleAsync(e => e.Id == editionId);
        for (var order = 0; order < count; order++)
            context.Add(edition.AddVisual(DomainEnums.VisualType.Plate, $"{Guid.NewGuid():N}.jpg", order));
        await context.SaveChangesAsync();
    }

    private async Task SeedAlbumIntentAsync(Guid albumId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var album = await context.Albums.SingleAsync(a => a.Id == albumId);
        context.Add(album.AddPurchaseIntent());
        await context.SaveChangesAsync();
    }

    private async Task<Guid> SeedWishedEditionAsync(Guid albumId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var album = await context.Albums.SingleAsync(a => a.Id == albumId);
        var publisher = new Publisher($"Éditeur {Guid.NewGuid():N}");
        var wished = new Edition(album, publisher);
        context.AddRange(publisher, wished, album.AddPurchaseIntent(wished));
        await context.SaveChangesAsync();
        return wished.Id;
    }
}
