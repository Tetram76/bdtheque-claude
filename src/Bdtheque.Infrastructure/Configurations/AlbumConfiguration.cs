using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    private const int TitleMaxLength = 500;

    // Mirrors SeriesConfiguration.SortKeyMaxLength: the largest growth the computed sort key
    // can incur relative to the title is the elided "L'" article moved to a bracketed suffix.
    private const int SortKeyMaxLength = TitleMaxLength + 10;

    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).HasMaxLength(TitleMaxLength);
        builder.Property(a => a.SortKey).HasMaxLength(SortKeyMaxLength);
        builder.Property(a => a.Summary);
        builder.Property(a => a.PersonalNotes);

        builder.HasIndex(a => a.SortKey);

        // Mirrors the domain guard in Album.EnsureTitleOrSeries: a title is required unless
        // the album is attached to a series.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_TitleRequiredWithoutSeries",
            $"\"{nameof(Album.SeriesId)}\" IS NOT NULL OR LENGTH(TRIM(COALESCE(\"{nameof(Album.Title)}\", ''))) > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_TitleNotBlank",
            $"\"{nameof(Album.Title)}\" IS NULL OR LENGTH(TRIM(\"{nameof(Album.Title)}\")) > 0"));

        // Mirrors Album.SetTitle: title and sort key are present or absent together — never
        // one without the other (a raw-SQL write or the future Firebird import tool could
        // otherwise leave a titled album with no stored sort key, which ordering/navigation
        // relies on, or a sort key with no title to justify it).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_SortKeyPresenceMatchesTitle",
            $"(\"{nameof(Album.Title)}\" IS NULL) = (\"{nameof(Album.SortKey)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_SortKeyNotBlank",
            $"\"{nameof(Album.SortKey)}\" IS NULL OR LENGTH(TRIM(\"{nameof(Album.SortKey)}\")) > 0"));

        // Mirrors Album.SetSortKey (which requires a title) and SetTitle(null) (which resets
        // this flag): the manual flag is meaningless without a title/sort key to override, and
        // leaving it true on a title-less row would make a later, legitimate SetTitle skip
        // recomputing the sort key (it trusts the flag), producing a titled album with no
        // sort key at the next save.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_ManualSortKeyRequiresTitle",
            $"\"{nameof(Album.IsManualSortKey)}\" = false OR \"{nameof(Album.Title)}\" IS NOT NULL"));

        // Mirrors Album.SetVolumeNumber / SetVolumeRange.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_VolumeNumberPositive",
            $"\"{nameof(Album.VolumeNumber)}\" IS NULL OR \"{nameof(Album.VolumeNumber)}\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_VolumeRangeBothOrNeither",
            $"(\"{nameof(Album.StartVolumeNumber)}\" IS NULL) = (\"{nameof(Album.EndVolumeNumber)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_VolumeRangeStartPositive",
            $"\"{nameof(Album.StartVolumeNumber)}\" IS NULL OR \"{nameof(Album.StartVolumeNumber)}\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_VolumeRangeOrder",
            $"\"{nameof(Album.StartVolumeNumber)}\" IS NULL OR \"{nameof(Album.StartVolumeNumber)}\" <= \"{nameof(Album.EndVolumeNumber)}\""));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_VolumeRangeOmnibusOnly",
            $"\"{nameof(Album.StartVolumeNumber)}\" IS NULL OR \"{nameof(Album.Type)}\" = {(int)AlbumType.Omnibus}"));

        // Mirrors Album.SetFirstPublicationDate.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_PublicationMonthRequiresYear",
            $"\"{nameof(Album.FirstPublicationMonth)}\" IS NULL OR \"{nameof(Album.FirstPublicationYear)}\" IS NOT NULL"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_PublicationMonthRange",
            $"\"{nameof(Album.FirstPublicationMonth)}\" IS NULL OR \"{nameof(Album.FirstPublicationMonth)}\" BETWEEN 1 AND 12"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_PublicationYearPositive",
            $"\"{nameof(Album.FirstPublicationYear)}\" IS NULL OR \"{nameof(Album.FirstPublicationYear)}\" > 0"));

        builder.HasOne(a => a.Series)
            .WithMany()
            .HasForeignKey(a => a.SeriesId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Genres)
            .WithMany()
            .UsingEntity(j => j.ToTable("AlbumGenres"));

        builder.HasMany(a => a.Universes)
            .WithMany()
            .UsingEntity(j => j.ToTable("AlbumUniverses"));
    }
}
