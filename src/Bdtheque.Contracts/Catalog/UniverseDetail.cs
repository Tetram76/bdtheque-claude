namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// The record of a universe in the consultation (fonctionnel/structure-de-l-application.md, visuel/fiche-univers/): whole,
/// without filter, like every record.
/// </summary>
/// <param name="Ancestors">From the top of the hierarchy down to its parent; empty for a universe at the top.</param>
/// <param name="Tree">
/// The whole tree of its universe at the top of the hierarchy, itself included, in the order of the hierarchy: each universe
/// followed by its sub-universes, siblings by name; each says its parent, which places it in the tree, and what is attached
/// to it as its own record presents it.
/// </param>
/// <param name="Works">
/// Its series and albums, those of its sub-universes included (fonctionnel/regles-metier/hierarchie-des-univers.md), presented
/// as the bibliography of an author: the series and the albums without series together, by sort key; a series attached itself
/// with all its albums, a series attached through some of its albums only with those. Nobody being credited on a universe, no
/// entry has roles.
/// </param>
public sealed record UniverseDetail(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<UniverseListItem> Ancestors,
    IReadOnlyList<UniverseInTree> Tree,
    IReadOnlyList<BibliographyEntry> Works,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>
/// A universe of the tree on the record of a universe (visuel/fiche-univers/contenu.md): its place in the tree, by its parent,
/// none at the top, and the numbers of series and albums its own record presents, its own sub-universes included.
/// </summary>
public sealed record UniverseInTree(Guid Id, string Name, Guid? ParentId, int SeriesCount, int AlbumCount);
