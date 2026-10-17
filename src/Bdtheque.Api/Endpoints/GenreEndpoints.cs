using System.Data;
using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the genres (<c>/admin/genres</c>).</summary>
internal static class GenreEndpoints
{
    // fonctionnel.md § Suppression des entités: a genre is only associated with albums and series.
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
        genres.MapGet("/{id:guid}", GetAsync);
        genres.MapPost("/", CreateAsync);
        genres.MapPut("/{id:guid}", UpdateAsync);
        genres.MapDeletion<Genre>(DeletionLinks);
    }

    public static void MapGenreCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/genres", ListAsync);
        catalog.MapGet("/genres/{id:guid}/works", GetWorksAsync);
    }

    /// <summary>
    /// The series and albums of a genre, which has no record (fonctionnel.md § Structure de l'application):
    /// a series of the genre with all its albums, the genres of an album in a series being those of the
    /// album and of the series, and a series with those of its albums only that are of the genre.
    /// </summary>
    private static async Task<GenreWorks> GetWorksAsync(
        Guid id, BdthequeDbContext context, CancellationToken cancellationToken, int page = 1, int pageSize = Paging.DefaultSize)
    {
        // A single snapshot for the reads assembled into one another.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var genre = await context.Genres.AsNoTracking()
                        .Where(g => g.Id == id)
                        .Select(g => new GenreListItem(g.Id, g.Label))
                        .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Genre), id);

        var albums = context.Albums.AsNoTracking().WhereGenre(id);
        var series = context.Series.AsNoTracking().Where(s => s.Genres.Any(g => g.Id == id) || albums.Any(a => a.SeriesId == s.Id));
        return new GenreWorks(genre, await CatalogWorks.ReadPageAsync(context, series, albums, page, pageSize, cancellationToken));
    }

    private static Task<Page<GenreListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.Genres.AsNoTracking()
            .WhereContains(q, g => g.Label)
            .OrderBy(g => g.Label)
            .ThenBy(g => g.Id)
            .ToPageAsync(page, pageSize, g => new GenreListItem(g.Id, g.Label), cancellationToken);

    private static async Task<GenreForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Genres
            .Where(g => g.Id == id)
            .Select(g => new GenreForm(g.Id, g.Label, EF.Property<uint>(g, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Genre), id);

    private static async Task<Created<GenreForm>> CreateAsync(
        CreateGenreRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var genre = new Genre(request.Label);
        context.Genres.Add(genre);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/genres/{genre.Id}", ToForm(context, genre));
    }

    private static async Task<GenreForm> UpdateAsync(
        Guid id, UpdateGenreRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var genre = await context.LoadAggregateForWriteAsync<Genre>(id, request.Version, cancellationToken: cancellationToken);
        genre.SetLabel(request.Label);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, genre);
    }

    private static GenreForm ToForm(BdthequeDbContext context, Genre genre) =>
        new(genre.Id, genre.Label, context.VersionOf(genre));
}
