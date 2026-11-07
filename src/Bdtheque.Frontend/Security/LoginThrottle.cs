namespace Bdtheque.Frontend.Security;

/// <summary>
/// The failed logins the application accepts over a window of time, after which it refuses any attempt until the oldest
/// failure leaves the window: guessing the password of the account, exposed on the Internet, takes years rather than
/// hours. Counted for the application as a whole, no request telling reliably who sends it behind the reverse proxy
/// (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md).
/// </summary>
public sealed class LoginThrottle(TimeProvider clock)
{
    public const int MaxFailures = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly Queue<DateTimeOffset> _failures = new();
    private readonly Lock _lock = new();

    public bool IsBlocked()
    {
        lock (_lock)
        {
            Forget();
            return _failures.Count >= MaxFailures;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            Forget();
            _failures.Enqueue(clock.GetUtcNow());
        }
    }

    /// <summary>Once signed in, the failures before were the administrator's own.</summary>
    public void Reset()
    {
        lock (_lock)
            _failures.Clear();
    }

    private void Forget()
    {
        var since = clock.GetUtcNow() - Window;
        while (_failures.TryPeek(out var oldest) && oldest <= since)
            _failures.Dequeue();
    }
}
