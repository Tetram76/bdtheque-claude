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

    public static readonly IReadOnlyList<AudienceFunction> Followed =
        [Home, AdminHome, AdminLogin, AdminSignedIn, AdminSignedOut, AudienceInformation, AudienceOppositionWithdrawn];
}
