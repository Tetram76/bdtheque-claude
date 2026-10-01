using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the universes and of their hierarchy (<c>/admin/universes</c>).</summary>
internal static class UniverseEndpoints
{
    // fonctionnel.md § Suppression des entités: a universe is refused deletion while it has
    // sub-universes, and is only associated with albums and series.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Universe,
            (context, id) => context.Universes.Where(u => u.ParentId == id).Select(u => u.Id)),
        new(LinkNature.Association, EntityKind.Album,
            (context, id) => context.Albums.Where(a => a.Universes.Any(u => u.Id == id)).Select(a => a.Id)),
        new(LinkNature.Association, EntityKind.Series,
            (context, id) => context.Series.Where(s => s.Universes.Any(u => u.Id == id)).Select(s => s.Id)),
    ];

    public static void MapUniverses(this RouteGroupBuilder admin)
    {
        var universes = admin.MapGroup("/universes");
        universes.MapGet("/{id:guid}", GetAsync);
        universes.MapPost("/", CreateAsync);
        universes.MapPut("/{id:guid}", UpdateAsync);
        universes.MapDeletion<Universe>(DeletionLinks);
    }

    private static async Task<UniverseForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Universes
            .Where(u => u.Id == id)
            .Select(u => new UniverseForm(u.Id, u.Name, u.Description, u.ParentId, EF.Property<uint>(u, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Universe), id);

    private static async Task<Created<UniverseForm>> CreateAsync(
        CreateUniverseRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var universe = new Universe(request.Name);
        universe.SetDescription(request.Description);
        if (request.ParentId is { } parentId)
            universe.SetParent(await LoadWithAncestorsAsync(context, parentId, cancellationToken));

        context.Universes.Add(universe);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/universes/{universe.Id}", ToForm(context, universe));
    }

    private static async Task<UniverseForm> UpdateAsync(
        Guid id, UpdateUniverseRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var universe = await context.LoadAggregateForWriteAsync<Universe>(id, request.Version, cancellationToken: cancellationToken);
        universe.SetName(request.Name);
        universe.SetDescription(request.Description);
        if (request.ParentId != universe.ParentId)
        {
            universe.SetParent(request.ParentId is { } parentId
                ? await LoadWithAncestorsAsync(context, parentId, cancellationToken)
                : null);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, universe);
    }

    /// <summary>
    /// Loads a universe with its whole ancestor chain, which <see cref="Universe.SetParent"/> needs to
    /// rule out a cycle: the universes form a small referential, loaded whole rather than walked
    /// recursively (choix-implementation.md § Organisation de l'API). Read under the hierarchy lock,
    /// so that the check sees every change of parent committed before it.
    /// </summary>
    private static async Task<Universe> LoadWithAncestorsAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken)
    {
        await UniverseHierarchy.LockAsync(context, cancellationToken);
        var universes = await context.Universes.ToListAsync(cancellationToken);
        return universes.SingleOrDefault(u => u.Id == id) ?? throw new EntityNotFoundException(typeof(Universe), id);
    }

    private static UniverseForm ToForm(BdthequeDbContext context, Universe universe) =>
        new(universe.Id, universe.Name, universe.Description, universe.ParentId, context.VersionOf(universe));
}
