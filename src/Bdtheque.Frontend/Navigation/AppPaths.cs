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

    /// <summary>The entry form of an album (#205).</summary>
    public static string AdminEditAlbum(Guid id) => NotYetImplemented;

    /// <summary>The page of the editions and purchase intents of an album (#204).</summary>
    public static string AdminAlbumEditions(Guid albumId) => NotYetImplemented;

    /// <summary>
    /// The next step of the merge of two albums, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the album and the other one, <c>album</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeAlbums = NotYetImplemented;

    /// <summary>The list of the series of the administration (visuel/listes/).</summary>
    public const string AdminSeries = "/admin/series";

    /// <summary>The entry form of a series (#206).</summary>
    public static string AdminEditSeries(Guid id) => NotYetImplemented;

    /// <summary>
    /// The next step of the merge of two series, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the series and the other one, <c>series</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeSeries = NotYetImplemented;

    public const string AdminEditions = NotYetImplemented;

    public const string AdminPurchaseIntents = NotYetImplemented;

    /// <summary>The list of the authors of the administration (visuel/listes/).</summary>
    public const string AdminAuthors = "/admin/authors";

    /// <summary>The entry form of an author (#207).</summary>
    public static string AdminEditAuthor(Guid id) => NotYetImplemented;

    /// <summary>
    /// The next step of the merge of two authors, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the author and the other one, <c>author</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergeAuthors = NotYetImplemented;

    /// <summary>The list of the publishers of the administration (visuel/listes/).</summary>
    public const string AdminPublishers = "/admin/publishers";

    /// <summary>The entry form of a publisher and of its collections (#208).</summary>
    public static string AdminEditPublisher(Guid id) => NotYetImplemented;

    /// <summary>
    /// The next step of the merge of two publishers, whose presentation awaits the terms of the merge
    /// (fonctionnel/structure-de-l-application.md): it receives the publisher and the other one, <c>publisher</c> and <c>other</c>.
    /// </summary>
    public const string AdminMergePublishers = NotYetImplemented;

    public const string AdminGenres = NotYetImplemented;

    public const string AdminUniverses = NotYetImplemented;

    public const string AdminSettings = NotYetImplemented;

    public const string AdminStatistics = NotYetImplemented;

    /// <summary>The search of an edition in the external sources, from its ISBN (visuel/administration/contenu.md).</summary>
    public const string AdminIsbnLookup = NotYetImplemented;

    public const string AdminNewAlbum = NotYetImplemented;

    public const string AdminNewSeries = NotYetImplemented;

    public const string AdminNewAuthor = NotYetImplemented;

    public const string AdminNewPublisher = NotYetImplemented;

    public const string AdminNewGenre = NotYetImplemented;

    public const string AdminNewUniverse = NotYetImplemented;

    /// <summary>The editions whose ISBN has a check digit that disagrees with it (visuel/administration/contenu.md, À vérifier).</summary>
    public const string AdminInvalidIsbns = NotYetImplemented;

    /// <summary>The page that modifies a record of the given type.</summary>
    public static string AdminEdit(RecordType type, Guid id) => type == RecordType.ApplicationSettings ? AdminSettings : NotYetImplemented;
}
