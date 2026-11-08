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

    public static readonly IReadOnlyList<AudienceFunction> Followed =
    [
        Home, AdminHome, AdminLogin, AdminSignedIn, AdminSignedOut, AudienceInformation, AudienceOppositionWithdrawn, AdminAlbums,
        AdminAlbumsSearch, AdminAlbumsFilter, AdminAlbumsInitial, AdminAlbumsPage, AdminAlbumsDeletion, AdminAlbumsDeleted, AdminAlbumsMerge,
        AdminSeries, AdminSeriesSearch, AdminSeriesFilter, AdminSeriesInitial, AdminSeriesPage, AdminSeriesDeletion, AdminSeriesDeleted,
        AdminSeriesMerge, AdminAuthors, AdminAuthorsSearch, AdminAuthorsInitial, AdminAuthorsPage, AdminAuthorsDeletion, AdminAuthorsDeleted,
        AdminAuthorsMerge, AdminPublishers, AdminPublishersSearch, AdminPublishersPage, AdminPublishersDeletion, AdminPublishersDeleted,
        AdminPublishersMerge,
    ];
}
