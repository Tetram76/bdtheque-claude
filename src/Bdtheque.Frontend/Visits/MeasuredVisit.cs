namespace Bdtheque.Frontend.Visits;

/// <summary>
/// A visit of the audience measurement, and whether it is that of the administrator (an authenticated session) or of a
/// visitor (fonctionnel/mesure-audience.md, Administrateur et visiteurs): set when it opens, never changed, so that a
/// change of the authentication (login, logout, end of the session) goes on in another one.
/// </summary>
public sealed record MeasuredVisit(Guid Id, bool OfAdministrator);
