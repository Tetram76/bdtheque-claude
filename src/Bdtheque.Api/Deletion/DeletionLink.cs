using Bdtheque.Contracts.Deletion;
using Bdtheque.Infrastructure;

namespace Bdtheque.Api.Deletion;

/// <summary>
/// One kind of link uniting a record to the rest of the data, as listed by fonctionnel.md §
/// Suppression des entités: its nature decides what deleting the record does to the records it
/// selects.
/// </summary>
/// <param name="Records">Identifiers of the records linked to the record with the given identifier.</param>
internal sealed record DeletionLink(LinkNature Nature, EntityKind Kind, Func<BdthequeDbContext, Guid, IQueryable<Guid>> Records);

internal enum LinkNature
{
    /// <summary>The linked records reference the deleted one: they block its deletion.</summary>
    Reference,

    /// <summary>The linked records lose their association with the deleted one.</summary>
    Association,

    /// <summary>The linked records are part of the deleted one, and deleted with it.</summary>
    Composition,
}
