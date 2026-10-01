using Bdtheque.Api.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The common deletion mechanism (choix-implementation.md § Suppression des entités : mise en
/// œuvre), beyond what the endpoints of each resource exercise.
/// </summary>
public sealed class AggregateDeletionTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AggregateDeletionTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ReferenceTheImpactMissed_IsStillABusinessRefusal()
    {
        // The ON DELETE RESTRICT safety net: a reference the impact does not know about (here, no
        // link declared at all) is refused by the database, and that refusal remains a business one.
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var parent = new Universe("Parent");
        var child = new Universe("Enfant");
        child.SetParent(parent);
        context.AddRange(parent, child);
        await context.SaveChangesAsync();
        var version = context.VersionOf(parent);
        context.ChangeTracker.Clear();
        var impact = await AggregateDeletion.ComputeImpactAsync(context, parent.Id, [], CancellationToken.None);

        await Assert.ThrowsAsync<DeletionRefusedException>(
            () => AggregateDeletion.DeleteAsync<Universe>(context, parent.Id, version, impact.Fingerprint, [], CancellationToken.None));

        context.ChangeTracker.Clear();
        Assert.True(await context.Universes.AnyAsync(u => u.Id == parent.Id));
    }
}
