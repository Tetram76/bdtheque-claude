using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class EditionConfiguration : IEntityTypeConfiguration<Edition>
{
    public void Configure(EntityTypeBuilder<Edition> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Isbn).HasMaxLength(20);
        builder.Property(e => e.PersonalReference).HasMaxLength(200);
        builder.Property(e => e.PersonalNotes);
        builder.Property(e => e.AcquisitionCurrency).HasMaxLength(3);
        builder.Property(e => e.AcquisitionAmount).HasPrecision(12, 2);

        // Mirrors Edition.SetPublicationYear / SetPageCount.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_PublicationYearPositive",
            $"\"{nameof(Edition.PublicationYear)}\" IS NULL OR \"{nameof(Edition.PublicationYear)}\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_PageCountPositive",
            $"\"{nameof(Edition.PageCount)}\" IS NULL OR \"{nameof(Edition.PageCount)}\" > 0"));

        // Mirrors Edition.SetAcquisitionPrice: amount and currency are present or absent together.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_AcquisitionAmountCurrencyTogether",
            $"(\"{nameof(Edition.AcquisitionAmount)}\" IS NULL) = (\"{nameof(Edition.AcquisitionCurrency)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_AcquisitionAmountPositive",
            $"\"{nameof(Edition.AcquisitionAmount)}\" IS NULL OR \"{nameof(Edition.AcquisitionAmount)}\" > 0"));

        // Mirrors the model constraint (modele-metier.md § Édition): an edition not possessed
        // (no acquisition mode) cannot carry an acquisition date or price.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_AcquisitionModeRequiredForDateOrPrice",
            $"\"{nameof(Edition.AcquisitionMode)}\" IS NOT NULL OR " +
            $"(\"{nameof(Edition.AcquisitionDate)}\" IS NULL AND \"{nameof(Edition.AcquisitionAmount)}\" IS NULL)"));

        // Mirrors the model constraint: a free edition cannot carry an acquisition price.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_FreeRequiresNoAmount",
            $"\"{nameof(Edition.IsFree)}\" = false OR \"{nameof(Edition.AcquisitionAmount)}\" IS NULL"));

        builder.HasOne(e => e.Album)
            .WithMany()
            .HasForeignKey(e => e.AlbumId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Publisher)
            .WithMany()
            .HasForeignKey(e => e.PublisherId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Cross-table half of the constraint (the collection must belong to this publisher) is
        // intentionally left to the domain layer only, as with Series' own template collection —
        // see SeriesConfiguration's remarks: a composite FK would add real schema complexity for
        // a raw-SQL bypass scenario that isn't plausible with a single admin user.
        builder.HasOne(e => e.PublisherCollection)
            .WithMany()
            .HasForeignKey(e => e.PublisherCollectionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
