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

    /// <summary>Contextual help (a later part of the Phase 3).</summary>
    public const string Help = NotYetImplemented;
}
