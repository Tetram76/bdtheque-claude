using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search, navigation by initial and cross filters of a list of editions (choix-implementation/api/recherche.md), which
/// the list of the consultation and that of the administration share, bound as a whole from the query of their request
/// (<c>[AsParameters]</c>): the search is its parameter <c>q</c>, each other criterion that of its name.
/// </summary>
/// <param name="InCollection">
/// Only the editions of the collection, when <c>true</c>: an edition a purchase intent targets is presented as the intent, never
/// as an edition (fonctionnel/regles-metier/intention-d-acquisition.md), as by the search of the consultation.
/// </param>
/// <param name="ByAlbum">
/// The search finds an edition by the title of its album or of its series too, when <c>true</c>: that of the dialog of the merge of
/// an edition, which an edition without ISBN must not escape (visuel/liste-albums/contenu.md, fusion). The list searches the ISBN
/// only (visuel/listes/liste-par-liste.md).
/// </param>
internal sealed record EditionFilters(
    [FromQuery(Name = "q")] string? Text, string? Entry, Guid? SeriesId, Guid? AlbumId, Guid? PublisherId, Guid? PublisherCollectionId,
    bool? InCollection, bool? ByAlbum)
{
    /// <summary>
    /// Narrows <paramref name="editions"/>: an edition is found by its ISBN, separators ignored on both sides
    /// (<see cref="IsbnSearch"/>), or by its album with <see cref="ByAlbum"/>, and filed under the entry of its album
    /// (visuel/listes/liste-par-liste.md).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    public IQueryable<Edition> Apply(IQueryable<Edition> editions)
    {
        editions = Searched(editions).WhereAlbumEntry(e => e.Album, Entry);
        if (SeriesId is not null)
            editions = editions.Where(e => e.Album.SeriesId == SeriesId);
        if (AlbumId is not null)
            editions = editions.Where(e => e.AlbumId == AlbumId);
        if (PublisherId is not null)
            editions = editions.Where(e => e.PublisherId == PublisherId);
        if (PublisherCollectionId is not null)
            editions = editions.Where(e => e.PublisherCollectionId == PublisherCollectionId);
        if (InCollection is true)
            editions = editions.Where(CollectionMembership.IsOwned);
        return editions;
    }

    // The ISBN and the titles are searched each as it is compared (separators ignored for the ISBN), so in two queries: one text
    // matching either one.
    private IQueryable<Edition> Searched(IQueryable<Edition> editions)
    {
        var byIsbn = editions.WhereContains(Text is null ? null : IsbnSearch.Normalize(Text), IsbnSearch.Normalized);
        if (ByAlbum is not true || string.IsNullOrWhiteSpace(Text))
            return byIsbn;

        var isbnIds = byIsbn.Select(e => e.Id);
        var albumIds = editions.WhereContains(Text, e => e.Album.Title, e => e.Album.Series!.Title).Select(e => e.Id);
        return editions.Where(e => isbnIds.Contains(e.Id) || albumIds.Contains(e.Id));
    }

    /// <summary>The records the list is filtered on, as the lists name them; none for a record that does not exist.</summary>
    public async Task<EditionListFilters> NameAsync(BdthequeDbContext context, CancellationToken cancellationToken) => new(
        await FilteredRecords.SeriesAsync(context, SeriesId, cancellationToken),
        await FilteredRecords.AlbumAsync(context, AlbumId, cancellationToken),
        await FilteredRecords.PublisherAsync(context, PublisherId, cancellationToken),
        await FilteredRecords.PublisherCollectionAsync(context, PublisherCollectionId, cancellationToken));
}
