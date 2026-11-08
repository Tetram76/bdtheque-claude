using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>An author as a row of the list of the administration presents it (visuel/listes/liste-par-liste.md).</summary>
/// <param name="CreditedAlbumCount">The albums the author is credited on, each once whatever its roles.</param>
/// <param name="Roles">The roles the author holds on albums and series, each once, in role order.</param>
/// <param name="Version">Version of the author, sent back with its deletion.</param>
public sealed record AuthorAdminListItem(
    Guid Id,
    string? LastName,
    string? FirstName,
    string? Pseudonym,
    string? Nationality,
    int CreditedAlbumCount,
    IReadOnlyList<ContributionRole> Roles,
    DateTimeOffset ModifiedAt,
    uint Version);
