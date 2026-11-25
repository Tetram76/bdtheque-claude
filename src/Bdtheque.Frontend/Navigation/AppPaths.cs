using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Lists;

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

    // Consultation (#210 to #212 for the records, #603 to #606 for the lists, #608 for the search).

    /// <summary>
    /// The simple search of the consultation, from its header (fonctionnel/structure-de-l-application.md): the records found,
    /// whatever their type, each leading where the consultation leads it.
    /// </summary>
    public const string Search = "/search";

    /// <summary>The list of the albums of the consultation, under which the record of an album is.</summary>
    public const string Albums = AlbumsSection;

    /// <summary>The list of the series of the consultation, under which the record of a series is.</summary>
    public const string Series = SeriesSection;

    /// <summary>The list of the authors of the consultation, under which the record of an author is.</summary>
    public const string Authors = AuthorsSection;

    /// <summary>The public list of the purchase intents, each leading to the record of its album (fonctionnel/structure-de-l-application.md).</summary>
    public const string PurchaseIntents = "/acquisition-intents";

    public const string AdvancedSearch = NotYetImplemented;

    /// <summary>The path of the list of the albums, under which the record of an album is: where the entry « Albums » of the menu is current.</summary>
    public const string AlbumsSection = "/albums";

    /// <summary>The route of the record of an album.</summary>
    public const string AlbumRoute = AlbumsSection + "/{Id:guid}";

    /// <summary>The record of an album (visuel/fiche-album/).</summary>
    public static string Album(Guid id) => $"{AlbumsSection}/{id}";

    /// <summary>
    /// Where a genre leads: the list of the albums of the consultation, filtered on that genre alone, from its first page
    /// (fonctionnel/structure-de-l-application.md): the search, the filters and the entry the list kept are lost.
    /// </summary>
    public static string AlbumsOfGenre(Guid id) => new CatalogAlbumListQuery(GenreId: id).Href();

    /// <summary>The route of the record of a universe, which has no list and no entry in the menu of the consultation.</summary>
    public const string UniverseRoute = "/universes/{Id:guid}";

    /// <summary>The record of a universe (visuel/fiche-univers/).</summary>
    public static string UniverseRecord(Guid id) => $"/universes/{id}";

    /// <summary>The path of the list of the series, under which the record of a series is: where the entry « Séries » of the menu is current.</summary>
    public const string SeriesSection = "/series";

    /// <summary>The route of the record of a series.</summary>
    public const string SeriesRoute = SeriesSection + "/{Id:guid}";

    /// <summary>The record of a series (visuel/fiche-serie/).</summary>
    public static string SeriesRecord(Guid id) => $"{SeriesSection}/{id}";

    /// <summary>
    /// The path of the list of the authors, under which the record of an author is: where the entry
    /// « Auteurs » of the menu is current.
    /// </summary>
    public const string AuthorsSection = "/authors";

    /// <summary>The route of the record of an author.</summary>
    public const string AuthorRoute = AuthorsSection + "/{Id:guid}";

    /// <summary>The record of an author (visuel/fiche-auteur/).</summary>
    public static string AuthorRecord(Guid id) => $"{AuthorsSection}/{id}";

    // Administration (#200 to #209, the settings #610; the private statistics, a later part of the Phase 3).

    /// <summary>
    /// Search of a record to modify, from the header of the administration (visuel/administration/en-tete-et-menu.md): the records
    /// found, whatever their type, each leading to its page of modification.
    /// </summary>
    public const string AdminSearch = "/admin/search";

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
    public const string AdminNewPurchaseIntentRoute = AdminAlbums + "/{AlbumId:guid}/acquisition-intents/new";

    /// <summary>The entry form of a new purchase intent of an album, on the new edition it targets (visuel/saisie-edition/).</summary>
    public static string AdminNewPurchaseIntent(Guid albumId) => $"{AdminAlbums}/{albumId}/acquisition-intents/new";

    /// <summary>Whether a path is that of the entry form of a new purchase intent, which shares its page with the editions.</summary>
    public static bool IsNewPurchaseIntent(string path) =>
        path.TrimEnd('/').EndsWith("/acquisition-intents/new", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The merge of two albums (visuel/fusion/fusion-de-deux-fiches.md), the next step of the dialog of the merge of the list of
    /// the albums: it receives the album and the other one, <c>album</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeAlbums = AdminAlbums + MergeSegment;

    /// <summary>
    /// The segment of the merge of two records, after the list of their type (visuel/fusion/fusion-de-deux-fiches.md).
    /// </summary>
    private const string MergeSegment = "/merge";

    /// <summary>The list of the series of the administration (visuel/listes/).</summary>
    public const string AdminSeries = "/admin/series";

    /// <summary>The entry form of a series (visuel/saisie-serie/).</summary>
    public static string AdminEditSeries(Guid id) => $"{AdminSeries}/{id}";

    /// <summary>
    /// The merge of two series (visuel/fusion/fusion-de-deux-fiches.md): it receives the series and the other one, <c>series</c>
    /// and <c>other</c>.
    /// </summary>
    public const string AdminMergeSeries = AdminSeries + MergeSegment;

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
    public const string AdminPurchaseIntents = "/admin/acquisition-intents";

    /// <summary>The route of the confirmation of the purchase that realizes an intent, addressed by its album.</summary>
    public const string AdminConfirmPurchaseRoute = AdminAlbums + "/{AlbumId:guid}/acquisition-intents/{IntentId:guid}/confirmation";

    /// <summary>
    /// The confirmation of the purchase that realizes an intent (fonctionnel/regles-metier/intention-d-acquisition.md#réalisation-dune-intention):
    /// the entry form of the edition acquired, the one the intent targets or, for an intent on the whole album, its first one.
    /// </summary>
    public static string AdminConfirmPurchase(Guid albumId, Guid id) => $"{AdminAlbums}/{albumId}/acquisition-intents/{id}/confirmation";

    /// <summary>Whether a path is that of the confirmation of a purchase, which shares its page with the editions.</summary>
    public static bool IsPurchaseConfirmation(string path) => IsOfAnIntent(path, "/confirmation");

    /// <summary>The route of the conversion of an intent on the whole album into an intent on a new edition.</summary>
    public const string AdminConvertPurchaseIntentToEditionRoute = AdminAlbums + "/{AlbumId:guid}/acquisition-intents/{IntentId:guid}/edition";

    /// <summary>
    /// The conversion of an intent on the whole album into an intent on a new edition (fonctionnel/regles-metier/intention-d-acquisition.md):
    /// the entry form of the new edition targeted. The conversion of an intent on an edition into one on the whole album, which
    /// deletes the edition, is confirmed in a dialog of the page it is asked from, as a removal.
    /// </summary>
    public static string AdminConvertPurchaseIntentToEdition(Guid albumId, Guid id) => $"{AdminAlbums}/{albumId}/acquisition-intents/{id}/edition";

    /// <summary>
    /// The same conversion, asked from the list of the intents, which it leads back to once saved or cancelled, instead of the
    /// page of the editions of the album (decision of the user, #612).
    /// </summary>
    public static string AdminConvertPurchaseIntentToEditionFromIntents(Guid albumId, Guid id) =>
        $"{AdminConvertPurchaseIntentToEdition(albumId, id)}?{FromParameter}={FromPurchaseIntents}";

    /// <summary>The parameter of the address of an entry form that tells the page it was opened from, when it has several.</summary>
    public const string FromParameter = "from";

    /// <summary>The value of <see cref="FromParameter"/> for the list of the purchase intents.</summary>
    public const string FromPurchaseIntents = "acquisition-intents";

    /// <summary>Whether a path is that of the conversion of an intent into one on a new edition, which shares its page with the editions.</summary>
    public static bool IsPurchaseIntentConversion(string path) => IsOfAnIntent(path, "/edition");

    // A page of an intent: under the intents of its album, ending with what it does.
    private static bool IsOfAnIntent(string path, string ending) =>
        path.Contains("/acquisition-intents/", StringComparison.OrdinalIgnoreCase)
        && path.TrimEnd('/').EndsWith(ending, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The list of the authors of the administration (visuel/listes/), where an author is entered in a side panel over the list
    /// (visuel/listes/saisie-selon-l-entite.md).
    /// </summary>
    public const string AdminAuthors = "/admin/authors";

    /// <summary>The list of the authors with the panel of the author open over it.</summary>
    public static string AdminEditAuthor(Guid id) => $"{AdminAuthors}?edit={id}";

    /// <summary>
    /// The merge of two authors (visuel/fusion/fusion-de-deux-fiches.md): it receives the author and the other one,
    /// <c>author</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeAuthors = AdminAuthors + MergeSegment;

    /// <summary>The list of the publishers of the administration (visuel/listes/).</summary>
    public const string AdminPublishers = "/admin/publishers";

    /// <summary>The entry form of a publisher and of its collections (visuel/saisie-editeur/).</summary>
    public static string AdminEditPublisher(Guid id) => $"{AdminPublishers}/{id}";

    /// <summary>
    /// The merge of two publishers and of their homonymous collections (visuel/fusion/fusion-de-deux-fiches.md): it receives the
    /// publisher and the other one, <c>publisher</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergePublishers = AdminPublishers + MergeSegment;

    /// <summary>
    /// The list of the genres of the administration (visuel/listes/), where a genre is changed in its row and a new one added in
    /// a frame above the list (visuel/listes/saisie-selon-l-entite.md).
    /// </summary>
    public const string AdminGenres = "/admin/genres";

    /// <summary>The list of the genres with the genre changed in its row: the list searched on its label, when it is not on its page.</summary>
    public static string AdminEditGenre(Guid id) => $"{AdminGenres}?edit={id}";

    /// <summary>
    /// The merge of two genres (visuel/fusion/fusion-de-deux-fiches.md): it receives the genre and the other one, <c>genre</c>
    /// and <c>other</c>.
    /// </summary>
    public const string AdminMergeGenres = AdminGenres + MergeSegment;

    /// <summary>
    /// The list of the universes of the administration (visuel/listes/), where a universe is entered in a side panel over the
    /// list (visuel/listes/saisie-selon-l-entite.md).
    /// </summary>
    public const string AdminUniverses = "/admin/universes";

    /// <summary>The list of the universes with the panel of the universe open over it.</summary>
    public static string AdminEditUniverse(Guid id) => $"{AdminUniverses}?edit={id}";

    /// <summary>
    /// The merge of two universes (visuel/fusion/fusion-de-deux-fiches.md): it receives the universe and the other one,
    /// <c>universe</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeUniverses = AdminUniverses + MergeSegment;

    /// <summary>The saved searches of the administration (fonctionnel/recherche-avancee/recherches-enregistrees.md).</summary>
    public const string AdminSavedSearches = NotYetImplemented;

    /// <summary>
    /// The entry form of the setting of the application (modele-metier/parametrage-de-l-application.md), a single record
    /// addressed without identifier.
    /// </summary>
    public const string AdminSettings = "/admin/settings";

    /// <summary>The private statistics of the administration: the indicators of the audience measurement (fonctionnel/mesure-audience.md).</summary>
    public const string AdminStatistics = "/admin/statistics";

    /// <summary>The search of an edition in the external sources, from its ISBN (visuel/administration/contenu.md).</summary>
    public const string AdminIsbnLookup = NotYetImplemented;

    /// <summary>
    /// The text searched in a list that found nothing, which the entry form of a new record on a page of its own takes as its
    /// title or name (visuel/liste-albums/contenu.md, Liste vide): the list offers to create the record looked for.
    /// </summary>
    public const string SearchedParameter = "searched";

    /// <summary>The entry form of a new record (<paramref name="newPath"/>) for the text searched in its list.</summary>
    public static string NewWithSearched(string newPath, string searched) => $"{newPath}?{SearchedParameter}={Uri.EscapeDataString(searched)}";

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

    /// <summary>The whole change log, from the latest entry to the oldest (visuel/journal/journal-des-modifications.md).</summary>
    public const string AdminChangeLog = "/admin/change-log";

    /// <summary>
    /// The page of a record in the consultation, for the records that have one (fonctionnel/structure-de-l-application.md):
    /// an album, a series, an author or a universe; <c>null</c> for any other.
    /// </summary>
    public static string? ConsultationRecord(RecordType type, Guid id) => type switch
    {
        RecordType.Album => Album(id),
        RecordType.Series => SeriesRecord(id),
        RecordType.Author => AuthorRecord(id),
        RecordType.Universe => UniverseRecord(id),
        _ => null,
    };

    /// <summary>The editions whose ISBN has a check digit that disagrees with it (visuel/administration/contenu.md, À vérifier).</summary>
    public const string AdminInvalidIsbns = "/admin/invalid-isbns";

    /// <summary>
    /// The page that modifies a record of the given type (visuel/administration/contenu.md, Dernières modifications). Every
    /// type is named, none falling to a default: a type added without its page is refused, and a type whose page is
    /// implemented cannot be left on <see cref="NotYetImplemented"/>.
    /// </summary>
    /// <param name="parentId">
    /// The record the page of this one is addressed under: the album of an edition or of a purchase intent, the publisher of a
    /// collection. Without it, a record deleted since it was read, there is no page to lead to.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">A value that names no type of record.</exception>
    public static string AdminEdit(RecordType type, Guid id, Guid? parentId) => type switch
    {
        RecordType.Album => AdminEditAlbum(id),
        RecordType.Edition => parentId is { } album ? AdminEditEdition(album, id) : NotYetImplemented,
        RecordType.Series => AdminEditSeries(id),
        // No record of its own: the page of the editions and purchase intents of its album (visuel/listes/saisie-selon-l-entite.md).
        RecordType.PurchaseIntent => parentId is { } album ? AdminAlbumEditions(album) : NotYetImplemented,
        RecordType.Author => AdminEditAuthor(id),
        RecordType.Publisher => AdminEditPublisher(id),
        // Entered with its publisher, without a page of its own (visuel/saisie-editeur/contenu.md).
        RecordType.PublisherCollection => parentId is { } publisher ? AdminEditPublisher(publisher) : NotYetImplemented,
        RecordType.Genre => AdminEditGenre(id),
        RecordType.Universe => AdminEditUniverse(id),
        RecordType.SavedSearch => AdminSavedSearches,
        RecordType.ApplicationSettings => AdminSettings,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown type of record."),
    };
}
