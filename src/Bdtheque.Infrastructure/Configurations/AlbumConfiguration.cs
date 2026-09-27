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

        // Mirrors Album.SetTitle: an absent title implies an absent sort key.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_SortKeyRequiresTitle",
            $"\"{nameof(Album.Title)}\" IS NOT NULL OR \"{nameof(Album.SortKey)}\" IS NULL"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Albums_SortKeyNotBlank",
            $"\"{nameof(Album.SortKey)}\" IS NULL OR LENGTH(TRIM(\"{nameof(Album.SortKey)}\")) > 0"));

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
            $"\"{nameof(Album.StartVolumeNumber)}\" IS NULL OR \"{nameof(Album.Type)}\" = '{nameof(AlbumType.Omnibus)}'"));

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
