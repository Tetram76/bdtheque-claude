using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Security;

/// <summary>
/// The session of the administrator inside an interactive circuit (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md).
/// A circuit keeps the principal it was opened with for as long as it lives, out of reach of the cookie: the same limits
/// apply to it, its own activity (events of the interface, calls from the browser) standing for the requests that slide
/// the cookie. One per circuit.
/// </summary>
public sealed class AdminCircuitSession(AdminSessions sessions, TimeProvider clock)
{
    private long _lastActivity = clock.GetUtcNow().ToUnixTimeMilliseconds();

    public void RecordActivity() => Interlocked.Exchange(ref _lastActivity, clock.GetUtcNow().ToUnixTimeMilliseconds());

    /// <summary>
    /// Whether <paramref name="principal"/> is still a session of the account, and the circuit still in use: idle up to the
    /// timeout included, as the cookie, which ASP.NET Core takes for expired only once its expiry is past.
    /// </summary>
    public bool IsCurrent(ClaimsPrincipal principal) =>
        sessions.IsCurrent(principal)
        && clock.GetUtcNow() - DateTimeOffset.FromUnixTimeMilliseconds(Interlocked.Read(ref _lastActivity)) <= AdminSessions.IdleTimeout;
}

/// <summary>How often a circuit checks its session again: an ended session leaves it within that time at most.</summary>
public sealed class AdminCircuitOptions
{
    public TimeSpan RevalidationInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>The activity of the circuit, from the browser to the server, that keeps its session alive.</summary>
public sealed class AdminCircuitActivityHandler(AdminCircuitSession session) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) =>
        context =>
        {
            session.RecordActivity();
            return next(context);
        };
}

/// <summary>
/// The authentication state of a circuit, checked again at each interval: an ended session makes the circuit anonymous,
/// and the pages of the administration lead back to the login, as a request would without its cookie.
/// </summary>
public sealed class AdminCircuitAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    AdminCircuitSession session,
    IOptions<AdminCircuitOptions> options)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => options.Value.RevalidationInterval;

    protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken) =>
        Task.FromResult(authenticationState.User.Identity?.IsAuthenticated != true || session.IsCurrent(authenticationState.User));
}
