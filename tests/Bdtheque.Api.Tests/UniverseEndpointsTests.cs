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

/// <summary>Administration of the universes (<c>/admin/universes</c>), including their hierarchy and deletion.</summary>
public sealed class UniverseEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UniverseEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_UnderAParent_ReturnsTheUniverseAsStored()
    {
        var parent = await CreateAsync("Spirou");

        var response = await _client.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(" Zorglub ", "  ", parent.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<UniverseForm>();
        Assert.Equal(("Zorglub", null, parent.Id), (created!.Name, created.Description, created.ParentId));
        Assert.Equal($"/admin/universes/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{created.Id}"));
    }

    [Fact]
    public async Task Create_WithBlankName_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(" ", null, null));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.UniverseNameRequired);
    }

    [Fact]
    public async Task Create_UnderAnUnknownParent_IsAFunctionalError()
    {
        var response = await _client.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest("Orphelin", null, Guid.CreateVersion7()));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Get_UnknownUniverse_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/universes/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_MovesTheUniverseUnderAnotherParent_ThenToTheTop()
    {
        var first = await CreateAsync("Marvel");
        var second = await CreateAsync("DC");
        var universe = await CreateAsync("Multivers", first.Id);

        var moved = await UpdateAsync(universe, new UpdateUniverseRequest("Multivers", "Toutes les Terres", second.Id, universe.Version));
        var detached = await UpdateAsync(moved, new UpdateUniverseRequest("Multivers", "Toutes les Terres", null, moved.Version));

        Assert.Equal((second.Id, "Toutes les Terres"), (moved.ParentId, moved.Description));
        Assert.Null(detached.ParentId);
        Assert.Equal(detached, await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{universe.Id}"));
    }

    [Fact]
    public async Task Update_UnderItsOwnDescendant_IsABusinessError()
    {
        var root = await CreateAsync("Valérian");
        var child = await CreateAsync("Galaxity", root.Id);
        var grandChild = await CreateAsync("Point central", child.Id);

        var response = await _client.PutAsJsonAsync(
            $"/admin/universes/{root.Id}", new UpdateUniverseRequest(root.Name, null, grandChild.Id, root.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.UniverseHierarchyCycle);
    }

    [Fact]
    public async Task Update_UnderAnUnknownParent_IsAFunctionalError()
    {
        var universe = await CreateAsync("Thorgal");

        var response = await _client.PutAsJsonAsync(
            $"/admin/universes/{universe.Id}", new UpdateUniverseRequest("Thorgal", null, Guid.CreateVersion7(), universe.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalError()
    {
        var universe = await CreateAsync("Blake et Mortimer");
        await UpdateAsync(universe, new UpdateUniverseRequest("Blake & Mortimer", null, null, universe.Version));

        var response = await _client.PutAsJsonAsync(
            $"/admin/universes/{universe.Id}", new UpdateUniverseRequest("Blake", null, null, universe.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfAUniverseWithSubUniverses_IsRefusedWithTheImpactNamingThem()
    {
        var parent = await CreateAsync("Star Wars");
        await CreateAsync("Ancienne République", parent.Id);
        await CreateAsync("Empire", parent.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/universes/{parent.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(parent, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.Universe, 2)], impact.BlockedBy);
        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
        Assert.Equal(parent, await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{parent.Id}"));
    }

    [Fact]
    public async Task Delete_AsConfirmed_RemovesTheUniverseFromItsAlbumsAndSeries()
    {
        var universe = await CreateAsync("Les Mondes d'Aldébaran");
        var (albumId, seriesId) = await SeedAlbumAndSeriesInAsync(universe.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/universes/{universe.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(universe, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.Album, 1), new ImpactCount(EntityKind.Series, 1)], impact.AssociationsRemoved);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Universes.AnyAsync(u => u.Id == universe.Id));
        Assert.Empty((await context.Albums.Include(a => a.Universes).SingleAsync(a => a.Id == albumId)).Universes);
        Assert.Empty((await context.Series.Include(s => s.Universes).SingleAsync(s => s.Id == seriesId)).Universes);
    }

    [Fact]
    public async Task Delete_OfASubUniverse_LeavesItsParent()
    {
        var parent = await CreateAsync("Troy");
        var child = await CreateAsync("Lanfeust des étoiles", parent.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/universes/{child.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(child, impact!.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(parent, await _client.GetFromJsonAsync<UniverseForm>($"/admin/universes/{parent.Id}"));
    }

    private static string DeleteUri(UniverseForm universe, string fingerprint) =>
        $"/admin/universes/{universe.Id}?version={universe.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    private async Task<UniverseForm> CreateAsync(string name, Guid? parentId = null)
    {
        var response = await _client.PostAsJsonAsync("/admin/universes", new CreateUniverseRequest(name, null, parentId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UniverseForm>())!;
    }

    private async Task<UniverseForm> UpdateAsync(UniverseForm universe, UpdateUniverseRequest request)
    {
        var response = await _client.PutAsJsonAsync($"/admin/universes/{universe.Id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UniverseForm>())!;
    }

    private async Task<(Guid AlbumId, Guid SeriesId)> SeedAlbumAndSeriesInAsync(Guid universeId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var universe = await context.Universes.SingleAsync(u => u.Id == universeId);
        var album = new Album("La Planète", null);
        album.AddUniverse(universe);
        var series = new Series("Aldébaran");
        series.AddUniverse(universe);
        context.AddRange(album, series);
        await context.SaveChangesAsync();
        return (album.Id, series.Id);
    }
}
