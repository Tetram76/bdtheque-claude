using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the authors (<c>/admin/authors</c>).</summary>
internal static class AuthorEndpoints
{
    // fonctionnel.md § Suppression des entités: an author is refused deletion while credited on an
    // album or a series. An author credited with several roles on one record blocks it once: the
    // impact counts records, not contributions.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Album,
            (context, id) => context.Contributions
                .Where(c => c.AuthorId == id && c.AlbumId != null)
                .Select(c => c.AlbumId!.Value)),
        new(LinkNature.Reference, EntityKind.Series,
            (context, id) => context.Contributions
                .Where(c => c.AuthorId == id && c.SeriesId != null)
                .Select(c => c.SeriesId!.Value)),
    ];

    public static void MapAuthors(this RouteGroupBuilder admin)
    {
        var authors = admin.MapGroup("/authors");
        authors.MapGet("/{id:guid}", GetAsync);
        authors.MapPost("/", CreateAsync);
        authors.MapPut("/{id:guid}", UpdateAsync);
        authors.MapDeletion<Author>(DeletionLinks);
    }

    public static void MapAuthorCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/authors", ListAsync);
        catalog.MapGet("/authors/{id:guid}", GetDetailAsync);
    }

    /// <summary>
    /// An author with the whole bibliography (fonctionnel.md § Structure de l'application), as two
    /// separate lists, each entry with the roles held: the albums credited on, and the series among whose
    /// authors the author is — its template contributions, presented as the authors of the series.
    /// </summary>
    private static async Task<AuthorDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var author = await context.Authors.AsNoTracking()
                         .Where(a => a.Id == id)
                         .Select(a => new { a.LastName, a.FirstName, a.Pseudonym, a.Biography, a.Nationality, a.CreatedAt, a.ModifiedAt })
                         .SingleOrDefaultAsync(cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Author), id);

        var albums = await context.Albums.AsNoTracking()
            .Where(a => a.Contributions.Any(c => c.AuthorId == id))
            .OrderByAlbum(a => a)
            .Select(CatalogExpressions.Expand((Album a) => new AlbumBibliographyItem(a.ToSummary(), a.Contributions.RolesOf(id))))
            .ToListAsync(cancellationToken);

        var series = await context.Series.AsNoTracking()
            .Where(s => s.TemplateContributions.Any(c => c.AuthorId == id))
            .OrderBy(s => s.SortKey)
            .ThenBy(s => s.Id)
            .Select(CatalogExpressions.Expand((Series s) => new SeriesBibliographyItem(
                new SeriesListItem(s.Id, s.Title), s.TemplateContributions.RolesOf(id))))
            .ToListAsync(cancellationToken);

        return new AuthorDetail(
            id, author.LastName, author.FirstName, author.Pseudonym, author.Biography, author.Nationality, albums, series, author.CreatedAt,
            author.ModifiedAt);
    }

    /// <summary>
    /// The authors found by any part of their name: last name, first name, pseudonym, or full name, as
    /// displayed ("First Last") or as sorted ("Last First", fonctionnel.md § Artistes).
    /// </summary>
    private static Task<Page<AuthorListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        CatalogFilters.EnsureNavigationEntry(entry);
        var authors = context.Authors.AsNoTracking()
            .WhereContains(q, a => a.LastName, a => a.FirstName, a => a.Pseudonym, a => a.FirstName + " " + a.LastName, a => a.SortKey);
        if (entry is not null)
            authors = authors.Where(a => a.NavigationEntry == entry);

        return authors
            .OrderBy(a => a.SortKey)
            .ThenBy(a => a.Id)
            .ToPageAsync(page, pageSize, a => new AuthorListItem(a.Id, a.LastName, a.FirstName, a.Pseudonym), cancellationToken);
    }

    private static async Task<AuthorForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Authors
            .Where(a => a.Id == id)
            .Select(a => new AuthorForm(
                a.Id, a.LastName, a.FirstName, a.Pseudonym, a.Biography, a.Nationality,
                EF.Property<uint>(a, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Author), id);

    private static async Task<Created<AuthorForm>> CreateAsync(
        CreateAuthorRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var author = new Author(request.LastName, request.FirstName, request.Pseudonym);
        author.UpdateBiography(request.Biography);
        author.UpdateNationality(request.Nationality);
        context.Authors.Add(author);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/authors/{author.Id}", ToForm(context, author));
    }

    private static async Task<AuthorForm> UpdateAsync(
        Guid id, UpdateAuthorRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var author = await context.LoadAggregateForWriteAsync<Author>(id, request.Version, cancellationToken: cancellationToken);
        author.UpdateIdentity(request.LastName, request.FirstName, request.Pseudonym);
        author.UpdateBiography(request.Biography);
        author.UpdateNationality(request.Nationality);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, author);
    }

    private static AuthorForm ToForm(BdthequeDbContext context, Author author) =>
        new(author.Id, author.LastName, author.FirstName, author.Pseudonym, author.Biography, author.Nationality,
            context.VersionOf(author));
}
