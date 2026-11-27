using Bdtheque.Frontend.Security;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Tests.Security;

/// <summary>
/// The session of the administrator inside an interactive circuit, which keeps the principal it was opened with for as
/// long as it lives: the same limits as the cookie, its activity standing for the requests
/// (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md).
/// </summary>
public sealed class AdminCircuitSessionTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Circuit_WithoutActivityForTheIdleTimeout_IsNoLongerTheSession()
    {
        var clock = new StepClock(Start);
        var (sessions, circuit) = Create(clock);
        var principal = sessions.SignIn();

        // Up to the timeout included, as the cookie.
        clock.Now = Start + AdminSessions.IdleTimeout;
        Assert.True(circuit.IsCurrent(principal));

        clock.Now = Start + AdminSessions.IdleTimeout + TimeSpan.FromMilliseconds(1);
        Assert.False(circuit.IsCurrent(principal));
    }

    [Fact]
    public void Circuit_Active_KeepsTheSession_UpToItsMaximumLifetime()
    {
        var clock = new StepClock(Start);
        var (sessions, circuit) = Create(clock);
        var principal = sessions.SignIn();

        for (var elapsed = TimeSpan.FromHours(1); elapsed < AdminSessions.MaximumLifetime; elapsed += TimeSpan.FromHours(1))
        {
            clock.Now = Start + elapsed;
            circuit.RecordActivity();
            Assert.True(circuit.IsCurrent(principal));
        }

        clock.Now = Start + AdminSessions.MaximumLifetime;
        circuit.RecordActivity();
        Assert.False(circuit.IsCurrent(principal));
    }

    [Fact]
    public void Circuit_OfARevokedSession_IsNoLongerTheSession()
    {
        var clock = new StepClock(Start);
        var opened = new AdminSessions(Options.Create(Account("Tr0ub4dor&3")), clock).SignIn();
        var (_, circuit) = Create(clock, Account("An0ther-password"));

        Assert.False(circuit.IsCurrent(opened));
    }

    private static AdminAccountOptions Account(string password) => new() { Login = "admin", Password = password };

    private static (AdminSessions Sessions, AdminCircuitSession Circuit) Create(TimeProvider clock, AdminAccountOptions? account = null)
    {
        var sessions = new AdminSessions(Options.Create(account ?? Account("Tr0ub4dor&3")), clock);
        return (sessions, new AdminCircuitSession(sessions, clock));
    }

    private sealed class StepClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
