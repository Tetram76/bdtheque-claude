using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

// Entries of the lists of the consultation (/catalog/…), each leading to the record of its item.

/// <summary>An album as a row of the list of the albums of the consultation presents it.</summary>
/// <param name="Cover">
/// Its cover among the editions the filter on the possession shows, all of them without that filter
/// (fonctionnel/regles-metier/couverture-d-un-album.md); <c>null</c> for the generic visual.
/// </param>
/// <param name="Authors">The authors credited on the album, each once, by role then by sort key.</param>
/// <param name="FirstPublicationMonth">Set only with <paramref name="FirstPublicationYear"/>.</param>
public sealed record AlbumListItem(
    AlbumSummary Album,
    AlbumCover? Cover,
    IReadOnlyList<AuthorListItem> Authors,
    int? FirstPublicationYear,
    int? FirstPublicationMonth,
    AlbumCollectionState State);

public sealed record SeriesListItem(Guid Id, string Title);

/// <summary>What the main identifier of an author is built from
/// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).</summary>
public sealed record AuthorListItem(Guid Id, string? LastName, string? FirstName, string? Pseudonym);

/// <param name="IsInCollection">Whether the edition is owned (fonctionnel/regles-metier/appartenance-a-la-collection.md).</param>
public sealed record EditionListItem(AlbumSummary Album, EditionSummary Edition, bool IsInCollection);

/// <param name="Website">Where a publisher leads, having no record of its own (fonctionnel/structure-de-l-application.md).</param>
public sealed record PublisherListItem(Guid Id, string Name, string? Website);

public sealed record PublisherCollectionListItem(Guid Id, string Name, Guid PublisherId, string PublisherName);

public sealed record GenreListItem(Guid Id, string Label);

/// <param name="ParentName">The name of the parent universe; <c>null</c> for a universe at the top of the hierarchy.</param>
public sealed record UniverseListItem(Guid Id, string Name, Guid? ParentId, string? ParentName);

/// <summary>
/// A visual of an edition, which leads to the record of the album of its edition; <c>api</c> serves its
/// media (<see cref="VisualMediaRoutes"/>).
/// </summary>
public sealed record EditionVisualListItem(Guid Id, VisualType Type, AlbumSummary Album, EditionSummary Edition);

/// <summary>
/// An author credited with a role, which leads to the record of the album or of the series carrying the
/// contribution: exactly one of <paramref name="Album"/> and <paramref name="Series"/>.
/// </summary>
public sealed record ContributionListItem(
    Guid Id, ContributionRole Role, AuthorListItem Author, AlbumSummary? Album, SeriesListItem? Series);
