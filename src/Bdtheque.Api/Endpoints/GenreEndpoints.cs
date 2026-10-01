using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
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
