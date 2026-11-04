namespace Bdtheque.Frontend.Security;

/// <summary>
/// The options of every cookie <c>frontend</c> writes itself (visit, opposition to the audience measurement): read by no
/// script, sent back for the same site only, and Secure on an HTTPS request only, like the cookies of authentication and
/// antiforgery (choix-implementation/deploiement/frontend-derriere-le-reverse-proxy-du-nas.md) — always Secure, they
/// would never be sent back on the internal deployment in HTTP.
/// </summary>
public static class FrontendCookies
{
    /// <param name="maxAge">How long the browser keeps it; <c>null</c> for a session cookie, forgotten with the session.</param>
    public static CookieOptions For(HttpRequest request, TimeSpan? maxAge = null) => new()
    {
        Path = "/",
        MaxAge = maxAge,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = request.IsHttps,
        IsEssential = true,
    };
}
