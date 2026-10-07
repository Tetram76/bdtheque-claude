using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
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
    public DbSet<EditionVisual> EditionVisuals => Set<EditionVisual>();
    public DbSet<PurchaseIntent> PurchaseIntents => Set<PurchaseIntent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Same French order as FrenchCollation, but ignoring case and accents when comparing
        // (ks-level1: base letters only), for the columns whose uniqueness ignores them.
        // Nondeterministic, so equality — hence unique indexes — follows it too.
        modelBuilder.HasCollation(
            CaseAndAccentInsensitiveFrenchCollation, locale: "fr-FR-u-ks-level1", provider: "icu", deterministic: false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BdthequeDbContext).Assembly);

        // EntityBase assigns every Id in the domain, while EF Core's convention treats Guid keys
        // as generated on add. With a generated key already set, EF assumes the row exists: a new
        // child reached through a loaded parent's navigation (e.g. Album.AddPurchaseIntent) would
        // be sent as an UPDATE of 0 rows instead of an INSERT.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(EntityBase).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(EntityBase.Id))
                .ValueGeneratedNever();
        }

        // Optimistic concurrency on whole aggregates (choix-implementation.md § Concurrence
        // d'accès): the root's xmin, a native PostgreSQL row version needing no column of its own,
        // stands for the aggregate. A shadow property keeps this persistence concern out of the
        // domain.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IAggregateRoot).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property<uint>(VersionProperty)
                .IsRowVersion();
        }
    }

    /// <summary>
    /// Shadow property holding the version of an aggregate, on its root only (mapped to
    /// <c>xmin</c>). Read it with <c>EF.Property&lt;uint&gt;(root, VersionProperty)</c>.
    /// </summary>
    public const string VersionProperty = "Version";

    /// <summary>
    /// Current version of a tracked aggregate root — after <c>SaveChanges</c>, the one its write
    /// produced, read back from the database.
    /// </summary>
    public uint VersionOf<TRoot>(TRoot root) where TRoot : EntityBase, IAggregateRoot =>
        Entry(root).Property<uint>(VersionProperty).CurrentValue;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareSave()
    {
        ChangeTracker.DetectChanges();
        DeleteSeveredContributions();
        StampAuditDates();
    }

    // A contribution removed by its owner (Album.SetTitleSeriesAndContributions,
    // Series.SetTemplateContributions) is part of it, and goes with its removal. Its relationship to
    // either owner being optional — it has two, exclusive —, EF Core nulls its foreign key instead of
    // deleting the orphan, which CK_Contributions_ExactlyOneOfAlbumOrSeries refuses: deleted here, the
    // single place every save goes through, rather than by each caller of the domain.
    private void DeleteSeveredContributions()
    {
        foreach (var entry in ChangeTracker.Entries<Contribution>()
                     .Where(e => e.State == EntityState.Modified && e.Entity.AlbumId is null && e.Entity.SeriesId is null)
                     .ToList())
            entry.State = EntityState.Deleted;
    }

    // Creation and last modification dates are set here, never entered (modele-metier.md §
    // Attributs communs à toutes les entités): the single place every save goes through. A write on
    // an aggregate marks its root modified (AggregateWriteExtensions), so the root's modification
    // date follows any change to its children or associations.
    private void StampAuditDates()
    {
        var now = DateTimeOffset.UtcNow;
        // Truncated to PostgreSQL's microsecond precision, so the tracked value is the stored one.
        now = now.AddTicks(-(now.Ticks % (TimeSpan.TicksPerMillisecond / 1000)));

        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.ModifiedAt).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.ModifiedAt).CurrentValue = now;
                    // Restores whatever value was loaded, and keeps it out of the UPDATE.
                    entry.Property(e => e.CreatedAt).CurrentValue = entry.Property(e => e.CreatedAt).OriginalValue;
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;
            }
        }
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

        // Every text column sorts in French linguistic order (fonctionnel.md § Langue et culture
        // d'affichage): the database default collation of the postgres image compares bytes,
        // which puts lowercase and accented initials after "Z". Declared on the columns rather
        // than relied upon from the database default, so the order travels with the schema
        // whatever locale the database was initialized with (see choix-implementation.md).
        configurationBuilder.Properties<string>().UseCollation(FrenchCollation);
    }

    /// <summary>ICU collation for French, predefined by PostgreSQL when built with ICU.</summary>
    public const string FrenchCollation = "fr-FR-x-icu";

    /// <summary>
    /// Case- and accent-insensitive variant of <see cref="FrenchCollation"/>, created by
    /// the migrations (see <see cref="OnModelCreating"/>).
    /// </summary>
    public const string CaseAndAccentInsensitiveFrenchCollation = "fr_case_accent_insensitive";
}
