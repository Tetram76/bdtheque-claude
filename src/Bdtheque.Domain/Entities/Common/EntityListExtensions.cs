namespace Bdtheque.Domain.Entities.Common;

/// <summary>Identity-based list operations for many-to-many associations between entities.</summary>
internal static class EntityListExtensions
{
    // Compared by Id rather than by reference: a detached or differently-instanced entity can
    // stand for the same row, and a duplicate would violate the join table's primary key.
    public static void AddOnce<TEntity>(this List<TEntity> entities, TEntity entity) where TEntity : EntityBase
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entities.TrueForAll(e => e.Id != entity.Id))
            entities.Add(entity);
    }

    public static void RemoveById<TEntity>(this List<TEntity> entities, TEntity entity) where TEntity : EntityBase
    {
        ArgumentNullException.ThrowIfNull(entity);
        entities.RemoveAll(e => e.Id == entity.Id);
    }
}
