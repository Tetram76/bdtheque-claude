using System.Data;
using Bdtheque.Contracts.Audience;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Audience;

/// <summary>A period, in months, and a compartment of the audience: each criterion left out takes in every value.</summary>
/// <param name="From">First day of the first month read.</param>
/// <param name="To">First day of the last month read.</param>
internal sealed record AudienceSelection(
    DateOnly? From, DateOnly? To, bool? Administrator, DeviceType? DeviceType, RenderingEngine? RenderingEngine, string? EngineVersion);

/// <summary>
/// The counters of the audience measurement (choix-implementation/mesure-audience.md § Mesure d'audience : mise
/// en œuvre): established from the visits when a month is closed, computed the same way for the months still
/// open, and every indicator recomposed from them (modele-metier.md § Compteur d'audience mensuel).
/// </summary>
internal static class AudienceCounting
{
    /// <summary>
    /// The counters of <paramref name="visits"/>: by function, month of the beginning of the visit, in universal
    /// time, and compartment. The same query establishes the counters of a closed month and computes those of an
    /// open one, so that the indicators of a month read the same before and after its closing.
    /// </summary>
    public static async Task<List<AudienceCounter>> CountAsync(
        BdthequeDbContext context, IQueryable<AudienceVisit> visits, CancellationToken cancellationToken)
    {
        var usages = context.AudienceUsages.Join(visits, u => u.VisitId, v => v.Id, (u, v) => new
        {
            u.Function,
            v.CreatedAt.Year,
            v.CreatedAt.Month,
            v.IsAdministrator,
            v.DeviceType,
            v.RenderingEngine,
            v.EngineVersion,
            u.VisitId,
            u.IsRepeat,
            Date = u.CreatedAt,
            u.PresentedRecordType,
            u.PresentedRecordId,
            u.Parameters,
        });

        var figures = await usages
            .GroupBy(u => new { u.Function, u.Year, u.Month, u.IsAdministrator, u.DeviceType, u.RenderingEngine, u.EngineVersion })
            .Select(g => new
            {
                g.Key,
                Usages = g.Count(),
                Visits = g.Select(u => u.VisitId).Distinct().Count(),
                Repeated = g.Count(u => u.IsRepeat),
                Last = g.Max(u => u.Date),
            })
            .ToListAsync(cancellationToken);

        var objects = (await usages
                .Where(u => u.PresentedRecordType != null || u.Parameters != null)
                .GroupBy(u => new
                {
                    u.Function, u.Year, u.Month, u.IsAdministrator, u.DeviceType, u.RenderingEngine, u.EngineVersion,
                    u.PresentedRecordType, u.PresentedRecordId, u.Parameters,
                })
                .Select(g => new { g.Key, Usages = g.Count() })
                .ToListAsync(cancellationToken))
            .ToLookup(
                o => (o.Key.Function, o.Key.Year, o.Key.Month, o.Key.IsAdministrator, o.Key.DeviceType, o.Key.RenderingEngine, o.Key.EngineVersion),
                o => new AudienceCounterObject(
                    o.Key.PresentedRecordType is { } type ? new Domain.Entities.PresentedRecord(type, o.Key.PresentedRecordId!.Value) : null,
                    o.Key.Parameters,
                    o.Usages));

        return figures
            .Select(f => new AudienceCounter(
                f.Key.Function, new DateOnly(f.Key.Year, f.Key.Month, 1), f.Key.IsAdministrator, f.Key.DeviceType, f.Key.RenderingEngine,
                f.Key.EngineVersion, f.Usages, f.Visits, f.Repeated, f.Last,
                objects[(f.Key.Function, f.Key.Year, f.Key.Month, f.Key.IsAdministrator, f.Key.DeviceType, f.Key.RenderingEngine, f.Key.EngineVersion)]))
            .ToList();
    }

    /// <summary>The last month closed into counters, or none: every later month is still read from the visits.</summary>
    public static Task<DateOnly?> ClosedThroughAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        context.AudienceCounters.MaxAsync(c => (DateOnly?)c.Month, cancellationToken);

    /// <summary>The indicators of each function used over <paramref name="selection"/>, by name of function.</summary>
    /// <exception cref="ArgumentException">A month not given by its first day: the frontend builds the period.</exception>
    public static async Task<List<FunctionIndicators>> ReadAsync(
        BdthequeDbContext context, AudienceSelection selection, CancellationToken cancellationToken)
    {
        if (selection.From is { Day: not 1 } || selection.To is { Day: not 1 })
            throw new ArgumentException("A month is given by its first day.", nameof(selection));

        // The counters and the visits read in one snapshot: a closing in between would count a month twice, or not
        // at all.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var closedThrough = await ClosedThroughAsync(context, cancellationToken);

        var stored = context.AudienceCounters.AsNoTracking();
        if (selection.From is { } from)
            stored = stored.Where(c => c.Month >= from);
        if (selection.To is { } to)
            stored = stored.Where(c => c.Month <= to);
        if (selection.Administrator is { } administrator)
            stored = stored.Where(c => c.IsAdministrator == administrator);
        if (selection.DeviceType is { } deviceType)
            stored = stored.Where(c => c.DeviceType == deviceType);
        if (selection.RenderingEngine is { } engine)
            stored = stored.Where(c => c.RenderingEngine == engine);
        if (selection.EngineVersion is { } version)
            stored = stored.Where(c => c.EngineVersion == version);

        var visits = context.AudienceVisits.AsNoTracking();
        if (closedThrough is { } closed)
            visits = visits.Where(v => v.CreatedAt >= StartOf(closed.AddMonths(1)));
        if (selection.From is { } fromMonth)
            visits = visits.Where(v => v.CreatedAt >= StartOf(fromMonth));
        if (selection.To is { } toMonth)
            visits = visits.Where(v => v.CreatedAt < StartOf(toMonth.AddMonths(1)));
        if (selection.Administrator is { } isAdministrator)
            visits = visits.Where(v => v.IsAdministrator == isAdministrator);
        if (selection.DeviceType is { } device)
            visits = visits.Where(v => v.DeviceType == device);
        if (selection.RenderingEngine is { } renderingEngine)
            visits = visits.Where(v => v.RenderingEngine == renderingEngine);
        if (selection.EngineVersion is { } engineVersion)
            visits = visits.Where(v => v.EngineVersion == engineVersion);

        var counters = (await stored.ToListAsync(cancellationToken)).Concat(await CountAsync(context, visits, cancellationToken));
        return counters
            .GroupBy(c => c.Function)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(Recompose)
            .ToList();
    }

    /// <summary>First instant of a month, in universal time, the time the beginnings of the visits are counted in.</summary>
    public static DateTimeOffset StartOf(DateOnly month) => new(month.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    // modele-metier.md § Compteur d'audience mensuel: the numbers add up, a visit belonging to a single counter per
    // function; the last usage is the latest; the different objects are those of the union of the counters.
    private static FunctionIndicators Recompose(IGrouping<string, AudienceCounter> counters) =>
        new(
            counters.Key,
            counters.Sum(c => c.UsageCount),
            counters.Sum(c => c.VisitCount),
            counters.Sum(c => c.RepeatedUsageCount),
            counters.SelectMany(c => c.Objects).Select(o => (o.RecordType, o.RecordId, o.Parameters)).Distinct().Count(),
            counters.Max(c => c.LastUsage),
            counters
                .GroupBy(c => c.Month)
                .OrderBy(m => m.Key)
                .Select(m => new MonthlyUsage(m.Key, m.Sum(c => c.UsageCount), m.Sum(c => c.VisitCount)))
                .ToList());
}
