using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Audience;

/// <summary>
/// Retention of the audience measurement (fonctionnel/mesure-audience.md § Mesure d'audience de l'interface web,
/// Durées de conservation; choix-implementation/mesure-audience.md § Mesure d'audience : mise en œuvre): closes
/// into counters the months whose first visits reach the end of their retention, then removes the public visits
/// that reached it. Run at startup, then daily; a failed run is caught up by the next.
/// </summary>
internal sealed class AudienceRetention(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<AudienceRetention> logger)
    : BackgroundService
{
    public static readonly TimeSpan Period = TimeSpan.FromDays(1);

    /// <summary>Months a public visit is kept after its beginning, with its path.</summary>
    public const int RetentionMonths = 25;

    /// <summary>Key of the PostgreSQL advisory lock reserved to the closing of the months.</summary>
    public const long LockKey = 0x4175_6469_656E_6365; // "Audience"

    public async Task RunAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Two runs at once would each establish the counters of the same months, neither seeing the other's.
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);

        // A month is closed once its first visits reach the end of their retention: its latest ones are then 24
        // months old, far beyond the 13 months the tracer of a visit lives, so that no usage can join it any more.
        var retentionEnd = now.AddMonths(-RetentionMonths);
        var closable = new DateOnly(retentionEnd.Year, retentionEnd.Month, 1);
        var closedThrough = await AudienceCounting.ClosedThroughAsync(context, cancellationToken);
        if (closedThrough is null || closedThrough < closable)
        {
            // Every month that can be, in one go and in order: the last month closed then separates the months the
            // counters carry from those still read from the visits.
            var visits = context.AudienceVisits.Where(v => v.CreatedAt < AudienceCounting.StartOf(closable.AddMonths(1)));
            if (closedThrough is { } closed)
                visits = visits.Where(v => v.CreatedAt >= AudienceCounting.StartOf(closed.AddMonths(1)));
            var counters = await AudienceCounting.CountAsync(context, visits, cancellationToken);
            context.AudienceCounters.AddRange(counters);
            await context.SaveChangesAsync(cancellationToken);
            if (counters.Count > 0)
                logger.LogInformation("The audience of the months through {Month} was closed into {Count} counters.", closable, counters.Count);
        }

        // Their month is closed by now. The visits of the administrator are kept without limit.
        await context.AudienceVisits
            .Where(v => !v.IsAdministrator && v.CreatedAt <= retentionEnd)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, timeProvider);
        do
        {
            try
            {
                await RunAsync(timeProvider.GetUtcNow(), stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed run (database unavailable) must not stop the API: the next one catches up.
                logger.LogError(exception, "The retention of the audience measurement failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
