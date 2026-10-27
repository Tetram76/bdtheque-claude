using System.Linq.Expressions;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The dashboard of the home page (fonctionnel/page-accueil.md § Page d'accueil), public: what the API computes
/// before Phase 4, the value indicators and the statistics drawn at random being signalled as not computable.
/// </summary>
internal static class DashboardEndpoints
{
    public const int DefaultLatestCount = 6;
    public const int MaxLatestCount = 20;

    public static void MapDashboard(this RouteGroupBuilder catalog) => catalog.MapGet("/dashboard", GetAsync);

    private static readonly Expression<Func<EditionSpotlight, SpotlightEdition>> SpotlightOf = CatalogExpressions.Expand(
        (EditionSpotlight s) => new SpotlightEdition(
            s.Edition.Album.ToSummary(), s.Edition.ToSummary(), CatalogExpressions.ToCover(s.Edition.CoverVisualId()), s.Edition.Album.Summary));

    private static readonly Expression<Func<Edition, LatestEntry>> LatestEntryOf = CatalogExpressions.Expand(
        (Edition e) => new LatestEntry(
            e.Album.ToSummary(), e.ToSummary(), CatalogExpressions.ToCover(e.CoverVisualId()), e.AcquisitionDate, e.EnteredCollectionAt!.Value));

    /// <param name="day">
    /// The date of the visitor, in the time zone of the browser, which the frontend sends: that of the edition in the
    /// spotlight, the same for every time zone.
    /// </param>
    /// <param name="latestCount">How many latest entries the theme presents.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A count outside 1 to <see cref="MaxLatestCount"/>: a technical error, the frontend asking for it, never the user.
    /// </exception>
    private static async Task<Dashboard> GetAsync(
        DateOnly day, BdthequeDbContext context, CancellationToken cancellationToken, int latestCount = DefaultLatestCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(latestCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latestCount, MaxLatestCount);

        // The spotlight first, which finds out an empty collection: nothing is then drawn nor computed
        // (contraintes-techniques/architecture-deploiement.md § Statistiques du dashboard).
        var spotlight = await SpotlightOfTheDayAsync(context, day, cancellationToken);
        if (spotlight is null)
            return new Dashboard(null);

        var owned = context.Editions.AsNoTracking().Where(CollectionMembership.IsOwned);
        var albums = context.Albums.AsNoTracking().Where(CatalogExpressions.Expand((Album a) => a.Editions.Any(e => e.IsOwned())));
        var counters = new DashboardCounters(
            await albums.CountAsync(cancellationToken),
            await albums.Where(a => a.SeriesId != null).Select(a => a.SeriesId).Distinct().CountAsync(cancellationToken),
            await owned.CountAsync(cancellationToken));

        // From the latest acquisition, or else entry in the collection: an acquisition date is a day, an entry an
        // instant, compared by its day in universal time. Then the latest entered, and the identifier, time-ordered,
        // for a total order.
        var latest = await owned
            .OrderByDescending(e => e.AcquisitionDate ?? DateOnly.FromDateTime(e.EnteredCollectionAt!.Value.UtcDateTime))
            .ThenByDescending(e => e.EnteredCollectionAt)
            .ThenByDescending(e => e.Id)
            .Take(latestCount)
            .Select(LatestEntryOf)
            .ToListAsync(cancellationToken);

        return new Dashboard(new DashboardContent(
            counters, spotlight, latest, AreValueIndicatorsComputable: false, AreRandomStatisticsComputable: false));
    }

    /// <summary>
    /// The edition in the spotlight of <paramref name="day"/>, drawn by the first display of the day without one
    /// (choix-implementation/domaine.md § Édition « À la une » : mise en œuvre).
    /// </summary>
    /// <returns><c>null</c> if the collection is empty.</returns>
    private static async Task<SpotlightEdition?> SpotlightOfTheDayAsync(BdthequeDbContext context, DateOnly day, CancellationToken cancellationToken)
    {
        var (from, to) = SpotlightDraw.RepetitionWindow(day);
        while (true)
        {
            // A day keeps its edition, unless deleted, which takes its spotlights with it: drawn again.
            var spotlight = await context.EditionSpotlights.AsNoTracking()
                .Where(s => s.Day == day)
                .Select(SpotlightOf)
                .SingleOrDefaultAsync(cancellationToken);
            if (spotlight is not null)
                return spotlight;

            var candidates = await context.Editions.AsNoTracking()
                .Where(CollectionMembership.IsOwned)
                .OrderBy(e => e.Id)
                .Select(e => new SpotlightCandidate(
                    e.Id,
                    e.Spotlights.Count(),
                    e.Spotlights.Any(s => s.Day >= from && s.Day <= to),
                    e.Visuals.Any(v => v.Type == DomainEnums.VisualType.Cover)))
                .ToListAsync(cancellationToken);
            if (candidates.Count == 0)
                return null;

            // Another display may draw the day at the same time, or the edition drawn be deleted before its
            // insertion: either way, the day is read again, and drawn again if still without edition.
            await context.InsertSpotlightAsync(day, SpotlightDraw.Pick(candidates, Random.Shared.NextDouble()), cancellationToken);
        }
    }
}
