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
/// Only a navigation to a page, by <c>GET</c>, asking for HTML: neither an enhanced navigation of Blazor nor a form it
/// submits (requests of a page already displayed, whose scripts have run), nor a medium, nor a client that does not ask for
/// a page. A browser that refuses the cookies, or runs no script, comes back marked by <see cref="SkipParameter"/>: its
/// page is then rendered as it is, never the screen again and again.
/// </para>
/// </summary>
public sealed class BrowserWelcome(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Query parameter of the request that follows the screen without the cookies: the page, not the screen again.</summary>
    public const string SkipParameter = "welcomed";

    // The flag of the Accept header by which blazor.web.js asks for the page of an enhanced navigation.
    private const string EnhancedNavigation = "blazor-enhanced-nav=on";

    private bool? _isDue;

    /// <summary>Whether the request of the page being rendered is answered by the welcome screen.</summary>
    public bool IsDue => _isDue ??= httpContextAccessor.HttpContext?.Request is { } request && IsDueFor(request);

    /// <summary>
    /// The navigation of a browser to a page, by <c>GET</c>, without the cookie of its time zone, nor the mark of a browser
    /// that keeps none.
    /// </summary>
    public static bool IsDueFor(HttpRequest request) =>
        HttpMethods.IsGet(request.Method)
        && IsNavigationToAPage(request)
        && !request.Cookies.ContainsKey(BrowserTimeZone.CookieName)
        && !request.Query.ContainsKey(SkipParameter);

    /// <summary>The address of the request, marked so that the screen gives way to the page.</summary>
    public static string SkipAddress(HttpRequest request) =>
        request.PathBase + request.Path + QueryString.Create(
            request.Query.SelectMany(parameter => parameter.Value.Select(value => KeyValuePair.Create(parameter.Key, value)))
                .Append(KeyValuePair.Create(SkipParameter, (string?)"")));

    // Asking for HTML, which neither a medium nor a client that does not ask for a page does, and not an enhanced navigation.
    // The metadata of a navigation (Fetch Metadata, Sec-Fetch-Mode and Sec-Fetch-Dest) tells it outright, when the browser
    // sends it: to a secure origin only (HTTPS, localhost), never to the NAS reached in HTTP on the internal network
    // (W3C Fetch Metadata Request Headers: a potentially trustworthy URL), where the Accept header alone decides.
    private static bool IsNavigationToAPage(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (!accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
            || accept.Contains(EnhancedNavigation, StringComparison.OrdinalIgnoreCase))
            return false;
        var mode = request.Headers[SecFetchMode];
        var destination = request.Headers[SecFetchDest];
        return (mode.Count == 0 && destination.Count == 0) || (mode == "navigate" && destination == "document");
    }

    private const string SecFetchMode = "Sec-Fetch-Mode";
    private const string SecFetchDest = "Sec-Fetch-Dest";
}
