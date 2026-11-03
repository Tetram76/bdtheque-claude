using Bdtheque.Frontend.Security;

namespace Bdtheque.Frontend.Visits;

/// <summary>
/// The visit a request continues, kept in its session cookie (<see cref="BrowserVisit"/>): read from the request, then
/// continued by the theme of the visit and by the audience measurement, each in turn. The cookie is written once, with
/// the visit as the request leaves it, when its response starts, and only if it differs from the one the request
/// brought: a single place writes it, whatever continues it.
/// </summary>
public sealed class VisitCookie(IHttpContextAccessor httpContextAccessor)
{
    private BrowserVisit? _brought;
    private BrowserVisit? _current;
    private bool _read;
    private bool _writing;

    /// <summary>The visit as the request has left it so far; <c>null</c> while none was brought nor started.</summary>
    public BrowserVisit? Current
    {
        get
        {
            Read();
            return _current;
        }
    }

    /// <summary>Continues the visit: its cookie will carry it.</summary>
    /// <exception cref="InvalidOperationException">
    /// The response has started (e.g. an interactive circuit, which keeps the request that opened it): nothing can be written.
    /// </exception>
    public void Set(BrowserVisit visit)
    {
        Read();
        _current = visit;
        if (_writing || visit == _brought)
            return;

        var context = Request();
        if (context.Response.HasStarted)
            throw new InvalidOperationException("The visit is continued within a request whose response has not started.");
        context.Response.OnStarting(() =>
        {
            // Without an expiry: a session cookie, which the browser forgets with its session, as a visit ends.
            if (_current is { } current && current != _brought)
                context.Response.Cookies.Append(BrowserVisit.CookieName, current.Format(), FrontendCookies.For(context.Request));
            return Task.CompletedTask;
        });
        _writing = true;
    }

    private void Read()
    {
        if (_read)
            return;
        _brought = _current = BrowserVisit.Parse(Request().Request.Cookies[BrowserVisit.CookieName]);
        _read = true;
    }

    private HttpContext Request() => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("The visit is continued for a request, rendered statically.");
}
