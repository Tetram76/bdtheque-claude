namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// The record of a universe in the consultation (fonctionnel/structure-de-l-application.md, visuel/fiche-univers/): whole,
/// without filter, like every record.
/// </summary>
/// <param name="Ancestors">From the top of the hierarchy down to its parent; empty for a universe at the top.</param>
/// <param name="SubUniverses">Its direct sub-universes, by name.</param>
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
    IReadOnlyList<UniverseListItem> SubUniverses,
    IReadOnlyList<BibliographyEntry> Works,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
