using Bdtheque.Api.Endpoints;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Api.Merge;

/// <summary>
/// The homonymous sub-universes the merge of two universes merges with them (fonctionnel/regles-metier/fusion-des-fiches.md):
/// under the two universes, then under each pair, at every level, two sub-universes whose names the merge would make collide
/// under one parent.
/// </summary>
internal static class UniverseMergePairs
{
    /// <summary>A pair: its universe under the first merged one, its universe under the second, where it stands.</summary>
    /// <param name="Path">The names that lead to it under the universe that remains, its own last, those of the first side.</param>
    /// <param name="Under">The first universe of the pair it stands under, none under the merged universes themselves.</param>
    public sealed record Pair(Guid First, Guid Second, IReadOnlyList<string> Path, Guid? Under);

    /// <summary>
    /// Every two sub-universes whose names are the same as their column compares them, case and accents aside
    /// (choix-implementation/persistance/collation-des-colonnes-texte.md): the database alone compares them as the uniqueness of
    /// a name under its parent does.
    /// </summary>
    public static async Task<IReadOnlySet<(Guid, Guid)>> HomonymsAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        (await context.Universes.AsNoTracking()
            .Where(u => u.ParentId != null)
            .SelectMany(u => context.Universes.Where(v => v.ParentId != null && v.Id != u.Id && v.Name == u.Name), (u, v) => new { u.Id, Other = v.Id })
            .ToListAsync(cancellationToken))
        .Select(h => (h.Id, h.Other))
        .ToHashSet();

    /// <summary>The pairs of the merge of <paramref name="first"/> and <paramref name="second"/>, a pair after the one it stands under.</summary>
    public static IReadOnlyList<Pair> Of(UniverseTree tree, IReadOnlySet<(Guid, Guid)> homonyms, Guid first, Guid second)
    {
        var paired = new HashSet<Guid> { first, second };
        var pairs = new List<Pair>();
        var pending = new Queue<(Guid First, Guid Second, IReadOnlyList<string> Path, Guid? Under)>([(first, second, [], null)]);
        while (pending.TryDequeue(out var level))
        {
            // The merged universes themselves are no sub-universe of the other (one universe merged with one of its own).
            var others = tree.SubUniverses(level.Second).Where(u => !paired.Contains(u.Id)).ToList();
            foreach (var sub in tree.SubUniverses(level.First).Where(u => !paired.Contains(u.Id)).ToList())
            {
                if (others.FirstOrDefault(o => !paired.Contains(o.Id) && homonyms.Contains((sub.Id, o.Id))) is not { } other)
                    continue;
                paired.Add(sub.Id);
                paired.Add(other.Id);
                var pair = new Pair(sub.Id, other.Id, [.. level.Path, sub.Name], level.Under);
                pairs.Add(pair);
                pending.Enqueue((sub.Id, other.Id, pair.Path, sub.Id));
            }
        }

        return pairs;
    }
}
