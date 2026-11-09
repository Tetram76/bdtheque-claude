using Bdtheque.Contracts.Catalog;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// A universe as a row of the list of the administration presents it (visuel/listes/liste-par-liste.md), the list following
/// the hierarchy: each universe followed by its sub-universes.
/// </summary>
/// <param name="Ancestors">
/// Its ancestors, from the top of the hierarchy down to its parent: the depth of its row, and what names it when its parent is
/// not on the page (a search, a page that begins below it).
/// </param>
/// <param name="AlbumCount">
/// Its albums, those of its sub-universes included (fonctionnel/regles-metier/hierarchie-des-univers.md), through their series
/// too (fonctionnel/regles-metier/genres-et-univers-d-un-album.md): those the list of the albums filtered on it finds.
/// </param>
/// <param name="SeriesCount">Its series, those of its sub-universes included: those the list of the series filtered on it finds.</param>
/// <param name="SubUniverseCount">Its sub-universes at every level.</param>
/// <param name="Version">Version of the universe, sent back with its deletion.</param>
public sealed record UniverseAdminListItem(
    Guid Id,
    string Name,
    Guid? ParentId,
    IReadOnlyList<UniverseListItem> Ancestors,
    int AlbumCount,
    int SeriesCount,
    int SubUniverseCount,
    DateTimeOffset ModifiedAt,
    uint Version);
