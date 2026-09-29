namespace Bdtheque.Domain.Entities.Common;

public abstract class EntityBase
{
    // Guid v7 is time-ordered, which avoids index fragmentation on PostgreSQL
    // while remaining globally unique. Assigned here, never by EF Core (see
    // BdthequeDbContext.OnModelCreating).
    public Guid Id { get; private set; } = Guid.CreateVersion7();
}
