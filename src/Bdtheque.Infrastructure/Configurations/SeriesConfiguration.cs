using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    private const int TitleMaxLength = 500;

    // TitleSortKeyCalculator moves the leading article to a bracketed suffix instead of
    // dropping it, which grows the computed key relative to the title (worst case: +3
    // characters for the elided "L'" form: "L'X" becomes "X [L']" — see its BuildSortKey). SortKey must stay large
    // enough to hold a max-length title's computed key, or SaveChanges would fail for a title
    // that legitimately fits the Title column. Guarded by SortKeyMaxLength_AccommodatesWorstCaseArticleSuffixGrowth.
    private const int SortKeyMaxLength = TitleMaxLength + 10;

    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength(TitleMaxLength);
        builder.Property(s => s.SortKey).IsRequired().HasMaxLength(SortKeyMaxLength);
        builder.Property(s => s.NavigationEntry).IsRequired().HasMaxLength(NavigationEntryColumn.MaxLength);
        builder.Property(s => s.Summary);
        builder.Property(s => s.PersonalNotes);

        // Full list ordered by sort key; navigation filters on the entry, then orders by sort key.
        builder.HasIndex(s => s.SortKey);
        builder.HasIndex(s => new { s.NavigationEntry, s.SortKey });
        builder.ToTable(t => t.HasCheckConstraint("CK_Series_NavigationEntryValid", NavigationEntryColumn.ValidValuesSql));

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
        // left to the domain layer only (see choix-implementation.md § Cohérence entre tables).
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
        // keep them predictable alongside Album's own Genre/Universe relations (AlbumConfiguration).
        builder.HasMany(s => s.Genres)
            .WithMany()
            .UsingEntity(j => j.ToTable("SeriesGenres"));

        builder.HasMany(s => s.Universes)
            .WithMany()
            .UsingEntity(j => j.ToTable("SeriesUniverses"));
    }
}
