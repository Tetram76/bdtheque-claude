using Bdtheque.Api.Deletion;
using Bdtheque.Api.Visuals;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Infrastructure;

namespace Bdtheque.Api.Endpoints;

internal static class DeletionEndpoints
{
    /// <summary>
    /// Maps the deletion of the group's resource: <c>GET {id}/deletion-impact</c>, the impact the
    /// confirmation message announces, then <c>DELETE {id}?version=…&amp;fingerprint=…</c>, sending back
    /// the version of the record and the fingerprint of the impact the user confirmed.
    /// </summary>
    public static void MapDeletion<TRoot>(this RouteGroupBuilder group, IReadOnlyList<DeletionLink> links)
        where TRoot : EntityBase, IAggregateRoot
    {
        group.MapGet("/{id:guid}/deletion-impact", (Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
            AggregateDeletion.GetImpactAsync<TRoot>(context, id, links, cancellationToken));

        group.MapDelete("/{id:guid}", async (Guid id, uint version, string fingerprint, BdthequeDbContext context, VisualStorage storage, CancellationToken cancellationToken) =>
        {
            await AggregateDeletion.DeleteAsync<TRoot>(context, storage, id, version, fingerprint, links, cancellationToken);
            return TypedResults.NoContent();
        });
    }

    /// <summary>
    /// Maps the deletion of a child of the group's aggregate (e.g. <c>/publishers/{rootId}/collections/{id}</c>),
    /// on the same two routes as <see cref="MapDeletion{TRoot}"/>; the group declares the root
    /// identifier as <c>{rootId}</c>. The version is the root's, which guards every write on the
    /// aggregate (choix-implementation.md § Concurrence d'accès).
    /// </summary>
    public static void MapChildDeletion<TRoot, TChild>(
        this RouteGroupBuilder group, Func<IQueryable<TRoot>, IQueryable<TRoot>> shape,
        Func<TRoot, IEnumerable<TChild>> children, IReadOnlyList<DeletionLink> links)
        where TRoot : EntityBase, IAggregateRoot
        where TChild : EntityBase
    {
        group.MapGet("/{id:guid}/deletion-impact", (Guid rootId, Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
            AggregateDeletion.GetChildImpactAsync(context, rootId, id, shape, children, links, cancellationToken));

        group.MapDelete("/{id:guid}", async (Guid rootId, Guid id, uint version, string fingerprint, BdthequeDbContext context, VisualStorage storage, CancellationToken cancellationToken) =>
        {
            await AggregateDeletion.DeleteChildAsync(context, storage, rootId, id, version, fingerprint, shape, children, links, cancellationToken);
            return TypedResults.NoContent();
        });
    }
}
