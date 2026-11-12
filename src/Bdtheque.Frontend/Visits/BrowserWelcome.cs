using Bdtheque.Frontend.Formatting;

namespace Bdtheque.Frontend.Visits;

/// <summary>
/// Whether a request is answered by the welcome screen rather than by its page
/// (choix-implementation/socle/transverse/internationalisation.md, écran d'accueil): what only the browser knows (its time
/// zone, hence its date, and its touch screen) reaches the server by cookies its scripts write, after a page is rendered.
/// The first request of a browser that has not given them yet gets a quick screen whose script writes them, then loads the
/// address again: its first real rendering already knows them, for the dates, the theme of the visit and the audience
/// measurement alike.
/// <para>
/// Only the page a browser navigates to: neither an enhanced navigation of Blazor nor a form it submits (requests of the
/// page itself, whose browser runs the scripts of its pages), nor a medium, nor a client that is not a browser (robots,
/// previews of links), which sends no metadata of the navigation. A browser that refuses the cookies, or runs no script,
/// comes back marked by <see cref="SkipParameter"/>: its page is then rendered as it is, never the screen again and again.
/// </para>
/// </summary>
public sealed class BrowserWelcome(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Query parameter of the request that follows the screen without the cookies: the page, not the screen again.</summary>
    public const string SkipParameter = "welcomed";

    private bool? _isDue;

    /// <summary>Whether the request of the page being rendered is answered by the welcome screen.</summary>
    public bool IsDue => _isDue ??= httpContextAccessor.HttpContext?.Request is { } request && IsDueFor(request);

    /// <summary>
    /// The navigation of a browser to a page (Fetch Metadata, <c>Sec-Fetch-Mode</c> and <c>Sec-Fetch-Dest</c>, which every
    /// browser sends since 2023), by <c>GET</c>, without the cookie of its time zone, nor the mark of a browser that keeps
    /// none.
    /// </summary>
    public static bool IsDueFor(HttpRequest request) =>
        HttpMethods.IsGet(request.Method)
        && request.Headers["Sec-Fetch-Mode"] == "navigate"
        && request.Headers["Sec-Fetch-Dest"] == "document"
        && !request.Cookies.ContainsKey(BrowserTimeZone.CookieName)
        && !request.Query.ContainsKey(SkipParameter);

    /// <summary>The address of the request, marked so that the screen gives way to the page.</summary>
    public static string SkipAddress(HttpRequest request) =>
        request.PathBase + request.Path + QueryString.Create(
            request.Query.SelectMany(parameter => parameter.Value.Select(value => KeyValuePair.Create(parameter.Key, value)))
                .Append(KeyValuePair.Create(SkipParameter, (string?)"")));
}
