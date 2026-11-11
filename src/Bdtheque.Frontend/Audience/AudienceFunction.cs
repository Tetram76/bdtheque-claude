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
    public static readonly AudienceFunction Home = new("home", AudienceFunctionKind.Screen);

    /// <summary>The record of an album (visuel/fiche-album/), with the album it presents.</summary>
    public static readonly AudienceFunction AlbumRecord = new("album", AudienceFunctionKind.Screen);

    // The actions of the record, each a request of the record whose parameters are its state (Navigation/AlbumRecordQuery.cs).

    /// <summary>A visual shown in the viewer: the one of a thumbnail, the previous or the next.</summary>
    public static readonly AudienceFunction AlbumRecordVisual = new("album.visual", AudienceFunctionKind.Action);

    /// <summary>The record of a series (visuel/fiche-serie/), with the series it presents.</summary>
    public static readonly AudienceFunction SeriesRecord = new("series", AudienceFunctionKind.Screen);

    /// <summary>
    /// The record of an author (visuel/fiche-auteur/), with the author it presents. Unfolding its series, one or all, is no
    /// action: a disclosure of the page itself, showing what it already holds.
    /// </summary>
    public static readonly AudienceFunction AuthorRecord = new("author", AudienceFunctionKind.Screen);

    public static readonly AudienceFunction AdminHome = new("admin.home", AudienceFunctionKind.Screen);

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
    // without filters nor navigation by initial.

    public static readonly AudienceFunction AdminPublishersSearch = new("admin.publishers.search", AudienceFunctionKind.Action);

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

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/EditionListQuery.cs);
    // without navigation by initial.

    public static readonly AudienceFunction AdminEditionsSearch = new("admin.editions.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminEditionsFilter = new("admin.editions.filter", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminEditionsPage = new("admin.editions.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the deletion of an edition, opened from its row.</summary>
    public static readonly AudienceFunction AdminEditionsDeletion = new("admin.editions.deletion", AudienceFunctionKind.Action);

    /// <summary>The deletion of an edition, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminEditionsDeleted = new("admin.editions.deleted", AudienceFunctionKind.Action);

    /// <summary>The dialog of the merge of an edition, opened from its row, with the search of the other edition.</summary>
    public static readonly AudienceFunction AdminEditionsMerge = new("admin.editions.merge", AudienceFunctionKind.Action);

    /// <summary>The list of the purchase intents of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminPurchaseIntents = new("admin.purchase-intents", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/PurchaseIntentListQuery.cs);
    // without filters nor navigation by initial, an intent being no record to merge.

    public static readonly AudienceFunction AdminPurchaseIntentsSearch = new("admin.purchase-intents.search", AudienceFunctionKind.Action);

    public static readonly AudienceFunction AdminPurchaseIntentsPage = new("admin.purchase-intents.page", AudienceFunctionKind.Action);

    /// <summary>The dialog of the removal of an intent, opened from its row.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentsRemoval = new("admin.purchase-intents.removal", AudienceFunctionKind.Action);

    /// <summary>The removal of an intent, once confirmed and done.</summary>
    public static readonly AudienceFunction AdminPurchaseIntentsRemoved = new("admin.purchase-intents.removed", AudienceFunctionKind.Action);

    /// <summary>The list of the genres of the administration (visuel/listes/liste-par-liste.md).</summary>
    public static readonly AudienceFunction AdminGenres = new("admin.genres", AudienceFunctionKind.Screen);

    // The actions of the list, each a request of the list whose parameters are its criteria (Lists/GenreListQuery.cs);
    // without filters nor navigation by initial.

    public static readonly AudienceFunction AdminGenresSearch = new("admin.genres.search", AudienceFunctionKind.Action);

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

    public static readonly IReadOnlyList<AudienceFunction> Followed =
    [
        AlbumRecord, AlbumRecordVisual,
        SeriesRecord, AuthorRecord,
        Home, AdminHome, AdminLogin, AdminSignedIn, AdminSignedOut, AudienceInformation, AudienceOppositionWithdrawn, AdminAlbums,
        AdminAlbumsSearch, AdminAlbumsFilter, AdminAlbumsInitial, AdminAlbumsPage, AdminAlbumsDeletion, AdminAlbumsDeleted, AdminAlbumsMerge,
        AdminAlbum, AdminNewAlbum, AdminAlbumCreated, AdminAlbumSaved, AdminAlbumAuthorCreated, AdminAlbumGenreCreated, AdminAlbumUniverseCreated,
        AdminAlbumChanges,
        AdminSeries, AdminSeriesSearch, AdminSeriesFilter, AdminSeriesInitial, AdminSeriesPage, AdminSeriesDeletion, AdminSeriesDeleted,
        AdminSeriesMerge, AdminSeriesForm, AdminNewSeriesForm, AdminSeriesCreated, AdminSeriesSaved, AdminSeriesAuthorCreated,
        AdminSeriesGenreCreated, AdminSeriesUniverseCreated, AdminSeriesChanges, AdminAuthors, AdminAuthorsSearch, AdminAuthorsInitial, AdminAuthorsPage, AdminAuthorsDeletion, AdminAuthorsDeleted,
        AdminAuthorsMerge, AdminAuthorsEdit, AdminAuthorsNew, AdminAuthorSaved, AdminAuthorCreated, AdminPublishers, AdminPublishersSearch, AdminPublishersPage, AdminPublishersDeletion, AdminPublishersDeleted,
        AdminPublishersMerge, AdminPublisherForm, AdminNewPublisherForm, AdminPublisherCreated, AdminPublisherSaved, AdminPublisherChanges,
        AdminEditions, AdminEditionsSearch, AdminEditionsFilter, AdminEditionsPage, AdminEditionsDeletion,
        AdminEditionsDeleted, AdminEditionsMerge, AdminPurchaseIntents, AdminPurchaseIntentsSearch, AdminPurchaseIntentsPage,
        AdminPurchaseIntentsRemoval, AdminPurchaseIntentsRemoved, AdminGenres, AdminGenresSearch, AdminGenresPage, AdminGenresDeletion,
        AdminGenresDeleted, AdminGenresMerge, AdminGenresEdit, AdminGenresNew, AdminGenreSaved, AdminGenreCreated, AdminUniverses,
        AdminUniversesSearch, AdminUniversesPage, AdminUniversesDeletion, AdminUniversesDeleted, AdminUniversesMerge, AdminUniversesEdit,
        AdminUniversesNew, AdminUniverseSaved, AdminUniverseCreated, AdminAlbumEditions, AdminAlbumEditionsEditionDeletion,
        AdminAlbumEditionsEditionDeleted, AdminAlbumEditionsIntentRemoval, AdminAlbumEditionsIntentRemoved,
        AdminAlbumEditionsAlbumIntentSet, AdminEditionForm, AdminNewEditionForm, AdminEditionCreated, AdminEditionSaved, AdminEditionChanges,
        AdminPurchaseIntentForm, AdminNewPurchaseIntentForm, AdminPurchaseIntentCreated, AdminPurchaseIntentSaved, AdminPurchaseIntentChanges,
    ];
}
