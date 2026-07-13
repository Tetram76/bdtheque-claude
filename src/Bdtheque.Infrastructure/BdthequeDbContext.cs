using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

/// <summary>
/// EF Core entry point to PostgreSQL. Intentionally has no <see cref="DbSet{TEntity}"/>
/// at this stage: the domain model (entities, configurations, migrations) is a separate
/// functional workstream from setting up the architecture.
/// </summary>
public sealed class BdthequeDbContext(DbContextOptions<BdthequeDbContext> options) : DbContext(options)
{
}
