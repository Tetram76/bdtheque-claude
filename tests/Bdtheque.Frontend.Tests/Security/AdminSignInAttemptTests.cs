using Bdtheque.Frontend.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Tests.Security;

/// <summary>
/// The decision on a sign-in to the administration, and what the log says of it: the page refuses without telling which
/// of the three was wrong, the log tells the administrator which one was (gestion-projet/gouvernance/qualite-du-code.md,
/// the log as important as the application).
/// </summary>
public sealed class AdminSignInAttemptTests
{
    private const string Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    // 287082 is the code of the step [30, 60[ of the test vectors of RFC 6238.
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(59);

    [Fact]
    public void RightCredentialsAndCode_AreAccepted_AndLogged()
    {
        var (signIn, log) = Create(Secret);

        Assert.Equal(AdminSignInOutcome.Accepted, signIn.Attempt("admin", "Tr0ub4dor&3", "287082"));
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("signed in", StringComparison.Ordinal));
    }

    [Fact]
    public void WrongPassword_IsRefused_AndLoggedAsSuch()
    {
        var (signIn, log) = Create(Secret);

        Assert.Equal(AdminSignInOutcome.Refused, signIn.Attempt("admin", "wrong", "287082"));
        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("login or password", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WrongCode_IsRefused_AndLoggedAsSuch()
    {
        var (signIn, log) = Create(Secret);

        Assert.Equal(AdminSignInOutcome.Refused, signIn.Attempt("admin", "Tr0ub4dor&3", "000000"));
        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("code", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("login or password", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutSecondFactor_TheCodeIsNotAsked()
    {
        var (signIn, _) = Create(secret: null);

        Assert.Equal(AdminSignInOutcome.Accepted, signIn.Attempt("admin", "Tr0ub4dor&3", code: null));
    }

    [Fact]
    public void AttemptsOnceTheFailuresAreSpent_AreSuspended_WithoutChecking_AndLogged()
    {
        var (signIn, log) = Create(Secret);
        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
            signIn.Attempt("admin", "wrong", "287082");

        Assert.Equal(AdminSignInOutcome.Suspended, signIn.Attempt("admin", "Tr0ub4dor&3", "287082"));
        Assert.Equal(LogLevel.Warning, log.Entries.Last().Level);
        Assert.Contains("suspended", log.Entries.Last().Message, StringComparison.Ordinal);
    }

    private static (AdminSignIn SignIn, CapturingLogger Log) Create(string? secret)
    {
        var clock = new FixedClock(Now);
        var account = Options.Create(new AdminAccountOptions { Login = "admin", Password = "Tr0ub4dor&3", TotpSecret = secret });
        var log = new CapturingLogger();
        return (new AdminSignIn(new AdminCredentials(account), new AdminSecondFactor(account, clock), new LoginThrottle(clock), log), log);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CapturingLogger : ILogger<AdminSignIn>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
