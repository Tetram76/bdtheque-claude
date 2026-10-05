using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Visuals;

/// <summary>
/// Removes the files of the visuals volume that no visual references any more (choix-implementation.md
/// § Visuels : stockage et traitement): those an immediate compensation could not cover — process
/// stopped between the file and the row, file that could not be deleted. Run at startup, then daily:
/// an orphan only costs disk space.
/// </summary>
internal sealed class VisualReconciliation(
    IServiceScopeFactory scopeFactory, VisualStorage storage, TimeProvider timeProvider, ILogger<VisualReconciliation> logger)
    : BackgroundService
{
    public static readonly TimeSpan Period = TimeSpan.FromDays(1);

    /// <summary>
    /// Age below which an unreferenced file is left alone: its upload may still be under way, the
    /// files being written just before the row. Kept until a later run.
    /// </summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);

    /// <summary>Deletes every file older than <see cref="GracePeriod"/> that no visual references.</summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        // The threshold is taken before the references are read: a file older than it was written
        // before the read, so its visual, if committed, is among the references.
        var threshold = timeProvider.GetUtcNow() - GracePeriod;

        HashSet<string> referenced;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
            var references = await context.EditionVisuals.Select(v => v.MediaReference).ToListAsync(cancellationToken);
            referenced = references.Select(VisualStorage.StemOf).ToHashSet(StringComparer.Ordinal);
        }

        foreach (var file in storage.Files())
        {
            if (!referenced.Contains(VisualStorage.StemOf(file.Name)) && file.LastWriteTimeUtc < threshold.UtcDateTime)
            {
                logger.LogInformation("Deleting the orphan visual file {File}.", file.FullName);
                storage.DeleteFile(file);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, timeProvider);
        do
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed run (database unavailable, volume unreadable) must not stop the API: the
                // next one catches up.
                logger.LogError(exception, "The reconciliation of the visual files failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
