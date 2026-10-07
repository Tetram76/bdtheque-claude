namespace Bdtheque.Api.Visuals;

/// <summary>
/// Lets one upload of a visual through at a time (choix-implementation.md § Visuels : stockage et
/// traitement). Each upload holds its file and its decoded image in memory — up to some 400 MB for
/// the largest scan accepted —, a bound per request only: several uploads at once, as when the
/// administrator drops several scans together, would add up and could exhaust the memory of the NAS,
/// stopping <c>api</c> altogether.
/// </summary>
/// <remarks>
/// An upload waits for its turn before reading its request body: nothing of it is held in memory
/// meanwhile, and Kestrel only enforces the minimum data rate of a request body while it is being
/// read, so a waiting upload is never cut off for its slowness.
/// </remarks>
internal sealed class VisualUploadGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Waits for the turn of the upload, held until the returned slot is disposed.</summary>
    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Slot(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Slot(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                semaphore.Release();
        }
    }
}
