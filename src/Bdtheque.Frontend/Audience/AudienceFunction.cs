namespace Bdtheque.Frontend.Audience;

/// <summary>A function of the web interface the audience measurement follows (fonctionnel/mesure-audience.md, Fonctions suivies).</summary>
public enum AudienceFunctionKind
{
    /// <summary>A screen displayed, with the record it presents, if any.</summary>
    Screen,

    /// <summary>An action triggered, with its parameters, if any.</summary>
    Action,
}

/// <param name="Key">
/// What the measurement records of the function, and counts it by: never renamed nor given to another function, its
/// usages being kept, and counted, under it.
/// </param>
public sealed record AudienceFunction(string Key, AudienceFunctionKind Kind);

/// <summary>
/// The functions the audience measurement follows: the screens and actions implemented, each screen's chantier adding its
/// own. The administration presents them all, a function never used with null indicators (fonctionnel/mesure-audience.md,
/// Indicateurs), the API knowing only the functions used.
/// </summary>
public static class AudienceFunctions
{
    /// <summary>The record of an album (visuel/fiche-album/), with the album it presents.</summary>
    public static readonly AudienceFunction AlbumRecord = new("album", AudienceFunctionKind.Screen);

    // The actions of the record, each a request of the record whose parameters are its state (Navigation/AlbumRecordQuery.cs).

    /// <summary>A visual shown in the viewer: the one of a thumbnail, the previous or the next.</summary>
    public static readonly AudienceFunction AlbumRecordVisual = new("album.visual", AudienceFunctionKind.Action);

    /// <summary>The list of the albums of the consultation, entry « Albums » of its menu.</summary>
    public static readonly AudienceFunction Albums = new("albums", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/CatalogAlbumListQuery.cs).

    public static readonly AudienceFunction AlbumsSearch = new("albums.search", AudienceFunctionKind.Action);

    /// <summary>The cross filters and the possession, which limits the list to the collection.</summary>
    public static readonly AudienceFunction AlbumsFilter = new("albums.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AlbumsInitial = new("albums.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AlbumsPage = new("albums.page", AudienceFunctionKind.Action);

    /// <summary>The record of a series (visuel/fiche-serie/), with the series it presents.</summary>
    public static readonly AudienceFunction SeriesRecord = new("series", AudienceFunctionKind.Screen);

    /// <summary>
    /// The list of the series of the consultation, entry « Séries » of its menu: its key apart from that of the record of a
    /// series, « series », which a list of series cannot share.
    /// </summary>
    public static readonly AudienceFunction SeriesList = new("series.list", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/CatalogSeriesListQuery.cs).

    public static readonly AudienceFunction SeriesListSearch = new("series.list.search", AudienceFunctionKind.Action);

    /// <summary>The cross filters and the possession, which limits the list to the collection.</summary>
    public static readonly AudienceFunction SeriesListFilter = new("series.list.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction SeriesListInitial = new("series.list.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction SeriesListPage = new("series.list.page", AudienceFunctionKind.Action);

    /// <summary>
    /// The record of an author (visuel/fiche-auteur/), with the author it presents. Unfolding its series, one or all, is no
    /// action: a disclosure of the page itself, showing what it already holds.
    /// </summary>
    public static readonly AudienceFunction AuthorRecord = new("author", AudienceFunctionKind.Screen);

    /// <summary>
    /// The record of a universe (visuel/fiche-univers/), with the universe it presents. Unfolding its series, one or all, is no
    /// action, as in the record of an author.
    /// </summary>
    public static readonly AudienceFunction UniverseRecord = new("universe", AudienceFunctionKind.Screen);

    /// <summary>The list of the authors of the consultation, entry « Auteurs » of its menu.</summary>
    public static readonly AudienceFunction Authors = new("authors", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/CatalogAuthorListQuery.cs).

    public static readonly AudienceFunction AuthorsSearch = new("authors.search", AudienceFunctionKind.Action);

    /// <summary>The possession, its only filter, which limits the list to the collection.</summary>
    public static readonly AudienceFunction AuthorsFilter = new("authors.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AuthorsInitial = new("authors.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AuthorsPage = new("authors.page", AudienceFunctionKind.Action);

    /// <summary>The public list of the purchase intents, entry « Intentions d'acquisition » of the menu of the consultation.</summary>
    public static readonly AudienceFunction PurchaseIntents = new("purchase-intents", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/CatalogPurchaseIntentListQuery.cs).

    public static readonly AudienceFunction PurchaseIntentsSearch = new("purchase-intents.search", AudienceFunctionKind.Action);

    /// <summary>The cross filters of the albums, applied to the album of each intent.</summary>
    public static readonly AudienceFunction PurchaseIntentsFilter = new("purchase-intents.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction PurchaseIntentsInitial = new("purchase-intents.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction PurchaseIntentsPage = new("purchase-intents.page", AudienceFunctionKind.Action);

    /// <summary>
    /// The simple search of the consultation, from its header (fonctionnel/structure-de-l-application.md): the page of its
    /// results, every type of record found.
    /// </summary>
    public static readonly AudienceFunction Search = new("search", AudienceFunctionKind.Screen);

    /// <summary>A search launched, from the header or from the page of its results, with its text.</summary>
    public static readonly AudienceFunction SearchQuery = new("search.query", AudienceFunctionKind.Action);

    /// <summary>
    /// The search of a record to modify, from the header of the administration (visuel/administration/en-tete-et-menu.md): the
    /// page of its results.
    /// </summary>
    public static readonly AudienceFunction AdminSearch = new("admin.search", AudienceFunctionKind.Screen);

    /// <summary>A search launched, from the header or from the page of its results, with its text.</summary>
    public static readonly AudienceFunction AdminSearchQuery = new("admin.search.query", AudienceFunctionKind.Action);

    /// <summary>The information of the visitor on the audience measurement, and their choice.</summary>
    public static readonly AudienceFunction AudienceInformation = new("audience", AudienceFunctionKind.Screen);

    /// <summary>
    /// The withdrawal of the opposition of a visitor, from which their visits are recorded again; the opposition itself is
    /// not recorded, the visitor refusing it.
    /// </summary>
    public static readonly AudienceFunction AudienceOppositionWithdrawn = new("audience.opposition-withdrawn", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminLogin = new("admin.login", AudienceFunctionKind.Screen);

    /// <summary>The login of the administrator, without any parameter: neither the login nor, above all, the password.</summary>
    public static readonly AudienceFunction AdminSignedIn = new("admin.signed-in", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminSignedOut = new("admin.signed-out", AudienceFunctionKind.Action);

    /// <summary>The list of the albums of the administration (visuel/liste-albums/contenu.md).</summary>
    public static readonly AudienceFunction AdminAlbums = new("admin.albums", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/AlbumListQuery.cs).

    public static readonly AudienceFunction AdminAlbumsSearch = new("admin.albums.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminAlbumsFilter = new("admin.albums.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminAlbumsInitial = new("admin.albums.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminAlbumsPage = new("admin.albums.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of an album, opened from its row.</summary>
    public static readonly AudienceFunction AdminAlbumsDeletion = new("admin.albums.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of an album, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminAlbumsDeleted = new("admin.albums.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of an album, opened from its row, with the search of the other album.</summary>
    public static readonly AudienceFunction AdminAlbumsMerge = new("admin.albums.merge", AudienceFunctionKind.Action);

    /// <summary>The entry form of an album, with the album it presents (visuel/saisie-album/contenu.md).</summary>
    public static readonly AudienceFunction AdminAlbum = new("admin.album", AudienceFunctionKind.Screen);

    /// <summary>The entry form of a new album.</summary>
    public static readonly AudienceFunction AdminNewAlbum = new("admin.album.new", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page.

    /// <summary>The creation of an album, with the album created.</summary>
    public static readonly AudienceFunction AdminAlbumCreated = new("admin.album.created", AudienceFunctionKind.Action);

    /// <summary>The saving of the modification of an album, with the album saved.</summary>
    public static readonly AudienceFunction AdminAlbumSaved = new("admin.album.saved", AudienceFunctionKind.Action);

    /// <summary>The creation of an author on the spot, from the form of an album, with the author created.</summary>
    public static readonly AudienceFunction AdminAlbumAuthorCreated = new("admin.album.author-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a genre on the spot, from the form of an album, with the genre created.</summary>
    public static readonly AudienceFunction AdminAlbumGenreCreated = new("admin.album.genre-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a universe on the spot, from the form of an album, with the universe created.</summary>
    public static readonly AudienceFunction AdminAlbumUniverseCreated = new("admin.album.universe-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a series on the spot, from the form of an album, with the series created.</summary>
    public static readonly AudienceFunction AdminAlbumSeriesCreated = new("admin.album.series-created", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the album having been modified in the meantime.</summary>
    public static readonly AudienceFunction AdminAlbumChanges = new("admin.album.changes", AudienceFunctionKind.Action);

    /// <summary>The list of the series of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminSeries = new("admin.series", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/SeriesListQuery.cs).

    public static readonly AudienceFunction AdminSeriesSearch = new("admin.series.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminSeriesFilter = new("admin.series.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminSeriesInitial = new("admin.series.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminSeriesPage = new("admin.series.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of a series, opened from its row.</summary>
    public static readonly AudienceFunction AdminSeriesDeletion = new("admin.series.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a series, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminSeriesDeleted = new("admin.series.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of a series, opened from its row, with the search of the other series.</summary>
    public static readonly AudienceFunction AdminSeriesMerge = new("admin.series.merge", AudienceFunctionKind.Action);

    /// <summary>The entry form of a series, with the series it presents (visuel/saisie-serie/contenu.md).</summary>
    public static readonly AudienceFunction AdminSeriesForm = new("admin.series-form", AudienceFunctionKind.Screen);

    /// <summary>The entry form of a new series.</summary>
    public static readonly AudienceFunction AdminNewSeriesForm = new("admin.series-form.new", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page.

    /// <summary>The creation of a series, with the series created.</summary>
    public static readonly AudienceFunction AdminSeriesCreated = new("admin.series-form.created", AudienceFunctionKind.Action);

    /// <summary>The saving of the modification of a series, with the series saved.</summary>
    public static readonly AudienceFunction AdminSeriesSaved = new("admin.series-form.saved", AudienceFunctionKind.Action);

    /// <summary>The creation of an author on the spot, from the form of a series.</summary>
    public static readonly AudienceFunction AdminSeriesAuthorCreated = new("admin.series-form.author-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a genre on the spot, from the form of a series.</summary>
    public static readonly AudienceFunction AdminSeriesGenreCreated = new("admin.series-form.genre-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a universe on the spot, from the form of a series.</summary>
    public static readonly AudienceFunction AdminSeriesUniverseCreated = new("admin.series-form.universe-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a publisher on the spot, from the form of a series.</summary>
    public static readonly AudienceFunction AdminSeriesPublisherCreated = new("admin.series-form.publisher-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a collection of the publisher on the spot, from the form of a series.</summary>
    public static readonly AudienceFunction AdminSeriesCollectionCreated = new("admin.series-form.collection-created", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the series having been modified in the meantime.</summary>
    public static readonly AudienceFunction AdminSeriesChanges = new("admin.series-form.changes", AudienceFunctionKind.Action);

    /// <summary>The list of the authors of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminAuthors = new("admin.authors", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/AuthorListQuery.cs); the
    // list has no filters.

    public static readonly AudienceFunction AdminAuthorsSearch = new("admin.authors.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminAuthorsInitial = new("admin.authors.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminAuthorsPage = new("admin.authors.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of an author, opened from its row.</summary>
    public static readonly AudienceFunction AdminAuthorsDeletion = new("admin.authors.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of an author, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminAuthorsDeleted = new("admin.authors.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of an author, opened from its row, with the search of the other author.</summary>
    public static readonly AudienceFunction AdminAuthorsMerge = new("admin.authors.merge", AudienceFunctionKind.Action);

    /// <summary>The panel of an author opened on it (visuel/listes/saisie-selon-l-entite.md), from its « Modifier ».</summary>
    public static readonly AudienceFunction AdminAuthorsEdit = new("admin.authors.edit", AudienceFunctionKind.Action);

    /// <summary>The panel of a new author opened, from « Nouvel auteur ».</summary>
    public static readonly AudienceFunction AdminAuthorsNew = new("admin.authors.new", AudienceFunctionKind.Action);

    /// <summary>The change of an author, once saved.</summary>
    public static readonly AudienceFunction AdminAuthorSaved = new("admin.author.saved", AudienceFunctionKind.Action);

    /// <summary>A new author, once created.</summary>
    public static readonly AudienceFunction AdminAuthorCreated = new("admin.author.created", AudienceFunctionKind.Action);

    /// <summary>The list of the publishers of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminPublishers = new("admin.publishers", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/PublisherListQuery.cs);
    // without filters.

    public static readonly AudienceFunction AdminPublishersSearch = new("admin.publishers.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminPublishersInitial = new("admin.publishers.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminPublishersPage = new("admin.publishers.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of a publisher, opened from its row.</summary>
    public static readonly AudienceFunction AdminPublishersDeletion = new("admin.publishers.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a publisher, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminPublishersDeleted = new("admin.publishers.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of a publisher, opened from its row, with the search of the other publisher.</summary>
    public static readonly AudienceFunction AdminPublishersMerge = new("admin.publishers.merge", AudienceFunctionKind.Action);

    /// <summary>The entry form of a publisher and of its collections, with the publisher it presents (visuel/saisie-editeur/contenu.md).</summary>
    public static readonly AudienceFunction AdminPublisherForm = new("admin.publisher-form", AudienceFunctionKind.Screen);

    /// <summary>The entry form of a new publisher.</summary>
    public static readonly AudienceFunction AdminNewPublisherForm = new("admin.publisher-form.new", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page.

    /// <summary>The creation of a publisher, with the publisher created.</summary>
    public static readonly AudienceFunction AdminPublisherCreated = new("admin.publisher-form.created", AudienceFunctionKind.Action);

    /// <summary>The saving of the modification of a publisher, with the publisher saved.</summary>
    public static readonly AudienceFunction AdminPublisherSaved = new("admin.publisher-form.saved", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the publisher having been modified in the meantime.</summary>
    public static readonly AudienceFunction AdminPublisherChanges = new("admin.publisher-form.changes", AudienceFunctionKind.Action);

    /// <summary>The list of the editions of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminEditions = new("admin.editions", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/EditionListQuery.cs).

    public static readonly AudienceFunction AdminEditionsSearch = new("admin.editions.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminEditionsInitial = new("admin.editions.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminEditionsFilter = new("admin.editions.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminEditionsPage = new("admin.editions.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of an edition, opened from its row.</summary>
    public static readonly AudienceFunction AdminEditionsDeletion = new("admin.editions.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of an edition, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminEditionsDeleted = new("admin.editions.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of an edition, opened from its row, with the search of the other edition.</summary>
    public static readonly AudienceFunction AdminEditionsMerge = new("admin.editions.merge", AudienceFunctionKind.Action);

    /// <summary>
    /// The list of the editions whose ISBN has an inconsistent check digit (visuel/listes/liste-par-liste.md), from the block
    /// « À vérifier » of the home page of the administration.
    /// </summary>
    public static readonly AudienceFunction AdminInvalidIsbns = new("admin.invalid-isbns", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/InvalidIsbnListQuery.cs).

    public static readonly AudienceFunction AdminInvalidIsbnsSearch = new("admin.invalid-isbns.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminInvalidIsbnsInitial = new("admin.invalid-isbns.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminInvalidIsbnsFilter = new("admin.invalid-isbns.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminInvalidIsbnsPage = new("admin.invalid-isbns.page", AudienceFunctionKind.Action);

    /// <summary>The ISBN of an edition corrected from its row, once done.</summary>
    public static readonly AudienceFunction AdminInvalidIsbnCorrected = new("admin.invalid-isbns.corrected", AudienceFunctionKind.Action);

    /// <summary>The error of the ISBN of an edition accepted from its row, once done.</summary>
    public static readonly AudienceFunction AdminInvalidIsbnAccepted = new("admin.invalid-isbns.accepted", AudienceFunctionKind.Action);

    /// <summary>The error of the ISBN of an edition accepted no more, from its row, once done.</summary>
    public static readonly AudienceFunction AdminInvalidIsbnRevoked = new("admin.invalid-isbns.revoked", AudienceFunctionKind.Action);

    /// <summary>The list of the purchase intents of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminPurchaseIntents = new("admin.purchase-intents", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/PurchaseIntentListQuery.cs);
    // without filters, an intent being no record to merge.

    public static readonly AudienceFunction AdminPurchaseIntentsSearch = new("admin.purchase-intents.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminPurchaseIntentsInitial = new("admin.purchase-intents.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminPurchaseIntentsPage = new("admin.purchase-intents.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the removal of an intent, opened from its row.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentsRemoval = new("admin.purchase-intents.removal", AudienceFunctionKind.Action);

    /// <summary>The removal of an intent, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentsRemoved = new("admin.purchase-intents.removed", AudienceFunctionKind.Action);

    /// <summary>The dialog of the conversion of an intent on an edition into one on the whole album, opened from its row.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentsConversion = new("admin.purchase-intents.conversion", AudienceFunctionKind.Action);

    /// <summary>The conversion of an intent on an edition into one on the whole album, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentsConverted = new("admin.purchase-intents.converted", AudienceFunctionKind.Action);

    /// <summary>The list of the genres of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminGenres = new("admin.genres", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/GenreListQuery.cs);
    // without filters.

    public static readonly AudienceFunction AdminGenresSearch = new("admin.genres.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminGenresInitial = new("admin.genres.initial", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminGenresPage = new("admin.genres.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of a genre, opened from its row.</summary>
    public static readonly AudienceFunction AdminGenresDeletion = new("admin.genres.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a genre, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminGenresDeleted = new("admin.genres.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of a genre, opened from its row, with the search of the other genre.</summary>
    public static readonly AudienceFunction AdminGenresMerge = new("admin.genres.merge", AudienceFunctionKind.Action);

    /// <summary>A genre changed in its row (visuel/listes/saisie-selon-l-entite.md), from its « Modifier ».</summary>
    public static readonly AudienceFunction AdminGenresEdit = new("admin.genres.edit", AudienceFunctionKind.Action);

    /// <summary>The dialog of a new genre opened, from « Nouveau genre ».</summary>
    public static readonly AudienceFunction AdminGenresNew = new("admin.genres.new", AudienceFunctionKind.Action);

    /// <summary>The change of a genre, once saved.</summary>
    public static readonly AudienceFunction AdminGenreSaved = new("admin.genre.saved", AudienceFunctionKind.Action);

    /// <summary>A new genre, once added.</summary>
    public static readonly AudienceFunction AdminGenreCreated = new("admin.genre.created", AudienceFunctionKind.Action);

    /// <summary>The list of the universes of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminUniverses = new("admin.universes", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/UniverseListQuery.cs);
    // without filters nor navigation by initial.

    public static readonly AudienceFunction AdminUniversesSearch = new("admin.universes.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminUniversesPage = new("admin.universes.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of a universe, opened from its row.</summary>
    public static readonly AudienceFunction AdminUniversesDeletion = new("admin.universes.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a universe, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminUniversesDeleted = new("admin.universes.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of a universe, opened from its row, with the search of the other universe.</summary>
    public static readonly AudienceFunction AdminUniversesMerge = new("admin.universes.merge", AudienceFunctionKind.Action);

    /// <summary>The panel of a universe opened on it (visuel/listes/saisie-selon-l-entite.md), from its « Modifier ».</summary>
    public static readonly AudienceFunction AdminUniversesEdit = new("admin.universes.edit", AudienceFunctionKind.Action);

    /// <summary>The panel of a new universe opened, from « Nouvel univers ».</summary>
    public static readonly AudienceFunction AdminUniversesNew = new("admin.universes.new", AudienceFunctionKind.Action);

    /// <summary>The change of a universe, once saved.</summary>
    public static readonly AudienceFunction AdminUniverseSaved = new("admin.universe.saved", AudienceFunctionKind.Action);

    /// <summary>A new universe, once created.</summary>
    public static readonly AudienceFunction AdminUniverseCreated = new("admin.universe.created", AudienceFunctionKind.Action);

    /// <summary>The parent of a universe created on the spot from its panel, once created with it, with the parent created.</summary>
    public static readonly AudienceFunction AdminUniverseParentCreated = new("admin.universe.parent-created", AudienceFunctionKind.Action);

    /// <summary>The page of the editions and purchase intents of an album, with the album it presents (visuel/editions-album/contenu.md).</summary>
    public static readonly AudienceFunction AdminAlbumEditions = new("admin.album-editions", AudienceFunctionKind.Screen);

    // The actions of the page: a dialog opened, a request of the page whose parameters are the dialog
    // (Navigation/AlbumEditionsQuery.cs), or a write done.
    /// <summary>The dialog of the deletion of an edition, opened from its row.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsEditionDeletion = new("admin.album-editions.edition-deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of an edition, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsEditionDeleted = new("admin.album-editions.edition-deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the removal of an intent, opened from its row.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsIntentRemoval = new("admin.album-editions.intent-removal", AudienceFunctionKind.Action);

    /// <summary>The removal of an intent, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsIntentRemoved = new("admin.album-editions.intent-removed", AudienceFunctionKind.Action);

    /// <summary>The dialog of the conversion of an intent on an edition into one on the whole album, opened from its row.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsIntentConversion =
        new("admin.album-editions.intent-conversion", AudienceFunctionKind.Action);

    /// <summary>The conversion of an intent on an edition into one on the whole album, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsIntentConverted =
        new("admin.album-editions.intent-converted", AudienceFunctionKind.Action);

    /// <summary>A new intent on the whole album, set at once from the choice of the new intents.</summary>
    public static readonly AudienceFunction AdminAlbumEditionsAlbumIntentSet = new("admin.album-editions.album-intent-set", AudienceFunctionKind.Action);

    /// <summary>The entry form of an owned edition, with the edition it presents (visuel/saisie-edition/contenu.md).</summary>
    public static readonly AudienceFunction AdminEditionForm = new("admin.edition-form", AudienceFunctionKind.Screen);

    /// <summary>The entry form of a new owned edition, with the album it is added to.</summary>
    public static readonly AudienceFunction AdminNewEditionForm = new("admin.edition-form.new", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page.

    /// <summary>The creation of an owned edition, with the edition created.</summary>
    public static readonly AudienceFunction AdminEditionCreated = new("admin.edition-form.created", AudienceFunctionKind.Action);

    /// <summary>The saving of the modification of an owned edition, with the edition saved.</summary>
    public static readonly AudienceFunction AdminEditionSaved = new("admin.edition-form.saved", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, an edition modified meanwhile, with the edition.</summary>
    public static readonly AudienceFunction AdminEditionChanges = new("admin.edition-form.changes", AudienceFunctionKind.Action);

    /// <summary>The creation of a publisher on the spot, from the form of an owned edition.</summary>
    public static readonly AudienceFunction AdminEditionPublisherCreated = new("admin.edition-form.publisher-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a collection of the publisher on the spot, from the form of an owned edition.</summary>
    public static readonly AudienceFunction AdminEditionCollectionCreated = new("admin.edition-form.collection-created", AudienceFunctionKind.Action);

    /// <summary>The entry form of the edition a purchase intent targets, with the intent it presents.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentForm = new("admin.purchase-intent-form", AudienceFunctionKind.Screen);

    /// <summary>The entry form of a new purchase intent on a new edition, with the album it is set on.</summary>
    public static readonly AudienceFunction AdminNewPurchaseIntentForm = new("admin.purchase-intent-form.new", AudienceFunctionKind.Screen);

    /// <summary>The creation of a purchase intent on a new edition, with the album it is set on.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentCreated = new("admin.purchase-intent-form.created", AudienceFunctionKind.Action);

    /// <summary>The saving of the edition a purchase intent targets, with the intent.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentSaved = new("admin.purchase-intent-form.saved", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the edition a purchase intent targets modified meanwhile, with the intent.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentChanges = new("admin.purchase-intent-form.changes", AudienceFunctionKind.Action);

    /// <summary>The creation of a publisher on the spot, from the form of the edition a purchase intent targets.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentPublisherCreated =
        new("admin.purchase-intent-form.publisher-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a collection of the publisher on the spot, from the form of the edition a purchase intent targets.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentCollectionCreated =
        new("admin.purchase-intent-form.collection-created", AudienceFunctionKind.Action);

    // The visuals of the edition, written as soon as they are added, ordered, retyped or deleted (visuel/saisie-edition/contenu.md),
    // each with the visual concerned, or the edition for an arrangement.

    /// <summary>A visual added to an owned edition, with the visual.</summary>
    public static readonly AudienceFunction AdminEditionVisualAdded = new("admin.edition-form.visual-added", AudienceFunctionKind.Action);

    /// <summary>The visuals of an owned edition ordered anew, with the edition.</summary>
    public static readonly AudienceFunction AdminEditionVisualsArranged = new("admin.edition-form.visuals-arranged", AudienceFunctionKind.Action);

    /// <summary>The type of a visual of an owned edition changed, with the visual.</summary>
    public static readonly AudienceFunction AdminEditionVisualTypeChanged = new("admin.edition-form.visual-type-changed", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of an owned edition asked, its confirmation shown, with the visual.</summary>
    public static readonly AudienceFunction AdminEditionVisualDeletion = new("admin.edition-form.visual-deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of an owned edition, once confirmed and done, with the visual.</summary>
    public static readonly AudienceFunction AdminEditionVisualDeleted = new("admin.edition-form.visual-deleted", AudienceFunctionKind.Action);

    /// <summary>A visual added to the edition a purchase intent targets, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentVisualAdded = new("admin.purchase-intent-form.visual-added", AudienceFunctionKind.Action);

    /// <summary>The visuals of the edition a purchase intent targets ordered anew, with the edition.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentVisualsArranged =
        new("admin.purchase-intent-form.visuals-arranged", AudienceFunctionKind.Action);

    /// <summary>The type of a visual of the edition a purchase intent targets changed, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentVisualTypeChanged =
        new("admin.purchase-intent-form.visual-type-changed", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of the edition a purchase intent targets asked, its confirmation shown, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentVisualDeletion =
        new("admin.purchase-intent-form.visual-deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of the edition a purchase intent targets, once confirmed and done, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentVisualDeleted =
        new("admin.purchase-intent-form.visual-deleted", AudienceFunctionKind.Action);

    /// <summary>
    /// The confirmation of the purchase that realizes a purchase intent, with the intent it presents
    /// (fonctionnel/regles-metier/intention-d-acquisition.md#réalisation-dune-intention).
    /// </summary>
    public static readonly AudienceFunction AdminPurchaseConfirmation = new("admin.purchase-confirmation", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page.

    /// <summary>The purchase confirmed, with the intent it realizes.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmed = new("admin.purchase-confirmation.confirmed", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the album of the intent modified meanwhile, with the intent.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationChanges = new("admin.purchase-confirmation.changes", AudienceFunctionKind.Action);

    /// <summary>The creation of a publisher on the spot, from the confirmation of a purchase.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationPublisherCreated =
        new("admin.purchase-confirmation.publisher-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a collection of the publisher on the spot, from the confirmation of a purchase.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationCollectionCreated =
        new("admin.purchase-confirmation.collection-created", AudienceFunctionKind.Action);

    /// <summary>A visual added to the edition acquired, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationVisualAdded =
        new("admin.purchase-confirmation.visual-added", AudienceFunctionKind.Action);

    /// <summary>The visuals of the edition acquired ordered anew, with the edition.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationVisualsArranged =
        new("admin.purchase-confirmation.visuals-arranged", AudienceFunctionKind.Action);

    /// <summary>The type of a visual of the edition acquired changed, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationVisualTypeChanged =
        new("admin.purchase-confirmation.visual-type-changed", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of the edition acquired asked, its confirmation shown, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationVisualDeletion =
        new("admin.purchase-confirmation.visual-deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of the edition acquired, once confirmed and done, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseConfirmationVisualDeleted =
        new("admin.purchase-confirmation.visual-deleted", AudienceFunctionKind.Action);

    /// <summary>
    /// The conversion of a purchase intent on the whole album into an intent on a new edition, with the intent it presents
    /// (fonctionnel/regles-metier/intention-d-acquisition.md).
    /// </summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversion = new("admin.purchase-intent-conversion", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page.

    /// <summary>The intent converted into an intent on the new edition, with the intent converted.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConverted = new("admin.purchase-intent-conversion.converted", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the album of the intent modified meanwhile, with the intent.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionChanges =
        new("admin.purchase-intent-conversion.changes", AudienceFunctionKind.Action);

    /// <summary>The creation of a publisher on the spot, from the conversion of an intent into one on an edition.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionPublisherCreated =
        new("admin.purchase-intent-conversion.publisher-created", AudienceFunctionKind.Action);

    /// <summary>The creation of a collection of the publisher on the spot, from the conversion of an intent into one on an edition.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionCollectionCreated =
        new("admin.purchase-intent-conversion.collection-created", AudienceFunctionKind.Action);

    /// <summary>A visual added to the new edition targeted, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionVisualAdded =
        new("admin.purchase-intent-conversion.visual-added", AudienceFunctionKind.Action);

    /// <summary>The visuals of the new edition targeted ordered anew.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionVisualsArranged =
        new("admin.purchase-intent-conversion.visuals-arranged", AudienceFunctionKind.Action);

    /// <summary>The type of a visual of the new edition targeted changed, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionVisualTypeChanged =
        new("admin.purchase-intent-conversion.visual-type-changed", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of the new edition targeted asked, its confirmation shown, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionVisualDeletion =
        new("admin.purchase-intent-conversion.visual-deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of a visual of the new edition targeted, once confirmed and done, with the visual.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentConversionVisualDeleted =
        new("admin.purchase-intent-conversion.visual-deleted", AudienceFunctionKind.Action);

    /// <summary>
    /// The entry form of the setting of the application (modele-metier/parametrage-de-l-application.md), a single record, which
    /// it presents without designating it.
    /// </summary>
    public static readonly AudienceFunction AdminSettings = new("admin.settings", AudienceFunctionKind.Screen);

    // The actions of the form, interactive, each recorded in the visit of the prerendering of its page, without parameter: the
    // setting is a single record.

    /// <summary>The saving of the modification of the setting.</summary>
    public static readonly AudienceFunction AdminSettingsSaved = new("admin.settings.saved", AudienceFunctionKind.Action);

    /// <summary>The changes of the user shown, the setting having been modified in the meantime.</summary>
    public static readonly AudienceFunction AdminSettingsChanges = new("admin.settings.changes", AudienceFunctionKind.Action);

    /// <summary>
    /// The merge of two genres (visuel/fusion/fusion-de-deux-fiches.md), reached from the dialog of the list, which it presents
    /// without designating the genres.
    /// </summary>
    public static readonly AudienceFunction AdminGenreMerge = new("admin.genre-merge", AudienceFunctionKind.Screen);

    // The merge, interactive, recorded in the visit of the prerendering of its page, once done.

    /// <summary>The merge of the two genres, confirmed and done.</summary>
    public static readonly AudienceFunction AdminGenreMerged = new("admin.genre-merge.merged", AudienceFunctionKind.Action);

    /// <summary>The merge of two authors, as that of two genres.</summary>
    public static readonly AudienceFunction AdminAuthorMerge = new("admin.author-merge", AudienceFunctionKind.Screen);

    /// <summary>The merge of the two authors, confirmed and done.</summary>
    public static readonly AudienceFunction AdminAuthorMerged = new("admin.author-merge.merged", AudienceFunctionKind.Action);

    public static readonly IReadOnlyList<AudienceFunction> Followed =
    [
        AlbumRecord, AlbumRecordVisual,
        Albums, AlbumsSearch, AlbumsFilter, AlbumsInitial, AlbumsPage,
        SeriesRecord, SeriesList, SeriesListSearch, SeriesListFilter, SeriesListInitial, SeriesListPage, AuthorRecord, UniverseRecord,
        Authors, AuthorsSearch, AuthorsFilter, AuthorsInitial, AuthorsPage,
        PurchaseIntents, PurchaseIntentsSearch, PurchaseIntentsFilter, PurchaseIntentsInitial, PurchaseIntentsPage,
        Search, SearchQuery, AdminSearch, AdminSearchQuery, AdminLogin, AdminSignedIn, AdminSignedOut, AudienceInformation, AudienceOppositionWithdrawn, AdminAlbums,
        AdminAlbumsSearch, AdminAlbumsFilter, AdminAlbumsInitial, AdminAlbumsPage, AdminAlbumsDeletion, AdminAlbumsDeleted, AdminAlbumsMerge,
        AdminAlbum, AdminNewAlbum, AdminAlbumCreated, AdminAlbumSaved, AdminAlbumAuthorCreated, AdminAlbumGenreCreated, AdminAlbumUniverseCreated, AdminAlbumSeriesCreated,
        AdminAlbumChanges,
        AdminSeries, AdminSeriesSearch, AdminSeriesFilter, AdminSeriesInitial, AdminSeriesPage, AdminSeriesDeletion, AdminSeriesDeleted,
        AdminSeriesMerge, AdminSeriesForm, AdminNewSeriesForm, AdminSeriesCreated, AdminSeriesSaved, AdminSeriesAuthorCreated,
        AdminSeriesGenreCreated, AdminSeriesUniverseCreated, AdminSeriesPublisherCreated, AdminSeriesCollectionCreated, AdminSeriesChanges, AdminAuthors, AdminAuthorsSearch, AdminAuthorsInitial, AdminAuthorsPage, AdminAuthorsDeletion, AdminAuthorsDeleted,
        AdminAuthorsMerge, AdminAuthorsEdit, AdminAuthorsNew, AdminAuthorSaved, AdminAuthorCreated, AdminPublishers, AdminPublishersSearch, AdminPublishersInitial, AdminPublishersPage, AdminPublishersDeletion, AdminPublishersDeleted,
        AdminPublishersMerge, AdminPublisherForm, AdminNewPublisherForm, AdminPublisherCreated, AdminPublisherSaved, AdminPublisherChanges,
        AdminEditions, AdminEditionsSearch, AdminEditionsInitial, AdminEditionsFilter, AdminEditionsPage, AdminEditionsDeletion,
        AdminEditionsDeleted, AdminEditionsMerge, AdminInvalidIsbns, AdminInvalidIsbnsSearch, AdminInvalidIsbnsInitial, AdminInvalidIsbnsFilter,
        AdminInvalidIsbnsPage, AdminInvalidIsbnCorrected, AdminInvalidIsbnAccepted, AdminInvalidIsbnRevoked, AdminPurchaseIntents, AdminPurchaseIntentsSearch, AdminPurchaseIntentsInitial, AdminPurchaseIntentsPage,
        AdminPurchaseIntentsRemoval, AdminPurchaseIntentsRemoved, AdminPurchaseIntentsConversion, AdminPurchaseIntentsConverted, AdminGenres, AdminGenresSearch, AdminGenresInitial, AdminGenresPage, AdminGenresDeletion,
        AdminGenresDeleted, AdminGenresMerge, AdminGenresEdit, AdminGenresNew, AdminGenreSaved, AdminGenreCreated, AdminUniverses,
        AdminUniversesSearch, AdminUniversesPage, AdminUniversesDeletion, AdminUniversesDeleted, AdminUniversesMerge, AdminUniversesEdit,
        AdminUniversesNew, AdminUniverseSaved, AdminUniverseCreated, AdminUniverseParentCreated, AdminAlbumEditions, AdminAlbumEditionsEditionDeletion,
        AdminAlbumEditionsEditionDeleted, AdminAlbumEditionsIntentRemoval, AdminAlbumEditionsIntentRemoved, AdminAlbumEditionsIntentConversion,
        AdminAlbumEditionsIntentConverted,
        AdminAlbumEditionsAlbumIntentSet, AdminEditionForm, AdminNewEditionForm, AdminEditionCreated, AdminEditionSaved, AdminEditionChanges,
        AdminEditionPublisherCreated, AdminEditionCollectionCreated, AdminPurchaseIntentForm, AdminNewPurchaseIntentForm,
        AdminPurchaseIntentCreated, AdminPurchaseIntentSaved, AdminPurchaseIntentChanges, AdminPurchaseIntentPublisherCreated,
        AdminPurchaseIntentCollectionCreated,
        AdminEditionVisualAdded, AdminEditionVisualsArranged, AdminEditionVisualTypeChanged, AdminEditionVisualDeletion, AdminEditionVisualDeleted,
        AdminPurchaseIntentVisualAdded, AdminPurchaseIntentVisualsArranged, AdminPurchaseIntentVisualTypeChanged, AdminPurchaseIntentVisualDeletion,
        AdminPurchaseIntentVisualDeleted,
        AdminPurchaseConfirmation, AdminPurchaseConfirmed, AdminPurchaseConfirmationChanges, AdminPurchaseConfirmationPublisherCreated,
        AdminPurchaseConfirmationCollectionCreated, AdminPurchaseConfirmationVisualAdded, AdminPurchaseConfirmationVisualsArranged,
        AdminPurchaseConfirmationVisualTypeChanged, AdminPurchaseConfirmationVisualDeletion, AdminPurchaseConfirmationVisualDeleted,
        AdminPurchaseIntentConversion, AdminPurchaseIntentConverted, AdminPurchaseIntentConversionChanges,
        AdminPurchaseIntentConversionPublisherCreated, AdminPurchaseIntentConversionCollectionCreated, AdminPurchaseIntentConversionVisualAdded,
        AdminPurchaseIntentConversionVisualsArranged, AdminPurchaseIntentConversionVisualTypeChanged, AdminPurchaseIntentConversionVisualDeletion,
        AdminPurchaseIntentConversionVisualDeleted,
        AdminSettings, AdminSettingsSaved, AdminSettingsChanges,
        AdminGenreMerge, AdminGenreMerged, AdminAuthorMerge, AdminAuthorMerged,
    ];
}
