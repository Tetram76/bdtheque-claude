using Bdtheque.Frontend.Security;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Tests.Security;

/// <summary>
/// The single administrator account (contraintes-techniques/architecture-deploiement/authentification.md): its credentials,
/// and the attempts the login accepts before it waits.
/// </summary>
public sealed class AdminSignInTests
{
    private static readonly AdminCredentials Credentials =
        new(Options.Create(new AdminAccountOptions { Login = "admin", Password = "Tr0ub4dor&3" }));

    [Theory]
    [InlineData("admin", "Tr0ub4dor&3", true)]
    [InlineData("admin", "tr0ub4dor&3", false)]
    [InlineData("Admin", "Tr0ub4dor&3", false)]
    [InlineData("admin", "Tr0ub4dor&", false)]
    [InlineData("admin", "", false)]
    [InlineData("", "", false)]
    [InlineData(null, null, false)]
    public void Credentials_AreThoseOfTheAccount_ExactlyAsConfigured(string? login, string? password, bool valid) =>
        Assert.Equal(valid, Credentials.AreValid(login, password));

    [Fact]
    public void Throttle_RefusesAttempts_OnceTheFailuresOfTheWindowAreSpent_UntilTheOldestLeavesIt()
    {
        var clock = new StepClock(new DateTimeOffset(2026, 11, 6, 20, 0, 0, TimeSpan.Zero));
        var throttle = new LoginThrottle(clock);

        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
        {
            Assert.False(throttle.IsBlocked());
            throttle.RecordFailure();
            clock.Now += TimeSpan.FromSeconds(10);
        }

        Assert.True(throttle.IsBlocked());
        // The first failure leaves the window: one more attempt.
        clock.Now = new DateTimeOffset(2026, 11, 6, 20, 0, 0, TimeSpan.Zero) + LoginThrottle.Window + TimeSpan.FromSeconds(1);
        Assert.False(throttle.IsBlocked());
    }

    [Fact]
    public void Throttle_ForgetsTheFailures_OnceSignedIn()
    {
        var throttle = new LoginThrottle(new StepClock(DateTimeOffset.UnixEpoch));
        for (var i = 0; i < LoginThrottle.MaxFailures - 1; i++)
            throttle.RecordFailure();

        throttle.Reset();
        throttle.RecordFailure();

        Assert.False(throttle.IsBlocked());
    }

    private sealed class StepClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
