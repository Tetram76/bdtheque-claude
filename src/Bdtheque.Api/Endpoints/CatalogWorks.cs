using System.Linq.Expressions;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ContributionRole = Bdtheque.Contracts.Enums.ContributionRole;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Lists presented as the bibliography of an author (fonctionnel/structure-de-l-application.md): the
/// series and the albums without series together at the first level, by sort key, each series grouping
/// its albums, in the order of the series. Shared by the bibliography of an author and by the record of a
/// universe.
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

    /// <summary>A series of a bibliography as its list presents it: credited, or attached, itself or not, and its roles.</summary>
    internal sealed record BibliographySeries(SeriesListItem Item, bool OnSeries, List<ContributionRole> Roles);

    /// <summary>
    /// The whole bibliography (bounded, it is not paginated): the first level of <paramref name="series"/> and of the albums
    /// without series among <paramref name="albums"/>, each series with those of <paramref name="albums"/> it contains, in its
    /// order, and its overview. The caller reads it in a single snapshot, the reads being assembled into one another.
    /// </summary>
    /// <param name="series">The series of the list, those of <paramref name="albums"/> included.</param>
    /// <param name="seriesItem">A series as the list presents it.</param>
    /// <param name="albumItem">An album as the list presents it.</param>
    public static async Task<List<BibliographyEntry>> ReadBibliographyAsync(
        BdthequeDbContext context, IQueryable<Series> series, IQueryable<Album> albums, Expression<Func<Series, BibliographySeries>> seriesItem,
        Expression<Func<Album, AlbumBibliographyItem>> albumItem, CancellationToken cancellationToken)
    {
        var entries = await FirstLevel(series, albums).ToListAsync(cancellationToken);
        var seriesItems = await series.Select(seriesItem).ToDictionaryAsync(s => s.Item.Id, cancellationToken);
        var overviews = await OverviewsAsync(context, seriesItems.Keys, cancellationToken);
        var albumItems = (await albums.OrderInSeries().Select(albumItem).ToListAsync(cancellationToken))
            .ToLookup(a => a.Album.Album.SeriesId);

        return entries
            .Select(e => e.SeriesId is { } seriesId
                ? new BibliographyEntry(
                    new SeriesBibliographyItem(
                        seriesItems[seriesId].Item, overviews[seriesId], seriesItems[seriesId].OnSeries, seriesItems[seriesId].Roles,
                        albumItems[seriesId].ToList()),
                    null)
                : new BibliographyEntry(null, albumItems[null].Single(a => a.Album.Album.Id == e.AlbumId)))
            .ToList();
    }

    /// <summary>
    /// Each of <paramref name="seriesIds"/> as a whole, whichever of its albums the list presents: the number
    /// of its albums, and the covers of its first regular volumes, in its order — the pile of covers of the
    /// mockups, where neither an omnibus nor a special issue stands in for a volume (visuel/fiche-serie/).
    /// </summary>
    /// <param name="ownedEditionsOnly">
    /// Whether the context presents the owned editions of the albums only, among which their covers are then taken
    /// (fonctionnel/regles-metier/couverture-d-un-album.md): a list limited to the collection; all of them otherwise.
    /// </param>
    /// <remarks>
    /// Every album of these series is read, rather than its first volumes only: a series counts a few dozen
    /// albums at most, and the count needs them all anyway.
    /// </remarks>
    public static async Task<Dictionary<Guid, SeriesOverview>> OverviewsAsync(
        BdthequeDbContext context, IReadOnlyCollection<Guid> seriesIds, CancellationToken cancellationToken, bool ownedEditionsOnly = false)
    {
        var albums = (await context.Albums.AsNoTracking()
                .Where(a => a.SeriesId != null && seriesIds.Contains(a.SeriesId.Value))
                .OrderInSeries()
                .Select(ownedEditionsOnly ? SeriesAlbumOfOwnedEditions : SeriesAlbum)
                .ToListAsync(cancellationToken))
            .ToLookup(a => a.SeriesId);
        return seriesIds.ToDictionary(
            id => id,
            id => new SeriesOverview(
                albums[id].Count(),
                albums[id]
                    .Where(a => a.IsRegularVolume)
                    .Take(SeriesOverview.FirstVolumeCount)
                    .Select(a => CatalogExpressions.ToCover(a.CoverVisual))
                    .ToList()));
    }

    // An album of a series, with its cover among all its editions, or among its owned ones only.
    private static readonly Expression<Func<Album, SeriesAlbumCover>> SeriesAlbum =
        CatalogExpressions.Expand((Album a) => new SeriesAlbumCover(
            a.SeriesId!.Value, a.Type == AlbumType.Regular && !a.IsSpecialIssue, a.CoverVisualOf()));

    private static readonly Expression<Func<Album, SeriesAlbumCover>> SeriesAlbumOfOwnedEditions =
        CatalogExpressions.Expand((Album a) => new SeriesAlbumCover(
            a.SeriesId!.Value, a.Type == AlbumType.Regular && !a.IsSpecialIssue, a.Editions.Where(e => e.IsOwned()).CoverVisualAmong()));

    private sealed record SeriesAlbumCover(Guid SeriesId, bool IsRegularVolume, CatalogExpressions.CoverVisual? CoverVisual);
}
