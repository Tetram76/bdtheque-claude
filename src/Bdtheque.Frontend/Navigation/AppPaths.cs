using Bdtheque.Contracts.Enums;

namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// Paths of the pages of the application, which every link uses. A page not implemented yet points
/// to <see cref="NotYetImplemented"/>: the chantier that implements it gives it its own path here,
/// and every link that leads to it follows (suivi-implementation/, Phase 3, pages non implémentées).
/// The pages still to come are thus those whose path is <see cref="NotYetImplemented"/>.
/// </summary>
public static class AppPaths
{
    public const string Home = "/";

    public const string AdminHome = "/admin";

    public const string NotYetImplemented = "/not-yet-implemented";

    /// <summary>Login page of the administration, the only one of its pages open to anyone (Components/Pages, not Pages/Admin).</summary>
    public const string AdminLogin = "/admin/login";

    /// <summary>Logout of the administration, which the last entry of its menu sends.</summary>
    public const string AdminLogout = "/admin/logout";

    /// <summary>The query parameter of the page to return to once signed in: that of the authentication cookie.</summary>
    public const string ReturnUrlParameter = "ReturnUrl";

    /// <summary>Information of the visitor on the audience measurement, and their opposition (fonctionnel/mesure-audience.md).</summary>
    public const string AudienceInformation = "/audience";

    /// <summary>Contextual help (a later part of the Phase 3).</summary>
    public const string Help = NotYetImplemented;

    // Consultation (#210 to #212 for the records; the lists and the search, a later part of the Phase 3).

    public const string Search = NotYetImplemented;

    public const string Albums = NotYetImplemented;

    public const string Series = NotYetImplemented;

    public const string Authors = NotYetImplemented;

    public const string PurchaseIntents = NotYetImplemented;

    public const string AdvancedSearch = NotYetImplemented;

    /// <summary>
    /// The path of the list of the albums once implemented, under which the record of an album is: where the entry
    /// « Albums » of the menu is current.
    /// </summary>
    public const string AlbumsSection = "/albums";

    /// <summary>The route of the record of an album.</summary>
    public const string AlbumRoute = AlbumsSection + "/{Id:guid}";

    /// <summary>The record of an album (visuel/fiche-album/).</summary>
    public static string Album(Guid id) => $"{AlbumsSection}/{id}";

    /// <summary>The series and albums of a genre, presented as the bibliography of an author (fonctionnel/structure-de-l-application.md).</summary>
    public static string GenreWorks(Guid id) => NotYetImplemented;

    /// <summary>The series and albums of a universe, those of its sub-universes included.</summary>
    public static string UniverseWorks(Guid id) => NotYetImplemented;

    /// <summary>The record of a series (#211).</summary>
    public static string SeriesRecord(Guid id) => NotYetImplemented;

    /// <summary>The record of an author (#212).</summary>
    public static string AuthorRecord(Guid id) => NotYetImplemented;

    // Administration (#200 to #209; the settings and the private statistics, a later part of the Phase 3).

    /// <summary>Search of a record to modify, from the header of the administration.</summary>
    public const string AdminSearch = NotYetImplemented;

    /// <summary>A new edition, which starts with the choice of its album (visuel/administration/en-tete-et-menu.md).</summary>
    public const string AdminNewEdition = NotYetImplemented;

    /// <summary>The list of the albums of the administration (visuel/liste-albums/).</summary>
    public const string AdminAlbums = "/admin/albums";

    /// <summary>The entry form of an album (visuel/saisie-album/).</summary>
    public static string AdminEditAlbum(Guid id) => $"{AdminAlbums}/{id}";

    /// <summary>The route of the page of the editions and purchase intents of an album.</summary>
    public const string AdminAlbumEditionsRoute = AdminAlbums + "/{Id:guid}/editions";

    /// <summary>The page of the editions and purchase intents of an album (visuel/editions-album/).</summary>
    public static string AdminAlbumEditions(Guid albumId) => $"{AdminAlbums}/{albumId}/editions";

    /// <summary>The route of the entry form of a new owned edition of an album.</summary>
    public const string AdminNewAlbumEditionRoute = AdminAlbums + "/{AlbumId:guid}/editions/new";

    /// <summary>The entry form of a new owned edition of an album (visuel/saisie-edition/).</summary>
    public static string AdminNewAlbumEdition(Guid albumId) => $"{AdminAlbums}/{albumId}/editions/new";

    /// <summary>The route of the entry form of a new purchase intent of an album, on the new edition it targets.</summary>
    public const string AdminNewPurchaseIntentRoute = AdminAlbums + "/{AlbumId:guid}/purchase-intents/new";

    /// <summary>The entry form of a new purchase intent of an album, on the new edition it targets (visuel/saisie-edition/).</summary>
    public static string AdminNewPurchaseIntent(Guid albumId) => $"{AdminAlbums}/{albumId}/purchase-intents/new";

    /// <summary>Whether a path is that of the entry form of a new purchase intent, which shares its page with the editions.</summary>
    public static bool IsNewPurchaseIntent(string path) =>
        path.TrimEnd('/').EndsWith("/purchase-intents/new", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The next step of the merge of two albums, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the album and the other one, <c>album</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeAlbums = NotYetImplemented;

    /// <summary>The list of the series of the administration (visuel/listes/).</summary>
    public const string AdminSeries = "/admin/series";

    /// <summary>The entry form of a series (visuel/saisie-serie/).</summary>
    public static string AdminEditSeries(Guid id) => $"{AdminSeries}/{id}";

    /// <summary>
    /// The next step of the merge of two series, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the series and the other one, <c>series</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeSeries = NotYetImplemented;

    /// <summary>The list of the editions of the administration (visuel/listes/).</summary>
    public const string AdminEditions = "/admin/editions";

    /// <summary>The route of the entry form of an edition, owned or targeted by a purchase intent.</summary>
    public const string AdminEditEditionRoute = AdminAlbums + "/{AlbumId:guid}/editions/{Id:guid}";

    /// <summary>
    /// The entry form of an edition, addressed by its album (visuel/saisie-edition/): an owned edition, or the edition a purchase
    /// intent targets.
    /// </summary>
    public static string AdminEditEdition(Guid albumId, Guid id) => $"{AdminAlbums}/{albumId}/editions/{id}";

    /// <summary>
    /// The next step of the merge of two editions, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the edition and the other one, <c>edition</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeEditions = NotYetImplemented;

    /// <summary>The list of the purchase intents of the administration (visuel/listes/).</summary>
    public const string AdminPurchaseIntents = "/admin/purchase-intents";

    /// <summary>
    /// The confirmation of the purchase that realizes an intent, addressed by its album
    /// (fonctionnel/regles-metier/intention-d-achat.md#réalisation-dune-intention), which no mockup presents yet.
    /// </summary>
    public static string AdminConfirmPurchase(Guid albumId, Guid id) => NotYetImplemented;

    /// <summary>
    /// The conversion of an intent into one on the other target, an edition or the whole album, addressed by its album
    /// (fonctionnel/regles-metier/intention-d-achat.md), which no mockup presents yet.
    /// </summary>
    public static string AdminConvertPurchaseIntent(Guid albumId, Guid id) => NotYetImplemented;

    /// <summary>
    /// The list of the authors of the administration (visuel/listes/), where an author is entered in a side panel over the list
    /// (visuel/listes/saisie-selon-l-entite.md).
    /// </summary>
    public const string AdminAuthors = "/admin/authors";

    /// <summary>The list of the authors with the panel of the author open over it.</summary>
    public static string AdminEditAuthor(Guid id) => $"{AdminAuthors}?edit={id}";

    /// <summary>
    /// The next step of the merge of two authors, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the author and the other one, <c>author</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeAuthors = NotYetImplemented;

    /// <summary>The list of the publishers of the administration (visuel/listes/).</summary>
    public const string AdminPublishers = "/admin/publishers";

    /// <summary>The entry form of a publisher and of its collections (visuel/saisie-editeur/).</summary>
    public static string AdminEditPublisher(Guid id) => $"{AdminPublishers}/{id}";

    /// <summary>
    /// The next step of the merge of two publishers, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the publisher and the other one, <c>publisher</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergePublishers = NotYetImplemented;

    /// <summary>
    /// The list of the genres of the administration (visuel/listes/), where a genre is changed in its row and a new one added in
    /// a frame above the list (visuel/listes/saisie-selon-l-entite.md).
    /// </summary>
    public const string AdminGenres = "/admin/genres";

    /// <summary>The list of the genres with the genre changed in its row: the list searched on its label, when it is not on its page.</summary>
    public static string AdminEditGenre(Guid id) => $"{AdminGenres}?edit={id}";

    /// <summary>
    /// The next step of the merge of two genres, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the genre and the other one, <c>genre</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeGenres = NotYetImplemented;

    /// <summary>
    /// The list of the universes of the administration (visuel/listes/), where a universe is entered in a side panel over the
    /// list (visuel/listes/saisie-selon-l-entite.md).
    /// </summary>
    public const string AdminUniverses = "/admin/universes";

    /// <summary>The list of the universes with the panel of the universe open over it.</summary>
    public static string AdminEditUniverse(Guid id) => $"{AdminUniverses}?edit={id}";

    /// <summary>
    /// The next step of the merge of two universes, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the universe and the other one, <c>universe</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeUniverses = NotYetImplemented;

    public const string AdminSettings = NotYetImplemented;

    public const string AdminStatistics = NotYetImplemented;

    /// <summary>The search of an edition in the external sources, from its ISBN (visuel/administration/contenu.md).</summary>
    public const string AdminIsbnLookup = NotYetImplemented;

    /// <summary>The entry form of a new album (visuel/saisie-album/).</summary>
    public const string AdminNewAlbum = $"{AdminAlbums}/new";

    /// <summary>The entry form of a new series (visuel/saisie-serie/).</summary>
    public const string AdminNewSeries = $"{AdminSeries}/new";

    /// <summary>The list of the authors with the panel of a new author open over it.</summary>
    public const string AdminNewAuthor = $"{AdminAuthors}?new=open";

    /// <summary>The entry form of a new publisher (visuel/saisie-editeur/).</summary>
    public const string AdminNewPublisher = $"{AdminPublishers}/new";

    /// <summary>The list of the genres with the frame of a new genre open.</summary>
    public const string AdminNewGenre = $"{AdminGenres}?new=open";

    /// <summary>The list of the universes with the panel of a new universe open over it.</summary>
    public const string AdminNewUniverse = $"{AdminUniverses}?new=open";

    /// <summary>The editions whose ISBN has a check digit that disagrees with it (visuel/administration/contenu.md, À vérifier).</summary>
    public const string AdminInvalidIsbns = NotYetImplemented;

    /// <summary>The page that modifies a record of the given type.</summary>
    public static string AdminEdit(RecordType type, Guid id) => type switch
    {
        RecordType.ApplicationSettings => AdminSettings,
        RecordType.Album => AdminEditAlbum(id),
        RecordType.Author => AdminEditAuthor(id),
        RecordType.Genre => AdminEditGenre(id),
        RecordType.Universe => AdminEditUniverse(id),
        _ => NotYetImplemented,
    };
}
