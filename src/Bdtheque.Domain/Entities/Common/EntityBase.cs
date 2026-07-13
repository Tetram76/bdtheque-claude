namespace Bdtheque.Domain.Entities.Common;

public abstract class EntityBase
{
    // Guid v7 is time-ordered, which avoids index fragmentation on PostgreSQL
    // while remaining globally unique. EF Core sets this via value generation.
    public Guid Id { get; private set; } = Guid.CreateVersion7();
}
