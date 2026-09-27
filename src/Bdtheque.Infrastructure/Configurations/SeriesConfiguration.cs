using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength(500);
        builder.Property(s => s.SortKey).IsRequired().HasMaxLength(500);
        builder.Property(s => s.Summary);
        builder.Property(s => s.PersonalNotes);

        builder.HasIndex(s => s.SortKey);

        // Database-level defence in depth mirroring the domain invariant (see AuthorConfiguration):
        // IsRequired() only enforces NOT NULL, so a raw-SQL write could still persist ''.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Series_TitleNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Series.Title)}\")), 0) > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Series_SortKeyNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Series.SortKey)}\")), 0) > 0"));

        // Mirrors the domain guard in Series.SetTheoreticalVolumeCount.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Series_TheoreticalVolumeCountPositive",
            $"\"{nameof(Series.TheoreticalVolumeCount)}\" IS NULL OR \"{nameof(Series.TheoreticalVolumeCount)}\" > 0"));

        // Mirrors the single-table half of the domain guard in Series.SetTemplate. The
        // cross-table half (the collection must belong to this publisher) is intentionally
        // left to the domain layer only — see the SeriesConfiguration remarks in the PR
        // description / CheckConstraintTests for the rationale.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Series_TemplateCollectionRequiresPublisher",
            $"\"{nameof(Series.TemplatePublisherCollectionId)}\" IS NULL OR \"{nameof(Series.TemplatePublisherId)}\" IS NOT NULL"));

        builder.HasOne(s => s.TemplatePublisher)
            .WithMany()
            .HasForeignKey(s => s.TemplatePublisherId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.TemplatePublisherCollection)
            .WithMany()
            .HasForeignKey(s => s.TemplatePublisherCollectionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Implicit many-to-many (no extra attribute on the link): explicit join table names
        // keep them predictable once Album grows the same relations (Phase 1, PR #3).
        builder.HasMany(s => s.Genres)
            .WithMany()
            .UsingEntity(j => j.ToTable("SeriesGenres"));

        builder.HasMany(s => s.Universes)
            .WithMany()
            .UsingEntity(j => j.ToTable("SeriesUniverses"));
    }
}
