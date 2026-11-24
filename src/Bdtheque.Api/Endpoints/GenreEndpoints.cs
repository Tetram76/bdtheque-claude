using System.Data;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Errors;
using Bdtheque.Api.Merge;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the genres (<c>/admin/genres</c>).</summary>
internal static class GenreEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: a genre is only associated with albums and series.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Association, EntityKind.Album,
            (context, id) => context.Albums.Where(a => a.Genres.Any(g => g.Id == id)).Select(a => a.Id)),
        new(LinkNature.Association, EntityKind.Series,
            (context, id) => context.Series.Where(s => s.Genres.Any(g => g.Id == id)).Select(s => s.Id)),
    ];

    public static void MapGenres(this RouteGroupBuilder admin)
    {
        var genres = admin.MapGroup("/genres");
        genres.MapGet("/", ListForAdministrationAsync);
        genres.MapGet("/{id:guid}", GetAsync);
        genres.MapPost("/", CreateAsync);
        genres.MapPut("/{id:guid}", UpdateAsync);
        genres.MapDeletion<Genre>(DeletionLinks);
        genres.MapGet("/merge", GetMergeAsync);
        genres.MapPost("/merge", MergeAsync);
    }

    // The series and albums a genre classes, as the lists filtered on it find them (visuel/fusion/fusion-de-deux-fiches.md).
    private static readonly (EntityKind Kind, Func<BdthequeDbContext, Guid, IQueryable<Guid>> Records)[] MergeLinks =
    [
        (EntityKind.Series, (context, id) => context.Series.AsNoTracking().WhereGenre(id).Select(s => s.Id)),
        (EntityKind.Album, (context, id) => context.Albums.AsNoTracking().WhereGenre(id).Select(a => a.Id)),
    ];

    /// <summary>Two genres to merge, and what they class (fonctionnel/regles-metier/fusion-des-fiches.md).</summary>
    private static async Task<GenreMergePreview> GetMergeAsync(Guid first, Guid second, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot: the counts are those of the two genres read.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        return new GenreMergePreview(
            await MergeRecordAsync(context, first, cancellationToken),
            await MergeRecordAsync(context, second, cancellationToken),
            await RecordMerge.CountLinksAsync(context, first, second, MergeLinks, cancellationToken));
    }

    private static async Task<GenreMergeRecord> MergeRecordAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken) =>
        await context.Genres.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GenreMergeRecord(g.Id, g.Label, g.ModifiedAt, EF.Property<uint>(g, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Genre), id);

    /// <summary>
    /// Merges two genres: the genre that remains keeps the label chosen, and classes the series and albums of both, each once.
    /// </summary>
    private static Task<MergeResult> MergeAsync(GenreMergeRequest request, BdthequeDbContext context, CancellationToken cancellationToken) =>
        RecordMerge.MergeAsync<Genre>(context, request.First, request.Second, RecordMerge.KeepsFirst([request.Label]),
            pair => pair.Kept.SetLabel(pair.Of(request.Label).Label),
            async pair =>
        {
            foreach (var series in await RecordMerge.LoadLinkedForWriteAsync<Series>(
                         context, context.Series.Where(s => s.Genres.Any(g => g.Id == pair.Merged.Id)).Select(s => s.Id), cancellationToken))
            {
                await context.Entry(series).Collection(s => s.Genres).LoadAsync(cancellationToken);
                series.TransferGenre(pair.Merged, pair.Kept);
            }

            foreach (var album in await RecordMerge.LoadLinkedForWriteAsync<Album>(
                         context, context.Albums.Where(a => a.Genres.Any(g => g.Id == pair.Merged.Id)).Select(a => a.Id), cancellationToken))
            {
                await context.Entry(album).Collection(a => a.Genres).LoadAsync(cancellationToken);
                album.TransferGenre(pair.Merged, pair.Kept);
            }
        }, cancellationToken);

    public static void MapGenreCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/genres", ListAsync);
    }

    private static Task<Page<GenreListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, int page = 1,
        int pageSize = Paging.DefaultSize) =>
        Search(context, q, entry).ToPageAsync(page, pageSize, g => new GenreListItem(g.Id, g.Label), cancellationToken);

    /// <summary>
    /// The list of the genres of the administration (visuel/listes/liste-par-liste.md): the search, the navigation by initial
    /// and the order of the list of the consultation. Each row counts the albums and series of the genre as the lists filtered
    /// on it find them, and carries its modification and its version, which its modification and its deletion send back.
    /// </summary>
    private static Task<Page<GenreAdminListItem>> ListForAdministrationAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, int page = 1,
        int pageSize = Paging.DefaultSize) =>
        Search(context, q, entry)
            .ToPageAsync(page, pageSize, CatalogExpressions.Expand((Genre g) => new GenreAdminListItem(
                g.Id,
                g.Label,
                context.Albums.Count(a => a.IsOfGenre(g.Id)),
                context.Series.Count(s => s.IsOfGenre(g.Id)),
                g.ModifiedAt,
                EF.Property<uint>(g, BdthequeDbContext.VersionProperty))), cancellationToken);

    /// <summary>The genres found by their label, under the entry of the navigation, by label.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    private static IOrderedQueryable<Genre> Search(BdthequeDbContext context, string? text, string? entry)
    {
        CatalogFilters.EnsureNavigationEntry(entry);
        var genres = context.Genres.AsNoTracking().WhereContains(text, g => g.Label);
        if (entry is not null)
            genres = genres.Where(g => g.NavigationEntry == entry);
        return genres.OrderByEntry(g => g.NavigationEntry).ThenBy(g => g.Label).ThenBy(g => g.Id);
    }

    private static async Task<GenreForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Genres
            .Where(g => g.Id == id)
            .Select(g => new GenreForm(g.Id, g.Label, EF.Property<uint>(g, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Genre), id);

    private static async Task<Created<GenreForm>> CreateAsync(
        CreateGenreRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        var genre = fields.Create("", () => new Genre(request.Label));
        context.Genres.Add(genre);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/genres/{genre.Id}", ToForm(context, genre));
    }

    private static async Task<GenreForm> UpdateAsync(
        Guid id, UpdateGenreRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var genre = await context.LoadAggregateForWriteAsync<Genre>(id, request.Version, cancellationToken: cancellationToken);
        fields.Place(genre);
        genre.SetLabel(request.Label);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, genre);
    }

    private static GenreForm ToForm(BdthequeDbContext context, Genre genre) =>
        new(genre.Id, genre.Label, context.VersionOf(genre));
}
