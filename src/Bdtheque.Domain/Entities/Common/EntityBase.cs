namespace Bdtheque.Domain.Entities.Common;

public abstract class EntityBase
{
    // Guid v7 is time-ordered, which avoids index fragmentation on PostgreSQL
    // while remaining globally unique. Assigned here, never by EF Core (see
    // BdthequeDbContext.OnModelCreating).
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    // Set automatically when the record is saved, never entered (modele-metier.md § Attributs
    // communs à toutes les entités): no domain operation writes them, the persistence layer does.
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ModifiedAt { get; private set; }
}
