using System.Linq.Expressions;
using Bdtheque.Domain.Entities;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Membership of the collection (fonctionnel/regles-metier/appartenance-a-la-collection.md), computed by a
/// single expression reused by every query, never recoded query by query
/// (choix-implementation/api/organisation-de-l-api.md).
/// </summary>
internal static class CollectionMembership
{
    /// <summary>An edition is owned — part of the collection — once its acquisition mode is set.</summary>
    public static readonly Expression<Func<Edition, bool>> IsOwned = e => e.AcquisitionMode != null;

    /// <summary>
    /// An edition targeted by a purchase intent on it; an intent on the album targets none of its editions
    /// (fonctionnel/regles-metier/intention-d-acquisition.md). The intents of an album carry its identifier, those on its
    /// editions included.
    /// </summary>
    public static readonly Expression<Func<Edition, bool>> IsTargetedByIntent = e => e.Album.PurchaseIntents.Any(p => p.EditionId == e.Id);
}
