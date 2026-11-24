using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>An author as a row of the list of the consultation presents it, that of the administration without its actions.</summary>
/// <param name="CreditedAlbumCount">The albums the author is credited on, each once whatever its roles.</param>
/// <param name="CreditedAlbumsInCollectionCount">Those of <paramref name="CreditedAlbumCount"/> that are in the collection.</param>
/// <param name="Roles">The roles the author holds on albums and series, each once, in role order.</param>
public sealed record AuthorCatalogListItem(
    Guid Id,
    string? LastName,
    string? FirstName,
    string? Pseudonym,
    string? Nationality,
    int CreditedAlbumCount,
    int CreditedAlbumsInCollectionCount,
    IReadOnlyList<ContributionRole> Roles);
