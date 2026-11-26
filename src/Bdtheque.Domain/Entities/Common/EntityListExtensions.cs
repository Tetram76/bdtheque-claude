namespace Bdtheque.Domain.Entities.Common;

/// <summary>Identity-based list operations for many-to-many associations between entities.</summary>
internal static class EntityListExtensions
{
    // Compared by Id rather than by reference: a detached or differently-instanced entity can
    // stand for the same row, and a duplicate would violate the join table's primary key.
    public static void AddOnce<TEntity>(this List<TEntity> entities, TEntity entity) where TEntity : EntityBase
    {
        // Stryker disable once Statement : a guard against a programming error, null being excluded by the nullable annotations, with which no caller passes one
        ArgumentNullException.ThrowIfNull(entity);
        if (entities.TrueForAll(e => e.Id != entity.Id))
            entities.Add(entity);
    }

    public static void RemoveById<TEntity>(this List<TEntity> entities, TEntity entity) where TEntity : EntityBase
    {
        // Stryker disable once Statement : a guard against a programming error, null being excluded by the nullable annotations, with which no caller passes one
        ArgumentNullException.ThrowIfNull(entity);
        entities.RemoveAll(e => e.Id == entity.Id);
    }

    /// <summary>
    /// Associates <paramref name="kept"/> instead of <paramref name="merged"/>, when the list holds the latter, as a merge does
    /// (fonctionnel/regles-metier/fusion-des-fiches.md): a list holding both holds <paramref name="kept"/> once.
    /// </summary>
    /// <exception cref="ArgumentException">The same entity twice.</exception>
    public static void Transfer<TEntity>(this List<TEntity> entities, TEntity merged, TEntity kept) where TEntity : EntityBase
    {
        Domain.Common.MergeGuard.EnsureDistinct(merged, kept);
        if (entities.RemoveAll(e => e.Id == merged.Id) > 0)
            entities.AddOnce(kept);
    }
}
