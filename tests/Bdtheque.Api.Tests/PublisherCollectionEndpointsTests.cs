using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Administration of the collections of a publisher (<c>/admin/publishers/{id}/collections</c>): they
/// belong to the aggregate of their publisher, whose version guards every write on them.
/// </summary>
public sealed class PublisherCollectionEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublisherCollectionEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_ReturnsTheCollectionAsStoredAndTheNewVersionOfThePublisher()
    {
        var publisher = await CreatePublisherAsync("Dargaud");

        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(" Poisson Pilote ", publisher.Version));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<PublisherCollectionForm>())!;
        Assert.Equal((publisher.Id, "Poisson Pilote"), (created.PublisherId, created.Name));
        Assert.NotEqual(publisher.Version, created.PublisherVersion);
        Assert.Equal($"/admin/publishers/{publisher.Id}/collections/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await _client.GetFromJsonAsync<PublisherCollectionForm>($"/admin/publishers/{publisher.Id}/collections/{created.Id}"));
        Assert.Equal(created.PublisherVersion, (await GetPublisherAsync(publisher.Id)).Version);
    }

    [Fact]
    public async Task Create_WithBlankName_IsABusinessError()
    {
        var publisher = await CreatePublisherAsync("Glénat");

        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(" ", publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNameRequired);
    }

    [Fact]
    public async Task Create_WithANameLongerThanTheColumn_IsABusinessError()
    {
        var publisher = await CreatePublisherAsync("Éditeur aux collections longues");

        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(new string('C', 301), publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Update_WithANameLongerThanTheColumn_IsABusinessError()
    {
        var publisher = await CreatePublisherAsync("Éditeur aux renommages longs");
        var collection = await CreateAsync(publisher, "Court");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections/{collection.Id}",
            new UpdatePublisherCollectionRequest(new string('D', 301), collection.PublisherVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Create_WithANameAlreadyUsedByThePublisher_IsABusinessError()
    {
        var publisher = await CreatePublisherAsync("Casterman");
        var first = await CreateAsync(publisher, "Écritures");

        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest("Écritures", first.PublisherVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNameAlreadyUsed);
    }

    [Fact]
    public async Task Create_WithANameUsedByAnotherPublisher_IsAccepted()
    {
        var first = await CreatePublisherAsync("Dupuis");
        var second = await CreatePublisherAsync("Le Lombard");
        await CreateAsync(first, "Aventure");

        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{second.Id}/collections", new CreatePublisherCollectionRequest("Aventure", second.Version));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ForAnUnknownPublisher_IsAFunctionalError()
    {
        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{Guid.CreateVersion7()}/collections", new CreatePublisherCollectionRequest("Orpheline", 1));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Create_FromAStaleVersionOfThePublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync("Soleil");
        await CreateAsync(publisher, "Celtic");

        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest("Métamorphose", publisher.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task List_ReturnsTheCollectionsOfThePublisherOnly()
    {
        var publisher = await CreatePublisherAsync("Delcourt");
        var other = await CreatePublisherAsync("Bamboo");
        var first = await CreateAsync(publisher, "Conquistador");
        var second = await CreateAsync(publisher with { Version = first.PublisherVersion }, "Contrebande");
        await CreateAsync(other, "Grand Angle");

        var collections = await _client.GetFromJsonAsync<List<PublisherCollectionForm>>($"/admin/publishers/{publisher.Id}/collections");

        Assert.Equal(["Conquistador", "Contrebande"], collections!.Select(c => c.Name));
        Assert.All(collections!, c => Assert.Equal(second.PublisherVersion, c.PublisherVersion));
    }

    [Fact]
    public async Task List_OfAnUnknownPublisher_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/publishers/{Guid.CreateVersion7()}/collections");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_ThroughAnotherPublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync("Futuropolis");
        var other = await CreatePublisherAsync("Vents d'Ouest");
        var collection = await CreateAsync(publisher, "Albums");

        var response = await _client.GetAsync($"/admin/publishers/{other.Id}/collections/{collection.Id}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_RenamesTheCollectionAndReturnsTheNewVersion()
    {
        var publisher = await CreatePublisherAsync("Kana");
        var collection = await CreateAsync(publisher, "Dark Kana");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections/{collection.Id}",
            new UpdatePublisherCollectionRequest("Big Kana", collection.PublisherVersion));

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<PublisherCollectionForm>())!;
        Assert.Equal("Big Kana", updated.Name);
        Assert.NotEqual(collection.PublisherVersion, updated.PublisherVersion);
        Assert.Equal(updated, await _client.GetFromJsonAsync<PublisherCollectionForm>($"/admin/publishers/{publisher.Id}/collections/{collection.Id}"));
    }

    [Fact]
    public async Task Update_ToTheNameOfAnotherCollectionOfThePublisher_IsABusinessError()
    {
        var publisher = await CreatePublisherAsync("Panini");
        var first = await CreateAsync(publisher, "Marvel");
        var second = await CreateAsync(publisher with { Version = first.PublisherVersion }, "Marvel Deluxe");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections/{second.Id}",
            new UpdatePublisherCollectionRequest("Marvel", second.PublisherVersion));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNameAlreadyUsed);
    }

    [Fact]
    public async Task Update_FromAStaleVersionOfThePublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync("Urban Comics");
        var first = await CreateAsync(publisher, "DC Essentiels");
        await CreateAsync(publisher with { Version = first.PublisherVersion }, "DC Rebirth");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections/{first.Id}",
            new UpdatePublisherCollectionRequest("DC", first.PublisherVersion));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_ThroughAnotherPublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync("Ankama");
        var other = await CreatePublisherAsync("Mana Books");
        var collection = await CreateAsync(publisher, "Dofus");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{other.Id}/collections/{collection.Id}",
            new UpdatePublisherCollectionRequest("Wakfu", other.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
        Assert.Equal(collection, await _client.GetFromJsonAsync<PublisherCollectionForm>($"/admin/publishers/{publisher.Id}/collections/{collection.Id}"));
    }

    [Fact]
    public async Task Delete_OfACollectionUsedByAnEditionAndASeriesTemplate_IsRefusedWithTheImpactNamingThem()
    {
        var publisher = await CreatePublisherAsync("Éditions Dupuis");
        var collection = await CreateAsync(publisher, "Repérages");
        await SeedEditionAndSeriesTemplateOfAsync(publisher.Id, collection.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>(
            $"/admin/publishers/{publisher.Id}/collections/{collection.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(publisher, collection, collection.PublisherVersion, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.Series, 1), new ImpactCount(EntityKind.Edition, 1)], impact.BlockedBy);
        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
        Assert.Equal(collection, await _client.GetFromJsonAsync<PublisherCollectionForm>($"/admin/publishers/{publisher.Id}/collections/{collection.Id}"));
    }

    [Fact]
    public async Task Delete_AsConfirmed_RemovesOnlyTheCollection()
    {
        var publisher = await CreatePublisherAsync("Glénat BD");
        var kept = await CreateAsync(publisher, "Grafica");
        var removed = await CreateAsync(publisher with { Version = kept.PublisherVersion }, "Vents d'Ailleurs");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>(
            $"/admin/publishers/{publisher.Id}/collections/{removed.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(publisher, removed, removed.PublisherVersion, impact!.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = await _client.GetFromJsonAsync<List<PublisherCollectionForm>>($"/admin/publishers/{publisher.Id}/collections");
        Assert.Equal([kept.Id], remaining!.Select(c => c.Id));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/publishers/{publisher.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_FromAStaleVersionOfThePublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync("Kurokawa");
        var first = await CreateAsync(publisher, "Seinen");
        await CreateAsync(publisher with { Version = first.PublisherVersion }, "Shonen");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>(
            $"/admin/publishers/{publisher.Id}/collections/{first.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(publisher, first, first.PublisherVersion, impact!.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfACollectionReferencedSinceConfirmation_IsRefusedAndKeepsTheCollection()
    {
        var publisher = await CreatePublisherAsync("Pika");
        var collection = await CreateAsync(publisher, "Pika Shonen");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>(
            $"/admin/publishers/{publisher.Id}/collections/{collection.Id}/deletion-impact");
        await SeedEditionAndSeriesTemplateOfAsync(publisher.Id, collection.Id);

        var response = await _client.DeleteAsync(DeleteUri(publisher, collection, collection.PublisherVersion, impact!.Fingerprint));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        Assert.Equal(collection, await _client.GetFromJsonAsync<PublisherCollectionForm>($"/admin/publishers/{publisher.Id}/collections/{collection.Id}"));
    }

    [Fact]
    public async Task Impact_ThroughAnotherPublisher_IsAFunctionalError()
    {
        var publisher = await CreatePublisherAsync("Tonkam");
        var other = await CreatePublisherAsync("Taifu");
        var collection = await CreateAsync(publisher, "Tonkam Seinen");

        var response = await _client.GetAsync($"/admin/publishers/{other.Id}/collections/{collection.Id}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    private static string DeleteUri(PublisherForm publisher, PublisherCollectionForm collection, uint publisherVersion, string fingerprint) =>
        $"/admin/publishers/{publisher.Id}/collections/{collection.Id}?version={publisherVersion}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    private async Task<PublisherForm> CreatePublisherAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest(name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
    }

    private Task<PublisherForm> GetPublisherAsync(Guid id) =>
        _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{id}")!;

    private async Task<PublisherCollectionForm> CreateAsync(PublisherForm publisher, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(name, publisher.Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherCollectionForm>())!;
    }

    private async Task SeedEditionAndSeriesTemplateOfAsync(Guid publisherId, Guid collectionId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = await context.Publishers.Include(p => p.Collections).SingleAsync(p => p.Id == publisherId);
        var collection = publisher.Collections.Single(c => c.Id == collectionId);
        var album = new Album("Spirou et Fantasio", null);
        var series = new Series("Le Petit Spirou");
        series.SetTemplate(publisher, collection);
        var edition = new Edition(album, publisher);
        edition.SetPublisher(publisher, collection);
        context.AddRange(album, series, edition);
        await context.SaveChangesAsync();
    }
}
