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

/// <summary>Administration of the publishers (<c>/admin/publishers</c>), including their deletion.</summary>
public sealed class PublisherEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublisherEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_ReturnsThePublisherAsStored()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest(" Dargaud ", " https://www.dargaud.com "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PublisherForm>();
        Assert.Equal(("Dargaud", "https://www.dargaud.com"), (created!.Name, created.Website));
        Assert.Equal($"/admin/publishers/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{created.Id}"));
    }

    [Fact]
    public async Task Create_WithBlankName_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest(" ", null));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherNameRequired);
    }

    [Fact]
    public async Task Create_WithAnInvalidWebsite_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest("Glénat", "pas une url"));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherWebsiteInvalid);
    }

    [Fact]
    public async Task Create_WithANameAlreadyUsed_IsABusinessError()
    {
        await CreateAsync("Casterman");

        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest("Casterman", null));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherNameAlreadyUsed);
    }

    [Fact]
    public async Task Create_WithANameDifferingOnlyByCase_IsAccepted()
    {
        // modele-metier.md § Éditeur: unique as typed, 'Dargaud' and 'dargaud' remain distinct.
        await CreateAsync("Delcourt");

        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest("delcourt", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownPublisher_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/publishers/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_ReturnsTheNewStateAndVersion()
    {
        var publisher = await CreateAsync("Dupuis");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest("Dupuis BD", "https://www.dupuis.com", publisher.Version));

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
        Assert.Equal(("Dupuis BD", "https://www.dupuis.com"), (updated.Name, updated.Website));
        Assert.NotEqual(publisher.Version, updated.Version);
        Assert.Equal(updated, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}"));
    }

    [Fact]
    public async Task Update_ToAnotherPublishersName_IsABusinessError()
    {
        await CreateAsync("Lombard");
        var publisher = await CreateAsync("Le Lombard");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest("Lombard", null, publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherNameAlreadyUsed);
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalError()
    {
        var publisher = await CreateAsync("Soleil");
        await UpdateAsync(publisher, "Soleil Productions");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest("Soleil", null, publisher.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_AfterACollectionWasAdded_IsAFunctionalError()
    {
        // The publisher is the aggregate root of its collections: a write on one changes its version.
        var publisher = await CreateAsync("Bamboo");
        await CreateCollectionAsync(publisher, "Grand Angle");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest("Bamboo Édition", null, publisher.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfAPublisherUsedByEditionsAndSeriesTemplates_IsRefusedWithTheImpactNamingThem()
    {
        var publisher = await CreateAsync("Futuropolis");
        await SeedEditionAndSeriesTemplateOfAsync(publisher.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(publisher, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.Series, 1), new ImpactCount(EntityKind.Edition, 1)], impact.BlockedBy);
        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
        Assert.Equal(publisher, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}"));
    }

    [Fact]
    public async Task Delete_AsConfirmed_RemovesThePublisherWithItsCollections()
    {
        var publisher = await CreateAsync("Vents d'Ouest");
        var first = await CreateCollectionAsync(publisher, "Éclipse");
        await CreateCollectionAsync(publisher with { Version = first.PublisherVersion }, "Étincelle");
        var current = await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(current!, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.PublisherCollection, 2)], impact.DeletedWith);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Publishers.AnyAsync(p => p.Id == publisher.Id));
        Assert.False(await context.Set<PublisherCollection>().AnyAsync(c => c.PublisherId == publisher.Id));
    }

    [Fact]
    public async Task Delete_WhoseImpactChangedSinceConfirmation_IsAFunctionalErrorAndKeepsThePublisher()
    {
        var publisher = await CreateAsync("Dargaud Benelux");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");
        var newVersion = (await CreateCollectionAsync(publisher, "Nouvelle")).PublisherVersion;

        var response = await _client.DeleteAsync(
            $"/admin/publishers/{publisher.Id}?version={newVersion}&fingerprint={Uri.EscapeDataString(impact!.Fingerprint)}");

        var problem = await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal([new ImpactCount(EntityKind.PublisherCollection, 1)], ProblemAssert.ImpactOf(problem).DeletedWith);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/publishers/{publisher.Id}")).StatusCode);
    }

    private static string DeleteUri(PublisherForm publisher, string fingerprint) =>
        $"/admin/publishers/{publisher.Id}?version={publisher.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    private async Task<PublisherForm> CreateAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new CreatePublisherRequest(name, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
    }

    private async Task UpdateAsync(PublisherForm publisher, string name)
    {
        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(name, null, publisher.Version));
        response.EnsureSuccessStatusCode();
    }

    private async Task<PublisherCollectionForm> CreateCollectionAsync(PublisherForm publisher, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(name, publisher.Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherCollectionForm>())!;
    }

    private async Task SeedEditionAndSeriesTemplateOfAsync(Guid publisherId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = await context.Publishers.SingleAsync(p => p.Id == publisherId);
        var album = new Album("Les Cités obscures", null);
        var series = new Series("Aldébaran");
        series.SetTemplate(publisher, null);
        context.AddRange(album, series, new Edition(album, publisher));
        await context.SaveChangesAsync();
    }
}
