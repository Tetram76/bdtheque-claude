namespace Bdtheque.Contracts.Catalog;

// Entries of the lists of the consultation (/catalog/…), each leading to the record of its item. The
// albums are listed as their AlbumSummary.

public sealed record SeriesListItem(Guid Id, string Title);

/// <summary>What the main identifier of an author is built from (fonctionnel.md § Artistes).</summary>
public sealed record AuthorListItem(Guid Id, string? LastName, string? FirstName, string? Pseudonym);

/// <param name="IsInCollection">Whether the edition is owned (fonctionnel.md § Appartenance à la collection).</param>
public sealed record EditionListItem(AlbumSummary Album, EditionSummary Edition, bool IsInCollection);

public sealed record PublisherListItem(Guid Id, string Name);

public sealed record PublisherCollectionListItem(Guid Id, string Name, Guid PublisherId, string PublisherName);

public sealed record GenreListItem(Guid Id, string Label);

/// <param name="ParentName">The name of the parent universe; <c>null</c> for a universe at the top of the hierarchy.</param>
public sealed record UniverseListItem(Guid Id, string Name, Guid? ParentId, string? ParentName);
