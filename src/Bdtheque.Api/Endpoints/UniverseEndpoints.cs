using System.Data;
using System.Linq.Expressions;
using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the universes and of their hierarchy (<c>/admin/universes</c>).</summary>
internal static class UniverseEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: a universe is refused deletion while it has
    // sub-universes, and is only associated with albums and series.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Universe,
            (context, id) => context.Universes.Where(u => u.ParentId == id).Select(u => u.Id)),
        new(LinkNature.Association, EntityKind.Album,
            (context, id) => context.Albums.Where(a => a.Universes.Any(u => u.Id == id)).Select(a => a.Id)),
        new(LinkNature.Association, EntityKind.Series,
            (context, id) => context.Series.Where(s => s.Universes.Any(u => u.Id == id)).Select(s => s.Id)),
    ];

    public static void MapUniverses(this RouteGroupBuilder admin)
    {
        var universes = admin.MapGroup("/universes");
        universes.MapGet("/", ListForAdministrationAsync);
        universes.MapGet("/{id:guid}", GetAsync);
        universes.MapGet("/{id:guid}/hierarchy", GetHierarchyAsync);
        universes.MapPost("/", CreateAsync);
        universes.MapPut("/{id:guid}", UpdateAsync);
        universes.MapDeletion<Universe>(DeletionLinks);
    }

    public static void MapUniverseCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/universes", ListAsync);
        catalog.MapGet("/universes/{id:guid}/works", GetWorksAsync);
    }

    internal static readonly Expression<Func<Universe, UniverseListItem>> ListItem = CatalogExpressions.Expand((Universe u) => u.ToListItem());

    private static Task<Page<UniverseListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize) =>
        context.Universes.AsNoTracking()
            .WhereContains(q, u => u.Name)
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .ToPageAsync(page, pageSize, ListItem, cancellationToken);

    /// <summary>
    /// The series and albums of a universe, which has no record, with its ancestors
    /// (fonctionnel/structure-de-l-application.md): those of its sub-universes included
    /// (fonctionnel/regles-metier/hierarchie-des-univers.md), a series attached with all its albums, the universes of
    /// an album in a series being those
    /// of the album and of the series, and a series with those of its albums only that are attached.
    /// </summary>
    private static async Task<UniverseWorks> GetWorksAsync(
        Guid id, BdthequeDbContext context, CancellationToken cancellationToken, int page = 1, int pageSize = Paging.DefaultSize)
    {
        // A single snapshot for the reads assembled into one another.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var tree = await UniverseTree.LoadAsync(context, cancellationToken);
        var universe = tree.InHierarchy(id);
        var universeIds = tree.WithDescendants(id);

        var albums = context.Albums.AsNoTracking().WhereUniverse(universeIds);
        var series = context.Series.AsNoTracking()
            .Where(s => s.Universes.Any(u => universeIds.Contains(u.Id)) || albums.Any(a => a.SeriesId == s.Id));
        return new UniverseWorks(universe, await CatalogWorks.ReadPageAsync(context, series, albums, page, pageSize, cancellationToken));
    }

    /// <summary>
    /// The list of the universes of the administration (visuel/listes/liste-par-liste.md): in the order of the hierarchy, each
    /// universe followed by its sub-universes, siblings by name; the search on the name keeps the universes found in that
    /// order. Each row carries its ancestors, what it counts, its sub-universes included, its modification and its version.
    /// </summary>
    /// <remarks>
    /// The order of a tree is no order of the base: the hierarchy, a small reference table read whole
    /// (choix-implementation/api/organisation-de-l-api.md), is ordered in memory and paged there; only the rows of the page
    /// are then counted.
    /// </remarks>
    private static async Task<Page<UniverseAdminListItem>> ListForAdministrationAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, int page = 1, int pageSize = Paging.DefaultSize)
    {
        // A single snapshot: the rows counted are those of the hierarchy read.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var tree = await UniverseTree.LoadAsync(context, cancellationToken);
        var found = string.IsNullOrWhiteSpace(q)
            ? null
            : (await context.Universes.AsNoTracking().WhereContains(q, u => u.Name).Select(u => u.Id).ToListAsync(cancellationToken)).ToHashSet();
        var ordered = tree.InHierarchyOrder().Where(id => found is null || found.Contains(id)).ToList();
        var ids = Paging.Slice(ordered, page, pageSize);

        var rows = await context.Universes.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.ModifiedAt, Version = EF.Property<uint>(u, BdthequeDbContext.VersionProperty) })
            .ToDictionaryAsync(u => u.Id, cancellationToken);
        var counts = await CountAsync(context, tree, ids, cancellationToken);
        var items = ids.Select(id =>
        {
            var (universe, ancestors) = tree.InHierarchy(id);
            var (albums, series) = counts[id];
            return new UniverseAdminListItem(
                id, universe.Name, universe.ParentId, ancestors, albums, series, tree.WithDescendants(id).Count - 1, rows[id].ModifiedAt,
                rows[id].Version);
        }).ToList();
        return new Page<UniverseAdminListItem>(items, page, pageSize, ordered.Count);
    }

    /// <summary>
    /// The albums and series of each universe, those of its sub-universes included, as the lists filtered on it find them
    /// (<see cref="AlbumFilters"/>, <see cref="SeriesFilters"/>), in a single query: one count per universe,
    /// joined by <c>UNION ALL</c>.
    /// </summary>
    private static async Task<Dictionary<Guid, (int Albums, int Series)>> CountAsync(
        BdthequeDbContext context, UniverseTree tree, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];
        var counts = ids.Select(id =>
            {
                var scope = tree.WithDescendants(id);
                var albums = context.Albums.WhereUniverse(scope);
                var series = context.Series.WhereUniverse(scope);
                return context.Universes.Where(u => u.Id == id).Select(u => new { u.Id, Albums = albums.Count(), Series = series.Count() });
            })
            .Aggregate(Queryable.Concat);
        return await counts.ToDictionaryAsync(c => c.Id, c => (c.Albums, c.Series), cancellationToken);
    }

    private static async Task<UniverseForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var universe = await context.Universes.AsNoTracking()
                           .Where(u => u.Id == id)
                           .Select(u => new
                           {
                               u.Id, u.Name, u.Description, u.ParentId, u.CreatedAt, u.ModifiedAt,
                               Version = EF.Property<uint>(u, BdthequeDbContext.VersionProperty),
                           })
                           .SingleOrDefaultAsync(cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Universe), id);
        var parent = universe.ParentId is { } parentId ? (await UniverseTree.LoadAsync(context, cancellationToken)).InHierarchy(parentId) : null;
        return new UniverseForm(
            universe.Id, universe.Name, universe.Description, universe.ParentId, parent, universe.CreatedAt, universe.ModifiedAt, universe.Version);
    }

    /// <summary>
    /// The universe with its ancestors, as an entry form presents a universe it adds (visuel/saisie-album/contenu.md).
    /// </summary>
    private static async Task<UniverseInHierarchy> GetHierarchyAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        (await UniverseTree.LoadAsync(context, cancellationToken)).InHierarchy(id);

    private static async Task<Created<UniverseForm>> CreateAsync(
        CreateUniverseRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var universe = new Universe(request.Name);
        universe.SetDescription(request.Description);
        if (request.ParentId is { } parentId)
        {
            await UniverseHierarchy.LockAsync(context, cancellationToken);
            universe.SetParent(await LoadWithAncestorsAsync(context, parentId, cancellationToken));
        }

        context.Universes.Add(universe);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/universes/{universe.Id}", await GetAsync(universe.Id, context, cancellationToken));
    }

    private static async Task<UniverseForm> UpdateAsync(
        Guid id, UpdateUniverseRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Before the row lock: two crossed moves (A under C, C under A) would otherwise each hold the
        // row whose foreign key check the other needs, while waiting for the hierarchy — a deadlock.
        // Whether the parent actually changes is only known once loaded, hence any parent assignment.
        if (request.ParentId is not null)
            await UniverseHierarchy.LockAsync(context, cancellationToken);
        var universe = await context.LoadAggregateForWriteAsync<Universe>(id, request.Version, cancellationToken: cancellationToken);
        universe.SetName(request.Name);
        universe.SetDescription(request.Description);
        if (request.ParentId != universe.ParentId)
        {
            universe.SetParent(request.ParentId is { } parentId
                ? await LoadWithAncestorsAsync(context, parentId, cancellationToken)
                : null);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(universe.Id, context, cancellationToken);
    }

    /// <summary>
    /// Loads a universe with its whole ancestor chain, which <see cref="Universe.SetParent"/> needs to
    /// rule out a cycle: the universes form a small referential, loaded whole rather than walked
    /// recursively (choix-implementation/api/organisation-de-l-api.md). The caller holds the hierarchy
    /// lock, so that the check sees every change of parent committed before it.
    /// </summary>
    internal static async Task<Universe> LoadWithAncestorsAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken)
    {
        var universes = await context.Universes.ToListAsync(cancellationToken);
        return universes.SingleOrDefault(u => u.Id == id) ?? throw new EntityNotFoundException(typeof(Universe), id);
    }
}
