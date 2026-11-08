using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The records a list of the administration is filtered on, as the lists name them, which the list presents without another
/// request (choix-implementation/api/organisation-de-l-api.md): none when the list is not filtered on the type, or on a record
/// that does not exist.
/// </summary>
internal static class FilteredRecords
{
    public static async Task<SeriesListItem?> SeriesAsync(BdthequeDbContext context, Guid? id, CancellationToken cancellationToken) =>
        id is { } seriesId
            ? await context.Series.AsNoTracking().Where(s => s.Id == seriesId)
                .Select(s => new SeriesListItem(s.Id, s.Title)).SingleOrDefaultAsync(cancellationToken)
            : null;

    public static async Task<AuthorListItem?> AuthorAsync(BdthequeDbContext context, Guid? id, CancellationToken cancellationToken) =>
        id is { } authorId
            ? await context.Authors.AsNoTracking().Where(a => a.Id == authorId)
                .Select(a => new AuthorListItem(a.Id, a.LastName, a.FirstName, a.Pseudonym)).SingleOrDefaultAsync(cancellationToken)
            : null;

    public static async Task<PublisherListItem?> PublisherAsync(BdthequeDbContext context, Guid? id, CancellationToken cancellationToken) =>
        id is { } publisherId
            ? await context.Publishers.AsNoTracking().Where(p => p.Id == publisherId)
                .Select(p => new PublisherListItem(p.Id, p.Name, p.Website)).SingleOrDefaultAsync(cancellationToken)
            : null;

    public static async Task<PublisherCollectionListItem?> PublisherCollectionAsync(
        BdthequeDbContext context, Guid? id, CancellationToken cancellationToken) =>
        id is { } collectionId
            ? await context.PublisherCollections.AsNoTracking().Where(c => c.Id == collectionId)
                .Select(c => new PublisherCollectionListItem(c.Id, c.Name, c.PublisherId, c.Publisher.Name)).SingleOrDefaultAsync(cancellationToken)
            : null;

    public static async Task<GenreListItem?> GenreAsync(BdthequeDbContext context, Guid? id, CancellationToken cancellationToken) =>
        id is { } genreId
            ? await context.Genres.AsNoTracking().Where(g => g.Id == genreId)
                .Select(g => new GenreListItem(g.Id, g.Label)).SingleOrDefaultAsync(cancellationToken)
            : null;

    public static async Task<UniverseListItem?> UniverseAsync(BdthequeDbContext context, Guid? id, CancellationToken cancellationToken) =>
        id is { } universeId
            ? await context.Universes.AsNoTracking().Where(u => u.Id == universeId)
                .Select(CatalogExpressions.Expand((Universe u) => u.ToListItem())).SingleOrDefaultAsync(cancellationToken)
            : null;
}
