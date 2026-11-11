using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PendingVisualConfiguration : IEntityTypeConfiguration<PendingVisual>
{
    public void Configure(EntityTypeBuilder<PendingVisual> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Ignore(v => v.ExpiresAt);

        builder.Property(v => v.MediaReference).IsRequired().HasMaxLength(2048);

        // The colours of the visual it becomes, stored as those of a visual (EditionVisualConfiguration).
        builder.ComplexProperty(v => v.Palette, palette =>
        {
            palette.Property(p => p.Dominant).HasConversion(c => c.Value, value => RgbColor.FromValue(value));
            palette.Property(p => p.Deep).HasConversion(c => c.Value, value => RgbColor.FromValue(value));
            palette.Property(p => p.Accent).HasConversion(c => c.Value, value => RgbColor.FromValue(value));
        });
        foreach (var color in (string[])[nameof(VisualPalette.Dominant), nameof(VisualPalette.Deep), nameof(VisualPalette.Accent)])
        {
            var column = $"{nameof(PendingVisual.Palette)}_{color}";
            builder.ToTable(t => t.HasCheckConstraint(
                $"CK_PendingVisuals_{column}IsRgb", $"\"{column}\" BETWEEN 0 AND {0xFFFFFF}"));
        }

        // Mirrors the domain invariant, as for a visual.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PendingVisuals_MediaReferenceNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(PendingVisual.MediaReference)}\")), 0) > 0"));

        // The reconciliation removes the pending visuals no longer kept, by the date of their upload.
        builder.HasIndex(v => v.CreatedAt);
    }
}
