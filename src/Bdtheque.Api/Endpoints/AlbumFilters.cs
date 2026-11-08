using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search, navigation by initial and cross filters of a list of albums (choix-implementation/api/recherche.md),
/// which the list of the consultation and that of the administration share, bound as a whole from the query of their request
/// (<c>[AsParameters]</c>): the search is its parameter <c>q</c>, each other criterion that of its name.
/// </summary>
internal sealed record AlbumFilters(
    [FromQuery(Name = "q")] string? Text, string? Entry, Guid? SeriesId, Guid? AuthorId, Guid? PublisherId, Guid? PublisherCollectionId, Guid? GenreId,
    Guid? UniverseId)
{
    /// <summary>
    /// Narrows <paramref name="albums"/>: an album is found by its title or by that of its series, part of its label
    /// (fonctionnel/presentation/libelle-d-un-album.md), and an album without a title of its own is filed under the current
    /// entry of its series.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    public async Task<IQueryable<Album>> ApplyAsync(IQueryable<Album> albums, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        CatalogFilters.EnsureNavigationEntry(Entry);
        albums = albums.WhereContains(Text, a => a.Title, a => a.Series!.Title);
        if (Entry is not null)
            albums = albums.Where(a => (a.NavigationEntry ?? a.Series!.NavigationEntry) == Entry);
        if (SeriesId is not null)
            albums = albums.Where(a => a.SeriesId == SeriesId);
        if (AuthorId is not null)
            albums = albums.Where(a => a.Contributions.Any(c => c.AuthorId == AuthorId));
        if (PublisherId is not null)
            albums = albums.Where(a => a.Editions.Any(e => e.PublisherId == PublisherId));
        if (PublisherCollectionId is not null)
            albums = albums.Where(a => a.Editions.Any(e => e.PublisherCollectionId == PublisherCollectionId));
        if (GenreId is not null)
            albums = albums.WhereGenre(GenreId.Value);
        if (UniverseId is not null)
            albums = albums.WhereUniverse(await CatalogFilters.UniverseAndDescendantsAsync(context, UniverseId.Value, cancellationToken));
        return albums;
    }

    /// <summary>The records the list is filtered on, as the lists name them; none for a record that does not exist.</summary>
    public async Task<AlbumListFilters> NameAsync(BdthequeDbContext context, CancellationToken cancellationToken) => new(
        SeriesId is { } seriesId
            ? await context.Series.AsNoTracking().Where(s => s.Id == seriesId)
                .Select(s => new SeriesListItem(s.Id, s.Title)).SingleOrDefaultAsync(cancellationToken)
            : null,
        AuthorId is { } authorId
            ? await context.Authors.AsNoTracking().Where(a => a.Id == authorId)
                .Select(a => new AuthorListItem(a.Id, a.LastName, a.FirstName, a.Pseudonym)).SingleOrDefaultAsync(cancellationToken)
            : null,
        PublisherId is { } publisherId
            ? await context.Publishers.AsNoTracking().Where(p => p.Id == publisherId)
                .Select(p => new PublisherListItem(p.Id, p.Name, p.Website)).SingleOrDefaultAsync(cancellationToken)
            : null,
        PublisherCollectionId is { } collectionId
            ? await context.PublisherCollections.AsNoTracking().Where(c => c.Id == collectionId)
                .Select(c => new PublisherCollectionListItem(c.Id, c.Name, c.PublisherId, c.Publisher.Name)).SingleOrDefaultAsync(cancellationToken)
            : null,
        GenreId is { } genreId
            ? await context.Genres.AsNoTracking().Where(g => g.Id == genreId)
                .Select(g => new GenreListItem(g.Id, g.Label)).SingleOrDefaultAsync(cancellationToken)
            : null,
        UniverseId is { } universeId
            ? await context.Universes.AsNoTracking().Where(u => u.Id == universeId)
                .Select(CatalogExpressions.Expand((Universe u) => u.ToListItem())).SingleOrDefaultAsync(cancellationToken)
            : null);
}
