using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class EditionVisualConfiguration : IEntityTypeConfiguration<EditionVisual>
{
    public void Configure(EntityTypeBuilder<EditionVisual> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.MediaReference).IsRequired().HasMaxLength(2048);

        // Sorted retrieval per fonctionnel.md § Ordre des visuels d'une édition: by edition,
        // then by type (its int value already reflects the fixed display order — see
        // VisualType), then by the user-adjustable display order within that type.
        builder.HasIndex(v => new { v.EditionId, v.Type, v.DisplayOrder });

        // Database-level defence in depth mirroring the domain invariant (see AuthorConfiguration):
        // IsRequired() only enforces NOT NULL, so a raw-SQL write could still persist ''.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_EditionVisuals_MediaReferenceNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(EditionVisual.MediaReference)}\")), 0) > 0"));

        // Mirrors EditionVisual.SetDisplayOrder.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_EditionVisuals_DisplayOrderNotNegative",
            $"\"{nameof(EditionVisual.DisplayOrder)}\" >= 0"));

        builder.HasOne(v => v.Edition)
            .WithMany(e => e.Visuals)
            .HasForeignKey(v => v.EditionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
