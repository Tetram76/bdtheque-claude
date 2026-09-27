using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

public sealed class BdthequeDbContext(DbContextOptions<BdthequeDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<PublisherCollection> PublisherCollections => Set<PublisherCollection>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Universe> Universes => Set<Universe>();
    public DbSet<Series> Series => Set<Series>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BdthequeDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Project-wide convention: persist every enum as its member name rather than its
        // numeric ordinal, so that reordering or inserting enum members later cannot silently
        // change the meaning of already-persisted rows (see contraintes-techniques.md).
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(50);
    }
}
