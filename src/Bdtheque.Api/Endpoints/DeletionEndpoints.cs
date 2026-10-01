using Bdtheque.Api.Deletion;
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

        group.MapDelete("/{id:guid}", async (Guid id, uint version, string fingerprint, BdthequeDbContext context, CancellationToken cancellationToken) =>
        {
            await AggregateDeletion.DeleteAsync<TRoot>(context, id, version, fingerprint, links, cancellationToken);
            return TypedResults.NoContent();
        });
    }
}
