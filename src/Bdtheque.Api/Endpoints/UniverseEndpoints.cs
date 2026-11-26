using System.Data;
using System.Linq.Expressions;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Errors;
using Bdtheque.Api.Merge;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContributionRole = Bdtheque.Contracts.Enums.ContributionRole;
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
        universes.MapGet("/merge", GetMergeAsync);
        universes.MapPost("/merge", MergeAsync);
    }

    /// <summary>
    /// Two universes to merge (fonctionnel/regles-metier/fusion-des-fiches.md): each with its parent, and whether that parent
    /// can be the one of the universe that remains; their homonymous sub-universes, merged with them at every level; and the
    /// series, albums and sub-universes that pass to the universe that remains, counted as the list of the universes counts
    /// them (visuel/listes/liste-par-liste.md), sub-universes included, what both hold counted once.
    /// </summary>
    private static async Task<UniverseMergePreview> GetMergeAsync(Guid first, Guid second, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot: the hierarchy, the pairs and the counts are those of the two universes read.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var tree = await UniverseTree.LoadAsync(context, cancellationToken);
        var (firstScope, secondScope) = (tree.WithDescendants(first), tree.WithDescendants(second));
        // Neither of the two universes nor any of their sub-universes can be the parent of the one that remains.
        var forbidden = firstScope.Union(secondScope).ToHashSet();
        var pairs = UniverseMergePairs.Of(tree, await UniverseMergePairs.HomonymsAsync(context, cancellationToken), first, second);

        var subRecords = await context.Universes.AsNoTracking()
            .Where(u => pairs.Select(p => p.First).Contains(u.Id) || pairs.Select(p => p.Second).Contains(u.Id))
            .Select(u => new UniverseMergeSubRecord(u.Id, u.Name, u.Description, u.ModifiedAt, EF.Property<uint>(u, BdthequeDbContext.VersionProperty)))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var links = (await RecordMerge.CountLinksAsync(context, first, second,
            [
                (EntityKind.Series, (c, id) => c.Series.AsNoTracking().WhereUniverse(tree.WithDescendants(id)).Select(s => s.Id)),
                (EntityKind.Album, (c, id) => c.Albums.AsNoTracking().WhereUniverse(tree.WithDescendants(id)).Select(a => a.Id)),
            ], cancellationToken)).ToList();
        // The sub-universes of each, the two merged excepted; a pair of homonyms, and a sub-universe of both (one universe
        // under the other), pass as one.
        var (ofFirst, ofSecond) = (firstScope.Except([first, second]).ToHashSet(), secondScope.Except([first, second]).ToHashSet());
        var total = ofFirst.Union(ofSecond).Count() - pairs.Count;
        links.Add(new MergeLinkCount(EntityKind.Universe, total, ofFirst.Count, ofSecond.Count, ofFirst.Count + ofSecond.Count - total));

        return new UniverseMergePreview(
            await MergeRecordAsync(context, tree, first, forbidden, cancellationToken),
            await MergeRecordAsync(context, tree, second, forbidden, cancellationToken),
            [.. pairs.Select(p => new UniverseMergePair(p.Path, subRecords[p.First], subRecords[p.Second]))],
            [.. links.OrderBy(l => Bdtheque.Contracts.Enums.EntityKindOrder.Of(l.Kind))]);
    }

    private static async Task<UniverseMergeRecord> MergeRecordAsync(
        BdthequeDbContext context, UniverseTree tree, Guid id, HashSet<Guid> forbidden, CancellationToken cancellationToken)
    {
        var universe = await context.Universes.AsNoTracking()
                           .Where(u => u.Id == id)
                           .Select(u => new { u.Id, u.Name, u.Description, u.ParentId, u.ModifiedAt, Version = EF.Property<uint>(u, BdthequeDbContext.VersionProperty) })
                           .SingleOrDefaultAsync(cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Universe), id);
        return new UniverseMergeRecord(
            universe.Id, universe.Name, universe.Description, universe.ParentId is { } parentId ? tree.InHierarchy(parentId) : null,
            universe.ParentId is not { } parent || !forbidden.Contains(parent), universe.ModifiedAt, universe.Version);
    }

    /// <summary>
    /// Merges two universes (fonctionnel/regles-metier/fusion-des-fiches.md): the universe that remains keeps each data chosen,
    /// takes the series, albums and sub-universes of both, and each pair of homonymous sub-universes is merged in the same way,
    /// at every level. The pairs the client chose on must be those of the hierarchy, which the lock of the hierarchy keeps from
    /// changing until the merge is done.
    /// </summary>
    private static async Task<MergeResult> MergeAsync(UniverseMergeRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // The lock of the hierarchy before any row lock, as any change of parent takes it, in the transaction of the merge.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await UniverseHierarchy.LockAsync(context, cancellationToken);
        // The pairs, each with its universe that remains and the one it absorbs, in the order of the hierarchy: a pair after the
        // one it stands under.
        var pairs = new List<UniverseMergeStep>();
        var result = await RecordMerge.MergeAsync<Universe>(
            context, request.First, request.Second, RecordMerge.KeepsFirst([request.Name, request.Description, request.Parent]),
            pair => AssignMerge(request, pair, pairs),
            async pair =>
            {
                pairs.AddRange(await MergePairsAsync(context, request, pair, cancellationToken));
                return await TransferMergedAsync(context, pair, pairs, cancellationToken);
            },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    // A pair of homonymous sub-universes merged with the two universes: the one that remains, the one it absorbs, the universe it
    // stands under, and what the user chose on it.
    private sealed record UniverseMergeStep(Universe Kept, Universe Merged, Guid? Under, UniverseMergePairChoice Choice)
    {
        public Universe Of(MergeSide side) => (side == MergeSide.First) == (Choice.First.Id == Kept.Id) ? Kept : Merged;
    }

    // The pairs of the hierarchy, which must be those the user chose on, each sub-universe locked at the version read.
    private static async Task<IReadOnlyList<UniverseMergeStep>> MergePairsAsync(
        BdthequeDbContext context, UniverseMergeRequest request, MergePair<Universe> pair, CancellationToken cancellationToken)
    {
        var tree = await UniverseTree.LoadAsync(context, cancellationToken);
        var found = UniverseMergePairs.Of(tree, await UniverseMergePairs.HomonymsAsync(context, cancellationToken), pair.First.Id, pair.Second.Id);
        // Chosen on other pairs than the hierarchy now holds: a sub-universe added, renamed or moved since the reading.
        if (found.Count != request.Pairs.Count
            || !found.Select(p => (p.First, p.Second)).ToHashSet().SetEquals(request.Pairs.Select(p => (p.First.Id, p.Second.Id))))
            throw new DbUpdateConcurrencyException("The homonymous sub-universes of the merged universes changed since they were read.");

        foreach (var record in request.Pairs.SelectMany(p => new[] { p.First, p.Second }).OrderBy(r => r.Id))
            await context.LockAtVersionAsync<Universe>(record.Id, record.Version, cancellationToken);
        // The whole referential, tracked, which the moves of the merge check for cycles.
        var universes = (await context.Universes.ToListAsync(cancellationToken)).ToDictionary(u => u.Id);
        return
        [
            .. found.Select(f =>
            {
                var choice = request.Pairs.Single(c => c.First.Id == f.First && c.Second.Id == f.Second);
                var (first, second) = (universes[f.First], universes[f.Second]);
                return RecordMerge.KeepsFirst([choice.Name, choice.Description])
                    ? new UniverseMergeStep(first, second, f.Under, choice)
                    : new UniverseMergeStep(second, first, f.Under, choice);
            }),
        ];
    }

    // The series and albums of every universe merged pass to the one that remains in its place.
    private static async Task<IReadOnlyList<MergeReplacement>> TransferMergedAsync(
        BdthequeDbContext context, MergePair<Universe> pair, IReadOnlyList<UniverseMergeStep> pairs, CancellationToken cancellationToken)
    {
        var replaced = pairs.Select(p => (p.Merged, p.Kept)).Append((pair.Merged, pair.Kept)).ToList();
        var mergedIds = replaced.Select(r => r.Merged.Id).ToList();
        foreach (var series in await RecordMerge.LoadLinkedForWriteAsync<Series>(
                     context, context.Series.Where(s => s.Universes.Any(u => mergedIds.Contains(u.Id))).Select(s => s.Id), cancellationToken))
        {
            await context.Entry(series).Collection(s => s.Universes).LoadAsync(cancellationToken);
            foreach (var (merged, kept) in replaced)
                series.TransferUniverse(merged, kept);
        }

        foreach (var album in await RecordMerge.LoadLinkedForWriteAsync<Album>(
                     context, context.Albums.Where(a => a.Universes.Any(u => mergedIds.Contains(u.Id))).Select(a => a.Id), cancellationToken))
        {
            await context.Entry(album).Collection(a => a.Universes).LoadAsync(cancellationToken);
            foreach (var (merged, kept) in replaced)
                album.TransferUniverse(merged, kept);
        }

        return [.. pairs.Select(p => new MergeReplacement(EntityKind.Universe, p.Merged.Id, p.Kept.Id))];
    }

    // Each universe that remains absorbs the other one and takes the data chosen: the two universes, then each pair.
    private static void AssignMerge(UniverseMergeRequest request, MergePair<Universe> pair, IReadOnlyList<UniverseMergeStep> pairs)
    {
        // Read from both before the kept universes change.
        var (name, description, parent) =
            (pair.Of(request.Name).Name, pair.Of(request.Description).Description, pair.Of(request.Parent).Parent);
        var chosen = pairs.Select(p => (p.Kept, p.Of(p.Choice.Name).Name, p.Of(p.Choice.Description).Description)).ToList();
        var leftOut = pairs.SelectMany(p => new[] { p.Kept.Id, p.Merged.Id }).Append(pair.Merged.Id).ToHashSet();

        pair.Kept.Absorb(pair.Merged, parent, leftOut);
        foreach (var step in pairs)
            step.Kept.Absorb(step.Merged, KeptUnder(step.Under), leftOut);
        pair.Kept.SetName(name);
        pair.Kept.SetDescription(description);
        foreach (var (kept, keptName, keptDescription) in chosen)
        {
            kept.SetName(keptName);
            kept.SetDescription(keptDescription);
        }

        // The universe that remains where a pair stands: under the kept universe of the pair above it, or of the merge.
        Universe KeptUnder(Guid? under) => under is { } id ? pairs.Single(p => p.Kept.Id == id || p.Merged.Id == id).Kept : pair.Kept;
    }

    public static void MapUniverseCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/universes", ListAsync);
        catalog.MapGet("/universes/{id:guid}", GetDetailAsync);
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
    /// The record of a universe in the consultation (visuel/fiche-univers/): its description, its ancestors and its
    /// sub-universes, and its series and albums, those of its sub-universes included
    /// (fonctionnel/regles-metier/hierarchie-des-univers.md), presented as the bibliography of an author, whole, a record
    /// having no filter (fonctionnel/structure-de-l-application.md).
    /// </summary>
    /// <remarks>
    /// A series attached itself comes with all its albums, the universes of an album in a series being those of the album and
    /// of the series (fonctionnel/regles-metier/genres-et-univers-d-un-album.md); a series attached through some of its albums
    /// only, with those only. Bounded, like the bibliography of an author, the list is whole.
    /// </remarks>
    private static async Task<UniverseDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot for the reads assembled into one another.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var tree = await UniverseTree.LoadAsync(context, cancellationToken);
        var (item, ancestors) = tree.InHierarchy(id);
        var universe = await context.Universes.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Description, u.CreatedAt, u.ModifiedAt })
            .SingleAsync(cancellationToken);
        var universeIds = tree.WithDescendants(id);

        var (series, albums) = WorksOf(context, universeIds);
        var works = await CatalogWorks.ReadBibliographyAsync(
            context, series, albums,
            (Series s) => new CatalogWorks.BibliographySeries(new SeriesListItem(s.Id, s.Title), s.Universes.Any(u => universeIds.Contains(u.Id)), new List<ContributionRole>()),
            CatalogExpressions.Expand((Album a) => new AlbumBibliographyItem(
                a.ToCard(), new List<ContributionRole>(), a.FirstPublicationYear, a.FirstPublicationMonth)),
            cancellationToken);

        // The whole tree of its universe at the top, where it stands (visuel/fiche-univers/contenu.md).
        var whole = tree.WithSubUniverses(ancestors.Count > 0 ? ancestors[0].Id : id);
        var counts = await CountWorksAsync(context, tree, whole, cancellationToken);
        return new UniverseDetail(
            id, item.Name, universe.Description, ancestors,
            [.. whole.Select(u => new UniverseInTree(u.Id, u.Name, u.ParentId, counts[u.Id].Series, counts[u.Id].Albums))],
            works, universe.CreatedAt, universe.ModifiedAt);
    }

    /// <summary>
    /// What the record of a universe presents (visuel/fiche-univers/contenu.md), its sub-universes included: its albums, attached
    /// themselves or through their series, and its series, attached themselves or through one of these albums.
    /// </summary>
    private static (IQueryable<Series> Series, IQueryable<Album> Albums) WorksOf(BdthequeDbContext context, IReadOnlyCollection<Guid> universeIds)
    {
        var albums = context.Albums.AsNoTracking().WhereUniverse(universeIds);
        var series = context.Series.AsNoTracking()
            .Where(s => s.Universes.Any(u => universeIds.Contains(u.Id)) || albums.Any(a => a.SeriesId == s.Id));
        return (series, albums);
    }

    /// <summary>
    /// The numbers of series and albums the record of each universe of a tree presents (<see cref="WorksOf"/>), so that its line
    /// in the tree on another record says what its own record says; in a single query, one count per universe joined by
    /// <c>UNION ALL</c>.
    /// </summary>
    private static async Task<Dictionary<Guid, (int Series, int Albums)>> CountWorksAsync(
        BdthequeDbContext context, UniverseTree tree, IReadOnlyList<UniverseListItem> universes, CancellationToken cancellationToken)
    {
        if (universes.Count == 0)
            return [];
        var counts = universes.Select(universe =>
            {
                var (series, albums) = WorksOf(context, tree.WithDescendants(universe.Id));
                var id = universe.Id;
                return context.Universes.Where(u => u.Id == id).Select(u => new { u.Id, Series = series.Count(), Albums = albums.Count() });
            })
            .Aggregate(Queryable.Concat);
        return await counts.ToDictionaryAsync(c => c.Id, c => (c.Series, c.Albums), cancellationToken);
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

    /// <summary>
    /// Creates a universe, under its parent if given: an existing universe, or one its panel created on the spot
    /// (choix-implementation/socle/frontend.md, création sur place), created with it, at the top of the hierarchy.
    /// </summary>
    private static async Task<Created<UniverseForm>> CreateAsync(
        CreateUniverseRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (request.ParentId is not null)
            await UniverseHierarchy.LockAsync(context, cancellationToken);
        var added = await FormReferences.CreateAddedAsync(context, fields, request.NewRecords, cancellationToken);
        var universe = fields.Create("", () => new Universe(request.Name));
        universe.SetDescription(request.Description);
        if (request.ParentId is { } parentId)
            universe.SetParent(await ParentAsync(context, parentId, added, cancellationToken));

        context.Universes.Add(universe);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/universes/{universe.Id}", await GetAsync(universe.Id, context, cancellationToken));
    }

    private static async Task<UniverseForm> UpdateAsync(
        Guid id, UpdateUniverseRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Before the row lock: two crossed moves (A under C, C under A) would otherwise each hold the
        // row whose foreign key check the other needs, while waiting for the hierarchy — a deadlock.
        // Whether the parent actually changes is only known once loaded, hence any parent assignment.
        if (request.ParentId is not null)
            await UniverseHierarchy.LockAsync(context, cancellationToken);
        var universe = await context.LoadAggregateForWriteAsync<Universe>(id, request.Version, cancellationToken: cancellationToken);
        fields.Place(universe);
        var added = await FormReferences.CreateAddedAsync(context, fields, request.NewRecords, cancellationToken);
        universe.SetName(request.Name);
        universe.SetDescription(request.Description);
        if (request.ParentId != universe.ParentId)
        {
            universe.SetParent(request.ParentId is { } parentId
                ? await ParentAsync(context, parentId, added, cancellationToken)
                : null);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(universe.Id, context, cancellationToken);
    }

    // The parent of a universe: the one its panel created on the spot, under its key, at the top of the hierarchy; an
    // existing one otherwise, with its ancestors.
    private static async Task<Universe> ParentAsync(
        BdthequeDbContext context, Guid parentId, AddedRecords added, CancellationToken cancellationToken) =>
        added.Universes.TryGetValue(parentId, out var parent)
            ? parent
            : await LoadWithAncestorsAsync(context, parentId, cancellationToken);

    /// <summary>
    /// Loads a universe with its whole ancestor chain, which <see cref="Universe.SetParent"/> needs to
    /// rule out a cycle: the universes form a small referential, loaded whole rather than walked
    /// recursively (choix-implementation/api/organisation-de-l-api.md). The caller holds the hierarchy
    /// lock, so that the check sees every change of parent committed before it.
    /// </summary>
    private static async Task<Universe> LoadWithAncestorsAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken)
    {
        var universes = await context.Universes.ToListAsync(cancellationToken);
        return universes.SingleOrDefault(u => u.Id == id) ?? throw new EntityNotFoundException(typeof(Universe), id);
    }
}
