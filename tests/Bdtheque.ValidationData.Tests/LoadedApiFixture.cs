namespace Bdtheque.ValidationData.Tests;

/// <summary>
/// An API on an empty database, into which the validation data is loaded once for the whole test class, at a time of its
/// own, so that what the data holds around the day of the load does not depend on when the tests run (a load and a test on
/// both sides of midnight would not look at the same days).
/// </summary>
public abstract class LoadedApiFixture(bool paleCoverInTheSpotlight) : ApiHostFactory(TestClock.StartingAt(Origin)), IAsyncLifetime
{
    public static readonly DateTimeOffset Origin = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        await new ValidationDataLoader(CreateApiClient(), Clock).LoadAsync(paleCoverInTheSpotlight, CancellationToken.None);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
