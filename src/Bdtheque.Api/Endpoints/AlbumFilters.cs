using System.Linq.Expressions;
using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search, navigation by initial and cross filters of a list of albums (choix-implementation/api/recherche.md), and the
/// state of the album with regard to the collection (fonctionnel/regles-metier/appartenance-a-la-collection.md), which the list
/// of the consultation and that of the administration share, bound as a whole from the query of their request
/// (<c>[AsParameters]</c>): the search is its parameter <c>q</c>, each other criterion that of its name.
/// </summary>
internal sealed record AlbumFilters(
    [FromQuery(Name = "q")] string? Text, string? Entry, Guid? SeriesId, Guid? AuthorId, Guid? PublisherId, Guid? PublisherCollectionId, Guid? GenreId,
    Guid? UniverseId, AlbumCollectionState? State)
{
    /// <summary>
    /// The editions the list shows of each album, among which a list of the consultation takes its cover
    /// (fonctionnel/regles-metier/couverture-d-un-album.md): the owned ones under « Possédé », those targeted by an intent
    /// under « Intention », all of them otherwise; an album « Non possédé » has none.
    /// </summary>
    public Expression<Func<Edition, bool>> EditionsShown => State switch
    {
        AlbumCollectionState.InCollection => CollectionMembership.IsOwned,
        AlbumCollectionState.PurchaseIntent => CollectionMembership.IsTargetedByIntent,
        _ => AllEditions,
    };

    private static readonly Expression<Func<Edition, bool>> AllEditions = _ => true;

    /// <summary>
    /// Narrows <paramref name="albums"/>: an album is found by its title or by that of its series, part of its label
    /// (fonctionnel/presentation/libelle-d-un-album.md), and an album without a title of its own is filed under the current
    /// entry of its series.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    public async Task<IQueryable<Album>> ApplyAsync(IQueryable<Album> albums, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        albums = albums.WhereContains(Text, a => a.Title, a => a.Series!.Title).WhereAlbumEntry(a => a, Entry);
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
        if (State is { } kept)
            albums = albums.Where(CatalogExpressions.Expand((Album a) => a.CollectionState() == kept));
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
