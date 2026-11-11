using Bdtheque.Api.Endpoints;
using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Visuals;

/// <summary>
/// The visuals of an edition saved with it (choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente): in
/// the transaction of its write, its pending visuals become its own, the order and the types are applied, and the visuals left
/// out are removed, their files once the transaction is committed, as any deletion.
/// </summary>
internal static class EditionVisualEntries
{
    /// <summary>
    /// Sets the visuals of <paramref name="edition"/>, whose visuals are loaded, as <paramref name="entries"/> give them; none
    /// leaves them as they are.
    /// </summary>
    /// <returns>The media of the visuals removed, to delete once the write is committed.</returns>
    /// <exception cref="DomainRuleViolationException">A pending visual no longer kept, or gone.</exception>
    public static async Task<IReadOnlyList<string>> ApplyAsync(
        BdthequeDbContext context, Edition edition, IReadOnlyList<EditionVisualEntry>? entries, TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (entries is null)
            return [];

        var pendingIds = entries.Where(e => e.PendingId is not null).Select(e => e.PendingId!.Value).ToList();
        var pending = await context.PendingVisuals.Where(v => pendingIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, cancellationToken);
        var now = clock.GetUtcNow();
        if (pendingIds.Any(id => !pending.TryGetValue(id, out var visual) || visual.IsExpiredAt(now)))
            throw new DomainRuleViolationException(
                DomainRules.EditionVisualPendingExpired, "A visual waiting for the saving of its edition is no longer kept.");

        var before = edition.Visuals.ToHashSet();
        var removed = edition.SetVisuals([.. entries.Select(e => (
            e.Id,
            e.PendingId is { } id ? pending[id] : null,
            EnumMapping.Map<DomainEnums.VisualType>(e.Type)!.Value))]);
        context.EditionVisuals.AddRange(edition.Visuals.Where(v => !before.Contains(v)));
        context.EditionVisuals.RemoveRange(removed);
        context.PendingVisuals.RemoveRange(pending.Values);
        return [.. removed.Select(v => v.MediaReference)];
    }

    /// <summary>The visuals of the editions of an album, which only a write that sets them needs.</summary>
    public static IQueryable<Album> WithVisuals(IQueryable<Album> query) => query.Include(a => a.Editions).ThenInclude(e => e.Visuals);
}
