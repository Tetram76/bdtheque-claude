namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// Paths of the pages of the application, which every link uses. A page not implemented yet points
/// to <see cref="NotYetImplemented"/>: the chantier that implements it gives it its own path here,
/// and every link that leads to it follows (suivi-implementation.md, Phase 3, pages non implémentées).
/// The pages still to come are thus those whose path is <see cref="NotYetImplemented"/>.
/// </summary>
public static class AppPaths
{
    public const string Home = "/";

    public const string AdminHome = "/admin";

    public const string NotYetImplemented = "/not-yet-implemented";

    /// <summary>Login page of the administration (#199).</summary>
    public const string AdminLogin = NotYetImplemented;

    /// <summary>Logout of the administration (#199).</summary>
    public const string AdminLogout = NotYetImplemented;

    /// <summary>Contextual help (a later part of the Phase 3).</summary>
    public const string Help = NotYetImplemented;

    // Consultation (#210 to #212 for the records; the lists and the search, a later part of the Phase 3).

    public const string Search = NotYetImplemented;

    public const string Albums = NotYetImplemented;

    public const string Series = NotYetImplemented;

    public const string Authors = NotYetImplemented;

    public const string PurchaseIntents = NotYetImplemented;

    // Administration (#200 to #209; the settings and the private statistics, a later part of the Phase 3).

    /// <summary>Search of a record to modify, from the header of the administration.</summary>
    public const string AdminSearch = NotYetImplemented;

    /// <summary>A new edition, which starts with the choice of its album (visuel/administration.md § En-tête et menu).</summary>
    public const string AdminNewEdition = NotYetImplemented;

    public const string AdminAlbums = NotYetImplemented;

    public const string AdminSeries = NotYetImplemented;

    public const string AdminEditions = NotYetImplemented;

    public const string AdminPurchaseIntents = NotYetImplemented;

    public const string AdminAuthors = NotYetImplemented;

    public const string AdminPublishers = NotYetImplemented;

    public const string AdminGenres = NotYetImplemented;

    public const string AdminUniverses = NotYetImplemented;

    public const string AdminSettings = NotYetImplemented;

    public const string AdminStatistics = NotYetImplemented;
}
