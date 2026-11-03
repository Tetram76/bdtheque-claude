using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Bdtheque.Frontend.Tests;

/// <summary>
/// A request rendered statically, whose cookies, user agent and user a test chooses, and whose response runs its
/// starting callbacks when the test starts it, as the server does before sending the headers.
/// </summary>
internal sealed class TestRequest
{
    private readonly StartingResponse _response = new();

    public TestRequest(IReadOnlyDictionary<string, string>? cookies = null, string? userAgent = null, bool authenticated = false)
    {
        Context = new DefaultHttpContext();
        Context.Features.Set<IHttpResponseFeature>(_response);
        Context.Request.Method = HttpMethods.Get;
        if (cookies is { Count: > 0 })
            Context.Request.Headers.Cookie = string.Join("; ", cookies.Select(c => $"{c.Key}={Uri.EscapeDataString(c.Value)}"));
        if (userAgent is not null)
            Context.Request.Headers.UserAgent = userAgent;
        if (authenticated)
            Context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "Cookies"));
        Accessor = new HttpContextAccessor { HttpContext = Context };
    }

    public DefaultHttpContext Context { get; }

    public IHttpContextAccessor Accessor { get; }

    /// <summary>The cookies the response writes, once started.</summary>
    public IEnumerable<string> SetCookies => Context.Response.Headers.SetCookie.OfType<string>();

    /// <summary>The cookie of the given name the response writes, without its attributes; <c>null</c> if none.</summary>
    public string? SetCookie(string name) =>
        SetCookies.Where(c => c.StartsWith($"{name}=", StringComparison.Ordinal))
            .Select(c => Uri.UnescapeDataString(c[(name.Length + 1)..].Split(';')[0]))
            .LastOrDefault();

    public void MarkStarted() => _response.MarkStarted();

    public Task StartResponseAsync() => _response.StartAsync();

    private sealed class StartingResponse : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _starting = [];
        private bool _started;

        public override bool HasStarted => _started;

        public override void OnStarting(Func<object, Task> callback, object state) => _starting.Add((callback, state));

        public void MarkStarted() => _started = true;

        public async Task StartAsync()
        {
            // Last registered, first run: the order of ASP.NET Core.
            for (var i = _starting.Count - 1; i >= 0; i--)
                await _starting[i].Callback(_starting[i].State);
            _started = true;
        }
    }
}
