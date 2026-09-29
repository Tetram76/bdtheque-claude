using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PurchaseIntentConfiguration : IEntityTypeConfiguration<PurchaseIntent>
{
    public void Configure(EntityTypeBuilder<PurchaseIntent> builder)
    {
        builder.HasKey(p => p.Id);

        // Database-level defence in depth mirroring the domain invariant (see PurchaseIntent
        // constructor): a raw-SQL write could otherwise leave both or neither target set.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PurchaseIntents_ExactlyOneOfAlbumOrEdition",
            $"(\"{nameof(PurchaseIntent.AlbumId)}\" IS NOT NULL) <> (\"{nameof(PurchaseIntent.EditionId)}\" IS NOT NULL)"));

        builder.HasOne(p => p.Album)
            .WithMany()
            .HasForeignKey(p => p.AlbumId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Edition)
            .WithMany()
            .HasForeignKey(p => p.EditionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // A given album or edition is targeted by at most one intent (see modele-metier.md §
        // Intention d'achat). Filtered to non-null rows, mirroring ContributionConfiguration, so
        // each index only covers the intents of its own kind.
        builder.HasIndex(p => p.AlbumId)
            .IsUnique()
            .HasFilter($"\"{nameof(PurchaseIntent.AlbumId)}\" IS NOT NULL");

        builder.HasIndex(p => p.EditionId)
            .IsUnique()
            .HasFilter($"\"{nameof(PurchaseIntent.EditionId)}\" IS NOT NULL");
    }
}
