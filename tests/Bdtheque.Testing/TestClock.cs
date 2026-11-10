namespace Bdtheque.Testing;

/// <summary>
/// Application clock of the tests, in one of two lives:
/// <list type="bullet">
/// <item>the system time (<c>new()</c>), for the tests that give every date they depend on (e.g. the day of a dashboard);</item>
/// <item>
/// a time of its own (<see cref="StartingAt"/>), for an application whose rendering or behaviour depends on the date
/// (choix-implementation/outillage.md): it starts at a fixed instant and moves forward by a millisecond at each reading,
/// never with the real time. A test then gives the same result whenever it runs, while successive writes still get
/// increasing dates, which FakeTimeProvider's time, not flowing by itself, would not give: their order would then fall
/// back on identifiers that Guid.CreateVersion7 orders to the millisecond of the real time only.
/// </item>
/// </list>
/// Either way, a test may set <see cref="UtcNow"/>, forwards or backwards, which stays put until set again. Timers stay
/// those of the system: setting the time never runs the periodic tasks of the application.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private readonly DateTimeOffset? _origin;
    private long _readings;
    private DateTimeOffset? _utcNow;

    /// <summary>The system time, until a test sets <see cref="UtcNow"/>.</summary>
    public TestClock()
    {
    }

    private TestClock(DateTimeOffset origin) => _origin = origin.ToUniversalTime();

    /// <summary>A time of its own, from <paramref name="origin"/>, a millisecond later at each reading.</summary>
    public static TestClock StartingAt(DateTimeOffset origin) => new(origin);

    /// <summary>The time the clock gives once set; <c>null</c> for its own time (the system time, or from its origin).</summary>
    public DateTimeOffset? UtcNow
    {
        get => _utcNow;
        set => _utcNow = value?.ToUniversalTime();
    }

    public override DateTimeOffset GetUtcNow() =>
        _utcNow
        ?? (_origin is { } origin ? origin.AddMilliseconds(Interlocked.Increment(ref _readings)) : System.GetUtcNow());
}
