using Bdtheque.Contracts.Admin;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// The list of the series of the consultation: a page of its rows, and the records it is filtered on, named as the lists name
/// them, for the screen to present them without another request.
/// </summary>
public sealed record SeriesCatalogList(Page<SeriesCatalogListItem> Series, SeriesListFilters Filters);

/// <summary>A series as a row of the list of the consultation presents it.</summary>
/// <param name="Overview">
/// Its pile of covers and the number of its albums, the covers taken among the editions the list presents
/// (fonctionnel/regles-metier/couverture-d-un-album.md): the owned editions only in the list limited to them, all of them
/// otherwise.
/// </param>
/// <param name="AlbumsInCollectionCount">
/// Its albums that are part of the collection, an album only targeted by a purchase intent being none
/// (fonctionnel/regles-metier/appartenance-a-la-collection.md).
/// </param>
public sealed record SeriesCatalogListItem(Guid Id, string Title, SeriesOverview Overview, int AlbumsInCollectionCount);
