namespace Bdtheque.Frontend.Security;

public enum AdminSignInOutcome
{
    Accepted,
    Refused,
    Suspended,
}

/// <summary>
/// The decision on a sign-in to the administration (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md):
/// attempts limited, then credentials, then the code of the second factor when it is asked. The page never tells which
/// was wrong; the log does, so that a refused sign-in can always be understood (gestion-projet/gouvernance/qualite-du-code.md).
/// </summary>
public sealed partial class AdminSignIn(
    AdminCredentials credentials,
    AdminSecondFactor secondFactor,
    LoginThrottle throttle,
    ILogger<AdminSignIn> logger)
{
    public AdminSignInOutcome Attempt(string? login, string? password, string? code)
    {
        // Too many failures of late: refused without even checking, which would otherwise go on telling a right guess.
        if (throttle.IsBlocked())
        {
            LogSuspended(LoginThrottle.MaxFailures, LoginThrottle.Window);
            return AdminSignInOutcome.Suspended;
        }

        if (!credentials.AreValid(login, password))
        {
            throttle.RecordFailure();
            LogWrongCredentials();
            return AdminSignInOutcome.Refused;
        }

        // Checked only for the right credentials: once accepted it can no longer be used, and a typing error in the
        // password must not spend the code of the administrator.
        if (secondFactor.IsRequired && !secondFactor.IsValid(code))
        {
            throttle.RecordFailure();
            LogWrongCode();
            return AdminSignInOutcome.Refused;
        }

        throttle.Reset();
        LogSignedIn();
        return AdminSignInOutcome.Accepted;
    }

    [LoggerMessage(LogLevel.Warning, "Sign-in to the administration refused: wrong login or password.")]
    private partial void LogWrongCredentials();

    [LoggerMessage(LogLevel.Warning,
        "Sign-in to the administration refused: wrong code of the second factor (not the code of the moment, of the step before or after it, or already used; check the key and the clock of the authenticator).")]
    private partial void LogWrongCode();

    [LoggerMessage(LogLevel.Warning, "Sign-in to the administration suspended: {MaxFailures} failed attempts within {Window}.")]
    private partial void LogSuspended(int maxFailures, TimeSpan window);

    [LoggerMessage(LogLevel.Information, "The administrator signed in.")]
    private partial void LogSignedIn();
}
