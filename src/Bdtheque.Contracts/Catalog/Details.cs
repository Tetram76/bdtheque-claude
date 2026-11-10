using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

// Records of the consultation (/catalog/…/{id}), which only the albums, the series and the authors have
// (fonctionnel/structure-de-l-application.md): everything known of the entity, and the entries leading
// to the records it is linked with. The records linked in unbounded numbers (editions of a publisher,
// albums of a genre…) are read through the cross filters of the lists, or through the works of a genre or
// of a universe, instead. Each record carries the dates of its creation and last modification
// (modele-metier/attributs-communs-a-toutes-les-entites.md), and so does each entity it presents
// without a record of its own (edition, contribution, visual, purchase intent).

/// <param name="Genres">Those of the album and of its series, without duplicates (fonctionnel/regles-metier/genres-et-univers-d-un-album.md).</param>
/// <param name="Universes">Those of the album and of its series, without duplicates, by name.</param>
/// <param name="Editions">Every edition, owned or targeted by a purchase intent, whole, by year.</param>
/// <param name="PurchaseIntent">The purchase intent targeting the whole album, if any.</param>
/// <param name="SeriesAlbums">
/// Every album of its series, itself included, as a card, in the order of the series
/// (fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md); none without series. A series counts a few dozen albums at
/// most: the record carries them whole (choix-implementation/api/organisation-de-l-api.md, consultation).
/// </param>
public sealed record AlbumDetail(
    AlbumSummary Album,
    int? FirstPublicationYear,
    int? FirstPublicationMonth,
    string? Summary,
    string? PersonalNotes,
    AlbumRating? Rating,
    IReadOnlyList<GenreListItem> Genres,
    IReadOnlyList<UniverseInHierarchy> Universes,
    IReadOnlyList<ContributionItem> Contributions,
    IReadOnlyList<EditionDetail> Editions,
    PurchaseIntentItem? PurchaseIntent,
    IReadOnlyList<AlbumCard> SeriesAlbums,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>An author credited with a role; a contribution has no record of its own.</summary>
public sealed record ContributionItem(Guid Id, AuthorListItem Author, ContributionRole Role, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <summary>
/// A purchase intent, which has no record of its own: the record of its album presents it, with the album
/// or the edition it targets (fonctionnel/structure-de-l-application.md).
/// </summary>
public sealed record PurchaseIntentItem(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <summary>
/// A universe with the chain of its ancestors, each leading, like the universe itself, to its works
/// (fonctionnel/structure-de-l-application.md).
/// </summary>
/// <param name="Ancestors">From the top of the hierarchy down to the parent; empty for a universe at the top.</param>
public sealed record UniverseInHierarchy(UniverseListItem Universe, IReadOnlyList<UniverseListItem> Ancestors);

/// <param name="Publisher">The publisher of the series: its template publisher (fonctionnel/structure-de-l-application.md).</param>
/// <param name="PublisherCollection">The collection of the series: its template collection.</param>
/// <param name="Universes">By name.</param>
/// <param name="Contributions">The authors of the series: its template contributions.</param>
/// <param name="Overview">The number of its albums and the covers of its first volumes, for its pile of covers.</param>
/// <param name="Albums">Every album of the series, as a card, in its order (fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md).</param>
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
    IReadOnlyList<UniverseInHierarchy> Universes,
    IReadOnlyList<ContributionItem> Contributions,
    SeriesOverview Overview,
    IReadOnlyList<AlbumCard> Albums,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>
/// An edition, which has no record of its own: the record of its album presents it whole, with its visuals
/// (fonctionnel/structure-de-l-application.md).
/// </summary>
/// <param name="PublisherWebsite">Where the publisher leads, having no record of its own.</param>
/// <param name="IsbnChecksumValid">
/// Whether the check digit of the ISBN is consistent, <c>null</c> without ISBN: a warning only, an
/// inconsistent ISBN being stored as entered (fonctionnel/regles-metier/validation-de-l-isbn.md).
/// </param>
/// <param name="AcquisitionAmount">Together with <paramref name="AcquisitionCurrency"/> (ISO 4217 code, or <c>QZF</c> for the old franc).</param>
/// <param name="InitialValueAmount">Together with <paramref name="InitialValueCurrency"/>.</param>
/// <param name="IsInCollection">Whether the edition is owned (fonctionnel/regles-metier/appartenance-a-la-collection.md).</param>
/// <param name="EnteredCollectionAt">
/// When the edition became owned, set automatically, <c>null</c> for an edition not owned (modele-metier/edition.md).
/// </param>
/// <param name="PurchaseIntent">The purchase intent targeting the edition, which is then not owned, if any.</param>
/// <param name="Visuals">In their presentation order (fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md).</param>
public sealed record EditionDetail(
    EditionSummary Edition,
    string? PublisherWebsite,
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
    DateTimeOffset? EnteredCollectionAt,
    PurchaseIntentItem? PurchaseIntent,
    IReadOnlyList<EditionVisualItem> Visuals,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>A visual of an edition, which has no record of its own; <c>api</c> serves its media (<see cref="VisualMediaRoutes"/>).</summary>
/// <param name="DisplayOrder">Rank among the visuals of the same type.</param>
public sealed record EditionVisualItem(Guid Id, VisualType Type, int DisplayOrder, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

/// <param name="Bibliography">
/// The whole bibliography, a single list reconciling the series and the albums the author took part in
/// (fonctionnel/structure-de-l-application.md): the series and the albums without series together, by
/// sort key.
/// </param>
public sealed record AuthorDetail(
    Guid Id,
    string? LastName,
    string? FirstName,
    string? Pseudonym,
    string? Biography,
    string? Nationality,
    IReadOnlyList<BibliographyEntry> Bibliography,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>
/// An entry of a bibliography: either a series, or an album without series, exactly one of the two. The
/// entries are an index leading to the records, which carry the contributions themselves, whole.
/// </summary>
public sealed record BibliographyEntry(SeriesBibliographyItem? Series, AlbumBibliographyItem? Album);

/// <summary>A series of a bibliography, credited on the series itself or on one of its albums.</summary>
/// <param name="Overview">The series as a whole, its albums the author took no part in included.</param>
/// <param name="Roles">The roles credited on the series itself; empty for an author credited on its albums only.</param>
/// <param name="Albums">The albums of the series the author took part in, in the order of the series.</param>
public sealed record SeriesBibliographyItem(
    SeriesListItem Series, SeriesOverview Overview, IReadOnlyList<ContributionRole> Roles, IReadOnlyList<AlbumBibliographyItem> Albums);

/// <summary>An album of a bibliography, with every role the author is credited with on it.</summary>
public sealed record AlbumBibliographyItem(AlbumCard Album, IReadOnlyList<ContributionRole> Roles);
