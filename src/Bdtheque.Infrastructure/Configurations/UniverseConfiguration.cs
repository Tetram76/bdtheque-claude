using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class UniverseConfiguration : IEntityTypeConfiguration<Universe>
{
    public void Configure(EntityTypeBuilder<Universe> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(300);
        builder.Property(u => u.Description);

        // Database-level defence in depth mirroring the domain invariant (see AuthorConfiguration):
        // IsRequired() only enforces NOT NULL, so a raw-SQL write could still persist ''.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Universes_NameNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Universe.Name)}\")), 0) > 0"));

        // Restrict prevents silently deleting a parent while it still has children.
        // The domain's SetParent already prevents in-memory cycles; this FK enforces
        // structural integrity at the database level regardless of how data is written.
        builder.HasOne(u => u.Parent)
            .WithMany(u => u.Children)
            .HasForeignKey(u => u.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Defence-in-depth for raw-SQL writes: a universe cannot be its own parent.
        // The domain's SetParent rejects self-reference, but this constraint enforces
        // the same rule at the database level.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Universes_NoSelfParent",
            $"\"{nameof(Universe.ParentId)}\" IS NULL OR \"{nameof(Universe.ParentId)}\" <> \"{nameof(Universe.Id)}\""));
    }
}
