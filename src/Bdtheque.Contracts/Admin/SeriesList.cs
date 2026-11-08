using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The list of the series of the administration (visuel/listes/liste-par-liste.md): one page of its series, and what the
/// list presents of the records it is filtered on.
/// </summary>
public sealed record SeriesAdminList(Page<SeriesAdminListItem> Series, SeriesListFilters Filters);

/// <summary>A series as a row of the list of the administration presents it.</summary>
/// <param name="Overview">Its pile of covers and the number of its albums, as the consultation presents them.</param>
/// <param name="AlbumsInCollectionCount">
/// Its albums that are part of the collection, an album only targeted by a purchase intent being none
/// (fonctionnel/regles-metier/appartenance-a-la-collection.md).
/// </param>
/// <param name="IsComplete">Whether the user deems the series complete: « Complète », otherwise « À compléter ».</param>
/// <param name="Version">Version of the series, sent back with its deletion.</param>
public sealed record SeriesAdminListItem(
    Guid Id,
    string Title,
    SeriesStatus? Status,
    SeriesOverview Overview,
    int AlbumsInCollectionCount,
    bool IsComplete,
    DateTimeOffset ModifiedAt,
    uint Version);

/// <summary>
/// The records the list of the series is filtered on, as the list names them; each <c>null</c> when the list is not
/// filtered on its type, or on a record that does not exist (choix-implementation/api/recherche.md).
/// </summary>
public sealed record SeriesListFilters(
    AuthorListItem? Author,
    PublisherListItem? Publisher,
    PublisherCollectionListItem? PublisherCollection,
    GenreListItem? Genre,
    UniverseListItem? Universe);
