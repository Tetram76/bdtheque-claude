using System.Linq.Expressions;
using Bdtheque.Domain.Entities;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Membership of the collection (fonctionnel.md § Appartenance à la collection), computed by a
/// single expression reused by every query, never recoded query by query
/// (choix-implementation.md § Organisation de l'API).
/// </summary>
internal static class CollectionMembership
{
    /// <summary>An edition is owned — part of the collection — once its acquisition mode is set.</summary>
    public static readonly Expression<Func<Edition, bool>> IsOwned = e => e.AcquisitionMode != null;
}
