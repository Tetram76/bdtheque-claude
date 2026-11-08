using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search, navigation by initial and cross filters of a list of series (choix-implementation/api/recherche.md), which
/// the list of the consultation and that of the administration share, bound as a whole from the query of their request
/// (<c>[AsParameters]</c>): the search is its parameter <c>q</c>, each other criterion that of its name.
/// </summary>
internal sealed record SeriesFilters(
    [FromQuery(Name = "q")] string? Text, string? Entry, Guid? AuthorId, Guid? PublisherId, Guid? PublisherCollectionId, Guid? GenreId,
    Guid? UniverseId)
{
    /// <summary>
    /// Narrows <paramref name="series"/>: a series is found by its title, and is credited, published or collected on itself —
    /// its contributions, publisher and collection are data of the series as much as a template
    /// (modele-metier/relations-et-cardinalites.md#série--relations) — or through its albums.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    public async Task<IQueryable<Series>> ApplyAsync(IQueryable<Series> series, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        CatalogFilters.EnsureNavigationEntry(Entry);
        series = series.WhereContains(Text, s => s.Title);
        if (Entry is not null)
            series = series.Where(s => s.NavigationEntry == Entry);
        if (AuthorId is not null)
            series = series.Where(s =>
                s.TemplateContributions.Any(c => c.AuthorId == AuthorId)
                || context.Albums.Any(a => a.SeriesId == s.Id && a.Contributions.Any(c => c.AuthorId == AuthorId)));
        if (PublisherId is not null)
            series = series.Where(s =>
                s.TemplatePublisherId == PublisherId
                || context.Albums.Any(a => a.SeriesId == s.Id && a.Editions.Any(e => e.PublisherId == PublisherId)));
        if (PublisherCollectionId is not null)
            series = series.Where(s =>
                s.TemplatePublisherCollectionId == PublisherCollectionId
                || context.Albums.Any(a => a.SeriesId == s.Id && a.Editions.Any(e => e.PublisherCollectionId == PublisherCollectionId)));
        if (GenreId is not null)
            series = series.Where(s => s.Genres.Any(g => g.Id == GenreId));
        if (UniverseId is not null)
        {
            var universeIds = await CatalogFilters.UniverseAndDescendantsAsync(context, UniverseId.Value, cancellationToken);
            series = series.Where(s => s.Universes.Any(u => universeIds.Contains(u.Id)));
        }

        return series;
    }

    /// <summary>The records the list is filtered on, as the lists name them; none for a record that does not exist.</summary>
    public async Task<SeriesListFilters> NameAsync(BdthequeDbContext context, CancellationToken cancellationToken) => new(
        await FilteredRecords.AuthorAsync(context, AuthorId, cancellationToken),
        await FilteredRecords.PublisherAsync(context, PublisherId, cancellationToken),
        await FilteredRecords.PublisherCollectionAsync(context, PublisherCollectionId, cancellationToken),
        await FilteredRecords.GenreAsync(context, GenreId, cancellationToken),
        await FilteredRecords.UniverseAsync(context, UniverseId, cancellationToken));
}
