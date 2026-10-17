using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

/// <summary>
/// The records a saved search designates, each a reference whose deletion the database refuses (fonctionnel.md §
/// Suppression des entités): a foreign key <c>ON DELETE RESTRICT</c> towards the table of its entity, on a column
/// generated from the designation, that only the designations of this entity fill.
/// </summary>
/// <remarks>
/// The domain keeps a designation as an entity and an identifier, whatever the entity; the generated columns turn
/// them into as many foreign keys as there are entities a saved search can designate. Designating a record deleted
/// in the meantime then fails as any reference to it would (<c>23503</c>), and deleting a designated record fails
/// as any reference blocks it (<c>23001</c>), whichever way it is deleted: by cascade, or removed from the form of
/// its owner (a contribution).
/// </remarks>
internal sealed class SavedSearchDesignationConfiguration : IEntityTypeConfiguration<SavedSearchDesignation>
{
    // Foreign key of a designation to its saved search, which it is part of.
    private const string SavedSearchIdProperty = "SavedSearchId";

    public void Configure(EntityTypeBuilder<SavedSearchDesignation> builder)
    {
        builder.ToTable(BdthequeDbContext.SavedSearchDesignationsTable);
        builder.HasKey(SavedSearchIdProperty, nameof(SavedSearchDesignation.Kind), nameof(SavedSearchDesignation.RecordId));
        builder.HasOne<SavedSearch>()
            .WithMany(s => s.Designations)
            .HasForeignKey(SavedSearchIdProperty)
            .OnDelete(DeleteBehavior.Cascade);

        // The deletion of a record looks up the searches designating it.
        builder.HasIndex(d => new { d.Kind, d.RecordId });

        foreach (var (kind, record) in DesignatedEntities)
        {
            var column = ColumnOf(kind);
            // Stored: PostgreSQL has no foreign key on a virtual generated column.
            builder.Property<Guid?>(column)
                .HasComputedColumnSql(
                    $"CASE WHEN \"{nameof(SavedSearchDesignation.Kind)}\" = {(int)kind} THEN \"{nameof(SavedSearchDesignation.RecordId)}\" END",
                    stored: true);
            builder.HasOne(record)
                .WithMany()
                .HasForeignKey(column)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // A designation always has the foreign key of its entity: never one of an entity without (an intent).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SavedSearchDesignations_DesignatesAReferenceableRecord",
            $"num_nonnulls({string.Join(", ", DesignatedEntities.Keys.Select(k => $"\"{ColumnOf(k)}\""))}) = 1"));
    }

    // Every entity a saved search can designate (fonctionnel.md § Recherches enregistrées: never an intent).
    private static readonly Dictionary<EntityKind, Type> DesignatedEntities = new()
    {
        [EntityKind.Album] = typeof(Album),
        [EntityKind.Series] = typeof(Series),
        [EntityKind.Universe] = typeof(Universe),
        [EntityKind.Edition] = typeof(Edition),
        [EntityKind.EditionVisual] = typeof(EditionVisual),
        [EntityKind.PublisherCollection] = typeof(PublisherCollection),
        [EntityKind.Contribution] = typeof(Contribution),
        [EntityKind.Author] = typeof(Author),
        [EntityKind.Publisher] = typeof(Publisher),
        [EntityKind.Genre] = typeof(Genre),
    };

    /// <summary>The generated column holding the identifier of the designated records of <paramref name="kind"/>.</summary>
    private static string ColumnOf(EntityKind kind) => $"{kind}Id";
}
