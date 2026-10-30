using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Theming;

/// <summary>Chooses the theme in which a request is presented.</summary>
public interface IThemeSelector
{
    Theme Current { get; }
}

/// <summary>
/// The theme of the visit of the browser (<see cref="VisitTheme"/>), read from its cookie and written back when the
/// request continues the visit otherwise (new visit, new date, theme drawn again). Chosen while the page is rendered
/// statically, by its root component, before the response starts (choix-implementation/socle.md § Thèmes : mise en
/// œuvre): an interactive circuit has no request, and never asks.
/// </summary>
public sealed class VisitThemeSelector(IHttpContextAccessor httpContextAccessor, BrowserTimeZone timeZone, TimeProvider clock) : IThemeSelector
{
    private Theme? _current;

    public Theme Current => _current ??= Select();

    private Theme Select()
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("The theme of the visit is chosen for a request, rendered statically.");
        var visit = BrowserVisit.Parse(context.Request.Cookies[BrowserVisit.CookieName]);
        var browserToday = timeZone.IsKnown ? timeZone.Today(clock) : (DateOnly?)null;
        var continued = VisitTheme.Continue(
            visit, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), browserToday, ThemeCatalog.Offered, Random.Shared.Next);

        if (continued != visit)
            context.Response.Cookies.Append(BrowserVisit.CookieName, continued.Format(), new CookieOptions
            {
                // Without an expiry: a session cookie, which the browser forgets with its session, as a visit ends.
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                // Secure on an HTTPS request only, like the other cookies of frontend (choix-implementation/deploiement.md
                // § Frontend derrière le reverse proxy du NAS).
                Secure = context.Request.IsHttps,
                IsEssential = true,
            });
        return VisitTheme.Of(continued, ThemeCatalog.Offered);
    }
}
