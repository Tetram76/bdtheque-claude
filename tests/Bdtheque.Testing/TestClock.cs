namespace Bdtheque.Testing;

/// <summary>
/// Application clock of the tests: the system time until a test sets <see cref="UtcNow"/>, then that
/// time, forwards or backwards. The tests that leave it alone run on the real time, as in production,
/// where FakeTimeProvider's time would not flow by itself: successive writes could get the same date,
/// their order then falling back on identifiers that Guid.CreateVersion7 orders to the millisecond
/// only. Timers stay those of the system: setting the time never runs the API's periodic tasks.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset? _utcNow;

    /// <summary>The time the clock gives; <c>null</c> for the system time.</summary>
    public DateTimeOffset? UtcNow
    {
        get => _utcNow;
        set => _utcNow = value?.ToUniversalTime();
    }

    public override DateTimeOffset GetUtcNow() => _utcNow ?? System.GetUtcNow();
}
