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
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Edition> Editions => Set<Edition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BdthequeDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Project-wide convention: persist every enum as its underlying int. Every enum member
        // across the domain carries an explicit numeric value (never left implicit), so
        // reordering or inserting members later cannot silently change the meaning of
        // already-persisted rows — the same guarantee a string conversion would give, without
        // coupling persisted data (and the occasional CHECK constraint referencing a member by
        // name, e.g. AlbumConfiguration) to a C# identifier that renaming would silently break
        // (see choix-implementation.md).
        configurationBuilder.Properties<Enum>().HaveConversion<int>();
    }
}
