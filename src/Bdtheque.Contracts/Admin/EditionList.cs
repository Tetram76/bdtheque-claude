using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The list of the editions of the administration (visuel/listes/liste-par-liste.md): one page of the editions of the
/// collection, and what the list presents of the records it is filtered on.
/// </summary>
public sealed record EditionAdminList(Page<EditionAdminListItem> Editions, EditionListFilters Filters);

/// <summary>
/// An edition of the collection as a row of the list of the administration presents it; an edition targeted by a purchase
/// intent is one of the intents, never one of the editions (fonctionnel/regles-metier/intention-d-achat.md).
/// </summary>
/// <param name="Cover">Its first cover (fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md); <c>null</c> for the generic visual.</param>
/// <param name="AcquisitionYear">The year of its acquisition date, if known.</param>
/// <param name="AlbumVersion">Version of its album, sent back with its deletion: the edition belongs to the aggregate of its album.</param>
public sealed record EditionAdminListItem(
    AlbumSummary Album,
    EditionSummary Edition,
    AlbumCover? Cover,
    EditionCategory? Category,
    AcquisitionMode AcquisitionMode,
    int? AcquisitionYear,
    DateTimeOffset ModifiedAt,
    uint AlbumVersion);

/// <summary>
/// The records the list of the editions is filtered on, as the list names them; each <c>null</c> when the list is not
/// filtered on its type, or on a record that does not exist (choix-implementation/api/recherche.md).
/// </summary>
public sealed record EditionListFilters(
    SeriesListItem? Series,
    AlbumSummary? Album,
    PublisherListItem? Publisher,
    PublisherCollectionListItem? PublisherCollection);
