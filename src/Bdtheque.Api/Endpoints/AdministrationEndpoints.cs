using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// What the home page of the administration presents of the whole base (visuel/administration/contenu.md): the number of
/// records of each type, which its menu presents too, and the checks of the records to correct.
/// </summary>
internal static class AdministrationEndpoints
{
    public static void MapAdministrationOverview(this RouteGroupBuilder admin)
    {
        admin.MapGet("/record-counts", GetRecordCountsAsync);
        admin.MapGet("/checks", GetChecksAsync);
    }

    // A count per query: each one a single aggregate on an indexed table of a few thousand rows at most.
    private static async Task<RecordCounts> GetRecordCountsAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        new(
            await context.Albums.CountAsync(cancellationToken),
            // The albums the filter « Catalogué » of the list of the albums keeps, by the same expression
            // (fonctionnel/regles-metier/appartenance-a-la-collection.md#états-dun-album): never an album wished for.
            await context.Albums.CountAsync(
                CatalogExpressions.Expand((Album a) => a.CollectionState() == AlbumCollectionState.OutsideCollection), cancellationToken),
            await context.Editions.CountAsync(CollectionMembership.IsOwned, cancellationToken),
            await context.Series.CountAsync(cancellationToken),
            await context.Series.CountAsync(s => s.Status == null, cancellationToken),
            await context.PurchaseIntents.CountAsync(cancellationToken),
            await context.Authors.CountAsync(cancellationToken),
            await context.Publishers.CountAsync(cancellationToken),
            await context.PublisherCollections.CountAsync(cancellationToken),
            await context.Genres.CountAsync(cancellationToken),
            await context.Universes.CountAsync(cancellationToken),
            await context.Universes.CountAsync(u => u.ParentId != null, cancellationToken));

    // The errors to correct, counted as their list counts them.
    private static async Task<AdminChecks> GetChecksAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        new(await InvalidIsbnEndpoints.CountToCorrectAsync(context, cancellationToken));
}
