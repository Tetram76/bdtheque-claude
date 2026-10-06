using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

// Records of the consultation (/catalog/…/{id}): everything known of an entity, and the entries
// leading to the records it is linked with (fonctionnel.md § Structure de l'application). The records
// linked in unbounded numbers (editions of a publisher, albums of a genre…) are read through the
// cross filters of the lists instead. Each record carries the dates of its creation and last
// modification (modele-metier.md § Attributs communs à toutes les entités), and so does each entity
// without a record of its own (contribution, visual, purchase intent), whose information is entirely
// presented by the record carrying it.

/// <param name="Genres">Those of the album and of its series, without duplicates (fonctionnel.md § Genres et univers d'un album).</param>
/// <param name="Universes">Those of the album and of its series, without duplicates.</param>
/// <param name="Editions">Every edition, owned or targeted by a purchase intent, by year.</param>
/// <param name="PurchaseIntent">The purchase intent targeting the whole album, if any.</param>
public sealed record AlbumDetail(
    AlbumSummary Album,
    int? FirstPublicationYear,
    int? FirstPublicationMonth,
    string? Summary,
    string? PersonalNotes,
    AlbumRating? Rating,
    IReadOnlyList<GenreListItem> Genres,
    IReadOnlyList<UniverseListItem> Universes,
    IReadOnlyList<ContributionItem> Contributions,
    IReadOnlyList<AlbumEditionItem> Editions,
    PurchaseIntentItem? PurchaseIntent,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>An author credited with a role; a contribution has no record of its own.</summary>
public sealed record ContributionItem(Guid Id, AuthorListItem Author, ContributionRole Role, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <param name="IsInCollection">Whether the edition is owned (fonctionnel.md § Appartenance à la collection).</param>
/// <summary>
/// A purchase intent, which has no record of its own: its target is the album, or the edition, whose
/// record carries it (fonctionnel.md § Structure de l'application).
/// </summary>
public sealed record PurchaseIntentItem(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <param name="PurchaseIntent">The purchase intent targeting the edition, which is then not owned, if any.</param>
public sealed record AlbumEditionItem(EditionSummary Edition, bool IsInCollection, PurchaseIntentItem? PurchaseIntent);

/// <param name="Publisher">The publisher of the series: its template publisher (fonctionnel.md § Structure de l'application).</param>
/// <param name="PublisherCollection">The collection of the series: its template collection.</param>
/// <param name="Contributions">The authors of the series: its template contributions.</param>
/// <param name="Albums">Every album of the series, in its order (fonctionnel.md § Ordre des albums dans une série).</param>
public sealed record SeriesDetail(
    Guid Id,
    string Title,
    SeriesStatus? Status,
    int? TheoreticalVolumeCount,
    bool IsComplete,
    string? Summary,
    string? PersonalNotes,
    PublisherListItem? Publisher,
    PublisherCollectionListItem? PublisherCollection,
    IReadOnlyList<GenreListItem> Genres,
    IReadOnlyList<UniverseListItem> Universes,
    IReadOnlyList<ContributionItem> Contributions,
    IReadOnlyList<AlbumSummary> Albums,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <param name="IsbnChecksumValid">
/// Whether the check digit of the ISBN is consistent, <c>null</c> without ISBN: a warning only, an
/// inconsistent ISBN being stored as entered (fonctionnel.md § Validation de l'ISBN).
/// </param>
/// <param name="AcquisitionAmount">Together with <paramref name="AcquisitionCurrency"/> (ISO 4217 code, or <c>QZF</c> for the old franc).</param>
/// <param name="InitialValueAmount">Together with <paramref name="InitialValueCurrency"/>.</param>
/// <param name="IsInCollection">Whether the edition is owned (fonctionnel.md § Appartenance à la collection).</param>
/// <param name="PurchaseIntent">The purchase intent targeting the edition, which is then not owned, if any.</param>
/// <param name="Visuals">In their presentation order (fonctionnel.md § Ordre des visuels d'une édition).</param>
public sealed record EditionDetail(
    AlbumSummary Album,
    EditionSummary Edition,
    bool? IsbnChecksumValid,
    BindingType? Binding,
    BookOrientation? Orientation,
    ReadingDirection? ReadingDirection,
    EditionFormat? Format,
    int? PageCount,
    EditionCategory? Category,
    bool IsDedicated,
    bool IsColor,
    EditionCondition? Condition,
    AcquisitionMode? AcquisitionMode,
    bool IsSecondHand,
    DateOnly? AcquisitionDate,
    decimal? AcquisitionAmount,
    string? AcquisitionCurrency,
    bool IsFree,
    decimal? InitialValueAmount,
    string? InitialValueCurrency,
    string? PersonalReference,
    string? PersonalNotes,
    bool IsInCollection,
    PurchaseIntentItem? PurchaseIntent,
    IReadOnlyList<EditionVisualItem> Visuals,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>A visual of an edition, which has no record of its own.</summary>
/// <param name="DisplayOrder">Rank among the visuals of the same type.</param>
/// <param name="OriginalPath">Path of the original file, relative to the visuals volume.</param>
/// <param name="DisplayPath">Path of the reduced WebP version shown by default, relative to the visuals volume.</param>
public sealed record EditionVisualItem(
    Guid Id, VisualType Type, int DisplayOrder, string OriginalPath, string DisplayPath, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <param name="Bibliography">The albums the author is credited on, in the order of the albums.</param>
public sealed record AuthorDetail(
    Guid Id,
    string? LastName,
    string? FirstName,
    string? Pseudonym,
    string? Biography,
    string? Nationality,
    IReadOnlyList<BibliographyItem> Bibliography,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>
/// An album of a bibliography, with every role the author is credited with on it: an index leading to
/// the record of the album, which carries the contributions themselves, whole.
/// </summary>
public sealed record BibliographyItem(AlbumSummary Album, IReadOnlyList<ContributionRole> Roles);

/// <param name="Collections">Its collections, by name. Its editions are listed by <c>/catalog/editions?publisherId=…</c>.</param>
public sealed record PublisherDetail(
    Guid Id, string Name, string? Website, IReadOnlyList<PublisherCollectionListItem> Collections, DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>A collection of a publisher; its editions are listed by <c>/catalog/editions?publisherCollectionId=…</c>.</summary>
public sealed record PublisherCollectionDetail(
    Guid Id, string Name, Guid PublisherId, string PublisherName, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <summary>A genre; its albums and series are listed by <c>/catalog/albums?genreId=…</c> and <c>/catalog/series?genreId=…</c>.</summary>
public sealed record GenreDetail(Guid Id, string Label, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <summary>
/// A universe and its place in the hierarchy; its albums and series, those of its sub-universes
/// included (fonctionnel.md § Hiérarchie des univers), are listed by <c>/catalog/albums?universeId=…</c>
/// and <c>/catalog/series?universeId=…</c>.
/// </summary>
/// <param name="Ancestors">From the top of the hierarchy down to the parent; empty for a universe at the top.</param>
/// <param name="Children">Its direct sub-universes, by name.</param>
public sealed record UniverseDetail(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<UniverseListItem> Ancestors,
    IReadOnlyList<UniverseListItem> Children,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
