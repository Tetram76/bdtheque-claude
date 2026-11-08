using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;

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
        await FilteredRecords.SeriesAsync(context, SeriesId, cancellationToken),
        await FilteredRecords.AuthorAsync(context, AuthorId, cancellationToken),
        await FilteredRecords.PublisherAsync(context, PublisherId, cancellationToken),
        await FilteredRecords.PublisherCollectionAsync(context, PublisherCollectionId, cancellationToken),
        await FilteredRecords.GenreAsync(context, GenreId, cancellationToken),
        await FilteredRecords.UniverseAsync(context, UniverseId, cancellationToken));
}
