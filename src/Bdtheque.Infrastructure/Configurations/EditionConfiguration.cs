using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
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
        // Scale 4 (not 2) so that ISO 4217 currencies with three minor-unit digits (e.g. KWD,
        // BHD, OMR, JOD, TND) aren't silently rounded on persistence — fonctionnel.md §
        // Gestion des devises requires supporting any currency, not just 2-decimal ones.
        builder.Property(e => e.AcquisitionAmount).HasPrecision(14, 4);
        builder.Property(e => e.InitialValueCurrency).HasMaxLength(3);
        builder.Property(e => e.InitialValueAmount).HasPrecision(14, 4);

        // Mirrors Edition.SetPublicationYear / SetPageCount.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_PublicationYearPositive",
            $"\"{nameof(Edition.PublicationYear)}\" IS NULL OR \"{nameof(Edition.PublicationYear)}\" > 0"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_PageCountPositive",
            $"\"{nameof(Edition.PageCount)}\" IS NULL OR \"{nameof(Edition.PageCount)}\" > 0"));

        // Mirrors Edition.EnsureValid: amount and currency are present or absent together.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_AcquisitionAmountCurrencyTogether",
            $"(\"{nameof(Edition.AcquisitionAmount)}\" IS NULL) = (\"{nameof(Edition.AcquisitionCurrency)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_AcquisitionAmountPositive",
            $"\"{nameof(Edition.AcquisitionAmount)}\" IS NULL OR \"{nameof(Edition.AcquisitionAmount)}\" > 0"));

        // Same rules for the initial value as for the acquisition price.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_InitialValueAmountCurrencyTogether",
            $"(\"{nameof(Edition.InitialValueAmount)}\" IS NULL) = (\"{nameof(Edition.InitialValueCurrency)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_InitialValueAmountPositive",
            $"\"{nameof(Edition.InitialValueAmount)}\" IS NULL OR \"{nameof(Edition.InitialValueAmount)}\" > 0"));

        // Mirrors the model constraint (modele-metier.md § Édition): an edition not possessed
        // (no acquisition mode) cannot carry an acquisition date, a price or an initial value.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_AcquisitionModeRequiredForDateOrAmounts",
            $"\"{nameof(Edition.AcquisitionMode)}\" IS NOT NULL OR " +
            $"(\"{nameof(Edition.AcquisitionDate)}\" IS NULL AND \"{nameof(Edition.AcquisitionAmount)}\" IS NULL " +
            $"AND \"{nameof(Edition.InitialValueAmount)}\" IS NULL)"));

        // Mirrors the model constraints: a free edition has no value, and a purchase is never free.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_FreeRequiresNoAmount",
            $"\"{nameof(Edition.IsFree)}\" = false OR " +
            $"(\"{nameof(Edition.AcquisitionAmount)}\" IS NULL AND \"{nameof(Edition.InitialValueAmount)}\" IS NULL)"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Editions_PurchaseNotFree",
            $"\"{nameof(Edition.IsFree)}\" = false OR \"{nameof(Edition.AcquisitionMode)}\" IS DISTINCT FROM {(int)AcquisitionMode.Purchase}"));

        // The reference date every amount needs (Edition.EnsureAmountsDated) may be the album's first
        // publication: a cross-table rule, left to the domain only (choix-implementation.md §
        // Cohérence entre tables).

        // An edition is part of its album and goes with it; publisher and collection are mere
        // references, whose deletion is refused while an edition uses them.
        builder.HasOne(e => e.Album)
            .WithMany(a => a.Editions)
            .HasForeignKey(e => e.AlbumId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Publisher)
            .WithMany()
            .HasForeignKey(e => e.PublisherId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Cross-table half of the constraint (the collection must belong to this publisher) is
        // intentionally left to the domain layer only, as with Series' own template collection
        // (see choix-implementation.md § Cohérence entre tables).
        builder.HasOne(e => e.PublisherCollection)
            .WithMany()
            .HasForeignKey(e => e.PublisherCollectionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
