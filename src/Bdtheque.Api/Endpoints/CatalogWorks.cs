using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Lists presented as the bibliography of an author (fonctionnel.md § Structure de l'application): the
/// series and the albums without series together at the first level, by sort key, each series grouping
/// its albums, in the order of the series. Shared by the bibliography of an author and by the works of a
/// genre or of a universe.
/// </summary>
internal static class CatalogWorks
{
    /// <summary>An entry of the first level: a series, or an album without series.</summary>
    internal sealed class FirstLevelEntry
    {
        public required string SortKey { get; init; }
        public Guid? SeriesId { get; init; }
        public Guid? AlbumId { get; init; }
    }

    /// <summary>
    /// The first level: <paramref name="series"/>, and the albums without series among
    /// <paramref name="albums"/>, ordered by the database, along the French collation of the sort keys,
    /// then by identifier, for a total order.
    /// </summary>
    public static IOrderedQueryable<FirstLevelEntry> FirstLevel(IQueryable<Series> series, IQueryable<Album> albums) =>
        series
            .Select(s => new FirstLevelEntry { SortKey = s.SortKey, SeriesId = s.Id, AlbumId = null })
            // An album without series has a title, hence a sort key.
            .Concat(albums.Where(a => a.SeriesId == null).Select(a => new FirstLevelEntry { SortKey = a.SortKey!, SeriesId = null, AlbumId = a.Id }))
            .OrderBy(e => e.SortKey)
            .ThenBy(e => e.SeriesId)
            .ThenBy(e => e.AlbumId);

    /// <summary>
    /// One page of the first level, each series with those of <paramref name="albums"/> it contains: the
    /// list is unbounded, unlike the bibliography of an author.
    /// </summary>
    /// <param name="series">The series of the list, those of <paramref name="albums"/> included.</param>
    public static async Task<Page<WorksEntry>> ReadPageAsync(
        IQueryable<Series> series, IQueryable<Album> albums, int page, int pageSize, CancellationToken cancellationToken)
    {
        var entries = await FirstLevel(series, albums).ToPageAsync(page, pageSize, e => e, cancellationToken);
        var seriesIds = entries.Items.Where(e => e.SeriesId is not null).Select(e => e.SeriesId!.Value).ToList();
        var albumIds = entries.Items.Where(e => e.AlbumId is not null).Select(e => e.AlbumId!.Value).ToList();

        var seriesItems = await series
            .Where(s => seriesIds.Contains(s.Id))
            .Select(s => new SeriesListItem(s.Id, s.Title))
            .ToDictionaryAsync(s => s.Id, cancellationToken);
        var summaries = (await albums
                .Where(a => (a.SeriesId != null && seriesIds.Contains(a.SeriesId.Value)) || albumIds.Contains(a.Id))
                .OrderInSeries()
                .Select(CatalogExpressions.Expand((Album a) => a.ToSummary()))
                .ToListAsync(cancellationToken))
            .ToLookup(a => a.SeriesId);

        var items = entries.Items
            .Select(e => e.SeriesId is { } seriesId
                ? new WorksEntry(new SeriesWorksItem(seriesItems[seriesId], summaries[seriesId].ToList()), null)
                : new WorksEntry(null, summaries[null].Single(a => a.Id == e.AlbumId)))
            .ToList();
        return new Page<WorksEntry>(items, entries.Number, entries.Size, entries.TotalCount);
    }
}
