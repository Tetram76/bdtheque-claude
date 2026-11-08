using Bdtheque.Domain.Common;
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

        // Each colour in one integer column, 0xRRGGBB: a colour is read as a whole, never searched by component.
        builder.ComplexProperty(v => v.Palette, palette =>
        {
            palette.Property(p => p.Dominant).HasConversion(c => c.Value, value => RgbColor.FromValue(value));
            palette.Property(p => p.Deep).HasConversion(c => c.Value, value => RgbColor.FromValue(value));
            palette.Property(p => p.Accent).HasConversion(c => c.Value, value => RgbColor.FromValue(value));
        });
        foreach (var color in (string[])[nameof(VisualPalette.Dominant), nameof(VisualPalette.Deep), nameof(VisualPalette.Accent)])
        {
            var column = $"{nameof(EditionVisual.Palette)}_{color}";
            builder.ToTable(t => t.HasCheckConstraint(
                $"CK_EditionVisuals_{column}IsRgb", $"\"{column}\" BETWEEN 0 AND {0xFFFFFF}"));
        }

        // Optimizes the (EditionId, Type, DisplayOrder) ordering that Edition.GetOrderedVisuals
        // applies in memory. The index alone does not sort a loaded collection: EF Core never
        // adds an implicit ORDER BY to a collection navigation, so it does not by itself
        // guarantee the fixed presentation order from
        // fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md — see GetOrderedVisuals, the single
        // place that actually applies it.
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
