namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The sort key the domain computes from the names of an author, with the main identifier it derives from and the entry of the
/// navigation it files the author under, which the entry form of an author presents while the names are typed
/// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).
/// </summary>
/// <remarks>All <c>null</c> without a last name or a pseudonym, which an author must have.</remarks>
public sealed record AuthorSortKey(string? DisplayName, string? SortKey, string? NavigationEntry);
