using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Visuals;

/// <summary>
/// Removes the visuals waiting for the saving of their edition that are no longer kept, then the files of the visuals volume
/// that no visual, saved or pending, references any more (choix-implementation/domaine/visuels-stockage-et-traitement.md):
/// those of the pending visuals abandoned, and those an immediate compensation could not cover — process stopped between the
/// file and the row, file that could not be deleted. Run at startup, then daily:
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

    /// <summary>
    /// Deletes the pending visuals older than <see cref="PendingVisual.Lifetime"/>, then every file older than
    /// <see cref="GracePeriod"/> that no visual, saved or pending, references.
    /// </summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        // The threshold is taken before the references are read: a file older than it was written
        // before the read, so its visual, if committed, is among the references.
        var now = timeProvider.GetUtcNow();
        var threshold = now - GracePeriod;

        HashSet<string> referenced;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
            // A pending visual no longer kept can no longer become a visual (PendingVisual.IsExpiredAt).
            var expired = now - PendingVisual.Lifetime;
            await context.PendingVisuals.Where(v => v.CreatedAt <= expired).ExecuteDeleteAsync(cancellationToken);
            // Both tables in a single query: a pending visual that becomes a visual meanwhile is seen in one or the other.
            var references = await context.EditionVisuals.Select(v => v.MediaReference)
                .Concat(context.PendingVisuals.Select(v => v.MediaReference))
                .ToListAsync(cancellationToken);
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
