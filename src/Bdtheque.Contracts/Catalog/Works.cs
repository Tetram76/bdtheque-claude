namespace Bdtheque.Contracts.Catalog;

// Works of a genre or of a universe (/catalog/genres/{id}/works, /catalog/universes/{id}/works), which
// have no record: the list of their series and albums, presented as the bibliography of an author
// (fonctionnel.md § Structure de l'application). Unbounded, the list is paginated on its first level.

/// <param name="Works">The series and the albums without series together, by sort key.</param>
public sealed record GenreWorks(GenreListItem Genre, Page<WorksEntry> Works);

/// <param name="Works">
/// The series and the albums without series together, by sort key, those of its sub-universes included
/// (fonctionnel.md § Hiérarchie des univers).
/// </param>
public sealed record UniverseWorks(UniverseInHierarchy Universe, Page<WorksEntry> Works);

/// <summary>An entry of the works: either a series, or an album without series, exactly one of the two.</summary>
public sealed record WorksEntry(SeriesWorksItem? Series, AlbumCard? Album);

/// <summary>
/// A series of the works, attached itself or through one of its albums, the genres and universes of an
/// album in a series being those of the album and of the series (fonctionnel.md § Genres et univers d'un
/// album).
/// </summary>
/// <param name="Overview">The series as a whole, its albums not attached included.</param>
/// <param name="Albums">The albums of the series attached, all of them for a series attached itself, in the order of the series.</param>
public sealed record SeriesWorksItem(SeriesListItem Series, SeriesOverview Overview, IReadOnlyList<AlbumCard> Albums);
