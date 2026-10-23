using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

// Entries of the lists of the consultation (/catalog/…), each leading to the record of its item. The
// albums are listed as their AlbumSummary.

public sealed record SeriesListItem(Guid Id, string Title);

/// <summary>What the main identifier of an author is built from (fonctionnel/presentation.md § Artistes).</summary>
public sealed record AuthorListItem(Guid Id, string? LastName, string? FirstName, string? Pseudonym);

/// <param name="IsInCollection">Whether the edition is owned (fonctionnel/regles-metier.md § Appartenance à la collection).</param>
public sealed record EditionListItem(AlbumSummary Album, EditionSummary Edition, bool IsInCollection);

/// <param name="Website">Where a publisher leads, having no record of its own (fonctionnel/index.md § Structure de l'application).</param>
public sealed record PublisherListItem(Guid Id, string Name, string? Website);

public sealed record PublisherCollectionListItem(Guid Id, string Name, Guid PublisherId, string PublisherName);

public sealed record GenreListItem(Guid Id, string Label);

/// <param name="ParentName">The name of the parent universe; <c>null</c> for a universe at the top of the hierarchy.</param>
public sealed record UniverseListItem(Guid Id, string Name, Guid? ParentId, string? ParentName);

/// <summary>A visual of an edition, which leads to the record of the album of its edition.</summary>
/// <param name="OriginalPath">Path of the original file, relative to the visuals volume.</param>
/// <param name="DisplayPath">Path of the reduced WebP version shown by default, relative to the visuals volume.</param>
public sealed record EditionVisualListItem(
    Guid Id, VisualType Type, string OriginalPath, string DisplayPath, AlbumSummary Album, EditionSummary Edition);

/// <summary>
/// An author credited with a role, which leads to the record of the album or of the series carrying the
/// contribution: exactly one of <paramref name="Album"/> and <paramref name="Series"/>.
/// </summary>
public sealed record ContributionListItem(
    Guid Id, ContributionRole Role, AuthorListItem Author, AlbumSummary? Album, SeriesListItem? Series);
