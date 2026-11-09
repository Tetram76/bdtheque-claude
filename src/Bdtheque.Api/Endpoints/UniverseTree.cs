using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The hierarchy of the universes, read whole — a small reference table — rather than walked by
/// recursive queries (choix-implementation/api/organisation-de-l-api.md): the ancestors of a universe, and
/// its descendants, to whose records it is attached too (fonctionnel/regles-metier/hierarchie-des-univers.md).
/// </summary>
internal sealed class UniverseTree
{
    private readonly Dictionary<Guid, UniverseListItem> _universes;
    private readonly ILookup<Guid?, Guid> _children;

    private UniverseTree(List<UniverseListItem> universes)
    {
        _universes = universes.ToDictionary(u => u.Id);
        // A lookup keeps the order of its source: the children of a universe are by name.
        _children = universes.ToLookup(u => u.ParentId, u => u.Id);
    }

    // By name in the order of the base, French (choix-implementation/persistance/collation-des-colonnes-texte.md), then by
    // identifier for a total order: the order of the siblings in the hierarchy.
    public static async Task<UniverseTree> LoadAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        new(await context.Universes.AsNoTracking()
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(CatalogExpressions.Expand((Universe u) => u.ToListItem()))
            .ToListAsync(cancellationToken));

    /// <summary>
    /// Every universe in the order of the hierarchy (visuel/listes/liste-par-liste.md): each followed by its sub-universes,
    /// siblings by name.
    /// </summary>
    public IEnumerable<Guid> InHierarchyOrder()
    {
        var pending = new Stack<Guid>(_children[null].Reverse());
        while (pending.TryPop(out var id))
        {
            yield return id;
            foreach (var childId in _children[id].Reverse())
                pending.Push(childId);
        }
    }

    /// <summary>
    /// The universes a record is attached to, each with its ancestors; the hierarchy is only read for a
    /// record attached to a universe.
    /// </summary>
    public static async Task<IReadOnlyList<UniverseInHierarchy>> InHierarchyAsync(
        BdthequeDbContext context, IReadOnlyCollection<UniverseListItem> universes, CancellationToken cancellationToken)
    {
        if (universes.Count == 0)
            return [];
        var tree = await LoadAsync(context, cancellationToken);
        return universes.Select(u => tree.InHierarchy(u)).ToList();
    }

    /// <exception cref="EntityNotFoundException">No such universe: a functional error, the universe being addressed.</exception>
    public UniverseInHierarchy InHierarchy(Guid id) =>
        _universes.TryGetValue(id, out var universe) ? InHierarchy(universe) : throw new EntityNotFoundException(typeof(Universe), id);

    /// <summary>The universe with its ancestors, from the top of the hierarchy down to its parent.</summary>
    public UniverseInHierarchy InHierarchy(UniverseListItem universe)
    {
        var ancestors = new List<UniverseListItem>();
        for (var parentId = universe.ParentId; parentId is { } id && _universes.TryGetValue(id, out var ancestor); parentId = ancestor.ParentId)
            ancestors.Insert(0, ancestor);
        return new UniverseInHierarchy(universe, ancestors);
    }

    /// <summary>The most ancestors a universe has: the depth of the hierarchy below its top.</summary>
    public int MaxAncestorCount => _universes.Values.Select(u => InHierarchy(u).Ancestors.Count).DefaultIfEmpty().Max();

    /// <summary>The universe and its descendants at every level.</summary>
    public IReadOnlyCollection<Guid> WithDescendants(Guid id)
    {
        var scope = new HashSet<Guid> { id };
        var pending = new Queue<Guid>(scope);
        while (pending.TryDequeue(out var parentId))
        {
            foreach (var childId in _children[parentId])
            {
                if (scope.Add(childId))
                    pending.Enqueue(childId);
            }
        }
        return scope;
    }
}
