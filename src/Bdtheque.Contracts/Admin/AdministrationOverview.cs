namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The number of records of each type, with the detail the home page of the administration gives of each
/// (visuel/administration/contenu.md, Les fiches), which its menu also presents (visuel/administration/en-tete-et-menu.md).
/// </summary>
/// <param name="AlbumsOutsideCollection">Albums without any owned edition (fonctionnel/regles-metier/appartenance-a-la-collection.md).</param>
/// <param name="SeriesWithoutStatus">Series whose status is not set.</param>
/// <param name="SubUniverses">Universes that have a parent universe.</param>
public sealed record RecordCounts(
    int Albums,
    int AlbumsOutsideCollection,
    int Editions,
    int Series,
    int SeriesWithoutStatus,
    int PurchaseIntents,
    int Authors,
    int Publishers,
    int PublisherCollections,
    int Genres,
    int Universes,
    int SubUniverses);

/// <summary>
/// The checks of the records to correct or complete (visuel/administration/contenu.md, À vérifier): only those the
/// specification defines.
/// </summary>
/// <param name="InvalidIsbnCount">
/// Editions whose ISBN has a check digit that disagrees with it (fonctionnel/regles-metier/validation-de-l-isbn.md).
/// </param>
public sealed record AdminChecks(int InvalidIsbnCount);
