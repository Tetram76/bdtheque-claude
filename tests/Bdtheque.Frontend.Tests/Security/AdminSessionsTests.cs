using System.Security.Claims;
using Bdtheque.Frontend.Security;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Tests.Security;

/// <summary>
/// The sessions of the administrator (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md): their
/// maximum lifetime, and their revocation by a change of the settings of the account.
/// </summary>
public sealed class AdminSessionsTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Session_IsCurrent_UntilItsMaximumLifetime()
    {
        var clock = new StepClock(Start);
        var sessions = Sessions(Account(), clock);
        var session = sessions.SignIn();

        clock.Now = Start + AdminSessions.MaximumLifetime - TimeSpan.FromSeconds(1);
        Assert.True(sessions.IsCurrent(session));

        clock.Now = Start + AdminSessions.MaximumLifetime;
        Assert.False(sessions.IsCurrent(session));
    }

    [Theory]
    [InlineData("admin", "An0ther-password", null)]
    [InlineData("admin", "Tr0ub4dor&3", "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ")]
    [InlineData("root", "Tr0ub4dor&3", null)]
    public void Session_IsRevoked_OnceTheSettingsOfTheAccountChange(string login, string password, string? secret)
    {
        var clock = new StepClock(Start);
        var session = Sessions(Account(), clock).SignIn();

        var restarted = Sessions(new AdminAccountOptions { Login = login, Password = password, TotpSecret = secret }, clock);

        Assert.False(restarted.IsCurrent(session));
        Assert.True(Sessions(Account(), clock).IsCurrent(session));
    }

    [Fact]
    public void Principal_WithoutTheClaimsOfASession_IsNotCurrent()
    {
        var sessions = Sessions(Account(), new StepClock(Start));
        var forged = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "Cookies"));

        Assert.False(sessions.IsCurrent(forged));
    }

    private static AdminAccountOptions Account() => new() { Login = "admin", Password = "Tr0ub4dor&3" };

    private static AdminSessions Sessions(AdminAccountOptions account, TimeProvider clock) => new(Options.Create(account), clock);

    private sealed class StepClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
