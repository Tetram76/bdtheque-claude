using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search and cross filters of a list of editions (choix-implementation/api/recherche.md), which the list of the
/// consultation and that of the administration share, bound as a whole from the query of their request
/// (<c>[AsParameters]</c>): the search is its parameter <c>q</c>, each other criterion that of its name.
/// </summary>
internal sealed record EditionFilters([FromQuery(Name = "q")] string? Text, Guid? SeriesId, Guid? AlbumId, Guid? PublisherId, Guid? PublisherCollectionId)
{
    /// <summary>
    /// Narrows <paramref name="editions"/>: an edition is found by its ISBN, separators ignored on both sides
    /// (<see cref="IsbnSearch"/>).
    /// </summary>
    public IQueryable<Edition> Apply(IQueryable<Edition> editions)
    {
        editions = editions.WhereContains(Text is null ? null : IsbnSearch.Normalize(Text), IsbnSearch.Normalized);
        if (SeriesId is not null)
            editions = editions.Where(e => e.Album.SeriesId == SeriesId);
        if (AlbumId is not null)
            editions = editions.Where(e => e.AlbumId == AlbumId);
        if (PublisherId is not null)
            editions = editions.Where(e => e.PublisherId == PublisherId);
        if (PublisherCollectionId is not null)
            editions = editions.Where(e => e.PublisherCollectionId == PublisherCollectionId);
        return editions;
    }

    /// <summary>The records the list is filtered on, as the lists name them; none for a record that does not exist.</summary>
    public async Task<EditionListFilters> NameAsync(BdthequeDbContext context, CancellationToken cancellationToken) => new(
        await FilteredRecords.SeriesAsync(context, SeriesId, cancellationToken),
        await FilteredRecords.AlbumAsync(context, AlbumId, cancellationToken),
        await FilteredRecords.PublisherAsync(context, PublisherId, cancellationToken),
        await FilteredRecords.PublisherCollectionAsync(context, PublisherCollectionId, cancellationToken));
}
