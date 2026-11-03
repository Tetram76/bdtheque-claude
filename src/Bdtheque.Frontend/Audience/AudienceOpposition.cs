using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Audience;

/// <summary>
/// The opposition of a visitor to the audience measurement (fonctionnel/mesure-audience.md, Information et opposition):
/// kept by their browser (<see cref="CookieName"/>) until they come back on it, no visit of theirs being recorded
/// meanwhile. Opposing leaves nothing of the measurement in the browser: its visit goes on, for its theme only.
/// </summary>
public sealed class AudienceOpposition(IHttpContextAccessor httpContextAccessor, VisitCookie visitCookie)
{
    public const string CookieName = "Bdtheque.AudienceOpposition";

    /// <summary>
    /// A year, below the 13 months a tracer may live (fonctionnel/mesure-audience.md, Durées de conservation), like the
    /// cookie of the time zone; never extended by the visits that follow, the lifetime of a tracer counting from its deposit.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    private bool? _opposed;

    /// <summary>Whether the visitor opposes the measurement, as the request leaves their choice.</summary>
    public bool IsOpposed => _opposed ??= Request().Request.Cookies.ContainsKey(CookieName);

    public void Oppose()
    {
        var context = Request();
        context.Response.Cookies.Append(CookieName, "1", Options(context, Lifetime));
        context.Response.Cookies.Delete(AudienceDevice.TouchScreenCookieName, Options(context, maxAge: null));
        if (visitCookie.Current is { AudienceVisitId: not null } visit)
            visitCookie.Set(visit with { AudienceVisitId = null });
        _opposed = true;
    }

    public void Withdraw()
    {
        var context = Request();
        context.Response.Cookies.Delete(CookieName, Options(context, maxAge: null));
        _opposed = false;
    }

    private static CookieOptions Options(HttpContext context, TimeSpan? maxAge) => new()
    {
        Path = "/",
        MaxAge = maxAge,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        // Secure on an HTTPS request only, like the other cookies of frontend (choix-implementation/deploiement.md
        // § Frontend derrière le reverse proxy du NAS).
        Secure = context.Request.IsHttps,
        IsEssential = true,
    };

    private HttpContext Request() => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("The opposition to the audience measurement is read for a request, rendered statically.");
}
