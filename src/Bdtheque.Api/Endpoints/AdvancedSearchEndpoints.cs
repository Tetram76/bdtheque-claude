using System.Data;
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
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The advanced search of the consultation (fonctionnel/recherche-avancee.md § Recherche avancée): the description of what it
/// queries, and the search of each entity, whose results are presented as the lists of the consultation
/// present it, in their order (choix-implementation/api.md § Recherche).
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
        foreach (var entity in Searches.Values)
            entity.Map(search);
    }

    /// <summary>
    /// The records of <paramref name="entity"/> that satisfy <paramref name="criteria"/>, as its own route of the
    /// advanced search returns them: a <see cref="Page{T}"/> of the entries of its list.
    /// </summary>
    public static Task<object> SearchAsync(
        EntityKind entity, SearchCondition? criteria, BdthequeDbContext context, int page, int pageSize, CancellationToken cancellationToken) =>
        Searches[entity].SearchAsync(criteria, context, page, pageSize, cancellationToken);

    // Declared before the searches, whose initialization reads them.
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

    private static readonly IReadOnlyDictionary<EntityKind, EntitySearch> Searches = new EntitySearch[]
    {
        new EntitySearch<Album, AlbumSummary>("albums", EntityKind.Album, albums => albums.OrderByAlbum(a => a), AlbumEndpoints.ListItem),
        new EntitySearch<Series, SeriesListItem>(
            "series", EntityKind.Series, series => series.OrderBy(s => s.SortKey).ThenBy(s => s.Id), s => new SeriesListItem(s.Id, s.Title)),
        new EntitySearch<Edition, EditionListItem>("editions", EntityKind.Edition,
            editions => editions.OrderByAlbum(e => e.Album).ThenBy(e => e.PublicationYear).ThenBy(e => e.Id), EditionEndpoints.ListItem),
        new EntitySearch<Author, AuthorListItem>("authors", EntityKind.Author,
            authors => authors.OrderBy(a => a.SortKey).ThenBy(a => a.Id), a => new AuthorListItem(a.Id, a.LastName, a.FirstName, a.Pseudonym)),
        new EntitySearch<Publisher, PublisherListItem>("publishers", EntityKind.Publisher,
            publishers => publishers.OrderBy(p => p.Name).ThenBy(p => p.Id), p => new PublisherListItem(p.Id, p.Name, p.Website)),
        new EntitySearch<PublisherCollection, PublisherCollectionListItem>("publisher-collections", EntityKind.PublisherCollection,
            collections => collections.OrderBy(c => c.Name).ThenBy(c => c.Publisher.Name).ThenBy(c => c.Id),
            c => new PublisherCollectionListItem(c.Id, c.Name, c.PublisherId, c.Publisher.Name)),
        new EntitySearch<Genre, GenreListItem>(
            "genres", EntityKind.Genre, genres => genres.OrderBy(g => g.Label).ThenBy(g => g.Id), g => new GenreListItem(g.Id, g.Label)),
        new EntitySearch<Universe, UniverseListItem>(
            "universes", EntityKind.Universe, universes => universes.OrderBy(u => u.Name).ThenBy(u => u.Id), UniverseEndpoints.ListItem),
        // The visuals of each edition in their order (fonctionnel/regles-metier.md § Ordre des visuels d'une édition).
        new EntitySearch<EditionVisual, EditionVisualListItem>("edition-visuals", EntityKind.EditionVisual,
            visuals => visuals.OrderByAlbum(v => v.Edition.Album).ThenBy(v => v.Edition.PublicationYear).ThenBy(v => v.EditionId)
                .ThenBy(v => v.Type).ThenBy(v => v.DisplayOrder).ThenBy(v => v.Id),
            VisualListItem),
        new EntitySearch<PurchaseIntent, PurchaseIntentListItem>("purchase-intents", EntityKind.PurchaseIntent,
            intents => intents.OrderByAlbum(p => p.Album).ThenBy(p => p.Id), PurchaseIntentEndpoints.ListItem),
        // In the order of the albums, the series standing in for the album of a contribution of their own.
        new EntitySearch<Contribution, ContributionListItem>("contributions", EntityKind.Contribution,
            contributions => contributions
                .OrderBy(c => c.Album != null ? c.Album.SortKey ?? c.Album.Series!.SortKey : c.Series!.SortKey)
                .ThenBy(c => c.Album!.VolumeNumber)
                .ThenBy(c => c.AlbumId)
                .ThenBy(c => c.Role)
                .ThenBy(c => c.Author.SortKey)
                .ThenBy(c => c.Id),
            ContributionListItem),
    }.ToDictionary(s => s.Entity);

    private abstract record EntitySearch(string Route, EntityKind Entity)
    {
        public abstract void Map(RouteGroupBuilder search);

        public abstract Task<object> SearchAsync(
            SearchCondition? criteria, BdthequeDbContext context, int page, int pageSize, CancellationToken cancellationToken);
    }

    /// <summary>The search of one entity: its route, the order of its results, and the entry of its list each one is presented as.</summary>
    private sealed record EntitySearch<T, TItem>(
        string Route, EntityKind Entity, Func<IQueryable<T>, IOrderedQueryable<T>> Order, Expression<Func<T, TItem>> Item)
        : EntitySearch(Route, Entity)
        where T : class
    {
        public override void Map(RouteGroupBuilder search) =>
            search.MapPost(
                $"/{Route}",
                (AdvancedSearchRequest request, BdthequeDbContext context, CancellationToken cancellationToken, int page = 1,
                    int pageSize = Paging.DefaultSize) => PageAsync(request.Criteria, context, page, pageSize, cancellationToken));

        public override async Task<object> SearchAsync(
            SearchCondition? criteria, BdthequeDbContext context, int page, int pageSize, CancellationToken cancellationToken) =>
            await PageAsync(criteria, context, page, pageSize, cancellationToken);

        private async Task<Page<TItem>> PageAsync(
            SearchCondition? criteria, BdthequeDbContext context, int page, int pageSize, CancellationToken cancellationToken)
        {
            var definition = AdvancedSearchSchema.Of(Entity);
            // Read only for the criteria that follow the hierarchy of the universes: a small reference table. Its depth
            // and the results are then read in a single snapshot, as any reads assembled into one another: a
            // universe moved deeper in between would otherwise escape a predicate built for the former depth.
            var followsUniverses = SearchTranslator.FollowsUniverseHierarchy(definition, criteria);
            await using var snapshot = followsUniverses
                ? await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken)
                : null;
            var universeLevels = followsUniverses ? (await UniverseTree.LoadAsync(context, cancellationToken)).MaxAncestorCount : 0;
            var condition = SearchTranslator.Translate<T>(definition, criteria, new SearchScope(context, universeLevels));

            return await Order(context.Set<T>().AsNoTracking().Where(condition)).ToPageAsync(page, pageSize, Item, cancellationToken);
        }
    }
}
