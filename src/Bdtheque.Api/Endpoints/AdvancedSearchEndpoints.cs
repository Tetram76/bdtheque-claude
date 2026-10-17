using System.Linq.Expressions;
using Bdtheque.Api.Search;
using Bdtheque.Api.Visuals;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The advanced search of the consultation (fonctionnel.md § Recherche avancée): the description of what it
/// queries, and the search of each entity, whose results are presented as the lists of the consultation
/// present it, in their order (choix-implementation.md § Recherche).
/// </summary>
/// <remarks>
/// A search is a <c>POST</c>: its criteria, a tree of conditions, are the body of the request. It reads
/// only, as any route of the consultation.
/// </remarks>
internal static class AdvancedSearchEndpoints
{
    public static void MapAdvancedSearch(this RouteGroupBuilder catalog)
    {
        var search = catalog.MapGroup("/search");
        search.MapGet("/schema", () => AdvancedSearchSchema.Description);

        Map<Album, AlbumSummary>(search, "albums", EntityKind.Album, albums => albums.OrderByAlbum(a => a), AlbumEndpoints.ListItem);
        Map<Series, SeriesListItem>(search, "series", EntityKind.Series, series => series.OrderBy(s => s.SortKey).ThenBy(s => s.Id), s => new SeriesListItem(s.Id, s.Title));
        Map<Edition, EditionListItem>(search, "editions", EntityKind.Edition,
            editions => editions.OrderByAlbum(e => e.Album).ThenBy(e => e.PublicationYear).ThenBy(e => e.Id), EditionEndpoints.ListItem);
        Map<Author, AuthorListItem>(search, "authors", EntityKind.Author,
            authors => authors.OrderBy(a => a.SortKey).ThenBy(a => a.Id), a => new AuthorListItem(a.Id, a.LastName, a.FirstName, a.Pseudonym));
        Map<Publisher, PublisherListItem>(search, "publishers", EntityKind.Publisher,
            publishers => publishers.OrderBy(p => p.Name).ThenBy(p => p.Id), p => new PublisherListItem(p.Id, p.Name, p.Website));
        Map<PublisherCollection, PublisherCollectionListItem>(search, "publisher-collections", EntityKind.PublisherCollection,
            collections => collections.OrderBy(c => c.Name).ThenBy(c => c.Publisher.Name).ThenBy(c => c.Id),
            c => new PublisherCollectionListItem(c.Id, c.Name, c.PublisherId, c.Publisher.Name));
        Map<Genre, GenreListItem>(search, "genres", EntityKind.Genre, genres => genres.OrderBy(g => g.Label).ThenBy(g => g.Id), g => new GenreListItem(g.Id, g.Label));
        Map<Universe, UniverseListItem>(search, "universes", EntityKind.Universe, universes => universes.OrderBy(u => u.Name).ThenBy(u => u.Id), UniverseEndpoints.ListItem);
        // The visuals of each edition in their order (fonctionnel.md § Ordre des visuels d'une édition).
        Map<EditionVisual, EditionVisualListItem>(search, "edition-visuals", EntityKind.EditionVisual,
            visuals => visuals.OrderByAlbum(v => v.Edition.Album).ThenBy(v => v.Edition.PublicationYear).ThenBy(v => v.EditionId)
                .ThenBy(v => v.Type).ThenBy(v => v.DisplayOrder).ThenBy(v => v.Id),
            VisualListItem);
        Map<PurchaseIntent, PurchaseIntentListItem>(search, "purchase-intents", EntityKind.PurchaseIntent,
            intents => intents.OrderByAlbum(p => p.Album).ThenBy(p => p.Id), PurchaseIntentEndpoints.ListItem);
        // In the order of the albums, the series standing in for the album of a contribution of their own.
        Map<Contribution, ContributionListItem>(search, "contributions", EntityKind.Contribution,
            contributions => contributions
                .OrderBy(c => c.Album != null ? c.Album.SortKey ?? c.Album.Series!.SortKey : c.Series!.SortKey)
                .ThenBy(c => c.Album!.VolumeNumber)
                .ThenBy(c => c.AlbumId)
                .ThenBy(c => c.Role)
                .ThenBy(c => c.Author.SortKey)
                .ThenBy(c => c.Id),
            ContributionListItem);
    }

    private static readonly Expression<Func<EditionVisual, EditionVisualListItem>> VisualListItem = CatalogExpressions.Expand(
        (EditionVisual v) => new EditionVisualListItem(
            v.Id,
            EnumMapping.Map<ContractEnums.VisualType>(v.Type)!.Value,
            VisualStorage.OriginalPath(v.MediaReference),
            VisualStorage.DisplayPath(v.MediaReference),
            v.Edition.Album.ToSummary(),
            v.Edition.ToSummary()));

    private static readonly Expression<Func<Contribution, ContributionListItem>> ContributionListItem = CatalogExpressions.Expand(
        (Contribution c) => new ContributionListItem(
            c.Id,
            EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value,
            new AuthorListItem(c.AuthorId, c.Author.LastName, c.Author.FirstName, c.Author.Pseudonym),
            c.Album == null ? null : c.Album.ToSummary(),
            c.Series == null ? null : new SeriesListItem(c.Series.Id, c.Series.Title)));

    private static void Map<T, TItem>(
        RouteGroupBuilder search, string route, EntityKind entity, Func<IQueryable<T>, IOrderedQueryable<T>> order,
        Expression<Func<T, TItem>> item)
        where T : class =>
        search.MapPost(
            $"/{route}",
            (AdvancedSearchRequest request, BdthequeDbContext context, CancellationToken cancellationToken, int page = 1,
                int pageSize = Paging.DefaultSize) => SearchAsync(entity, request, context, order, item, page, pageSize, cancellationToken));

    private static async Task<Page<TItem>> SearchAsync<T, TItem>(
        EntityKind entity, AdvancedSearchRequest request, BdthequeDbContext context, Func<IQueryable<T>, IOrderedQueryable<T>> order,
        Expression<Func<T, TItem>> item, int page, int pageSize, CancellationToken cancellationToken)
        where T : class
    {
        var definition = AdvancedSearchSchema.Of(entity);
        // Read only for the criteria that follow the hierarchy of the universes: a small reference table.
        var universeLevels = SearchTranslator.FollowsUniverseHierarchy(definition, request.Criteria)
            ? (await UniverseTree.LoadAsync(context, cancellationToken)).MaxAncestorCount
            : 0;
        var condition = SearchTranslator.Translate<T>(definition, request.Criteria, new SearchScope(context, universeLevels));

        return await order(context.Set<T>().AsNoTracking().Where(condition)).ToPageAsync(page, pageSize, item, cancellationToken);
    }
}
