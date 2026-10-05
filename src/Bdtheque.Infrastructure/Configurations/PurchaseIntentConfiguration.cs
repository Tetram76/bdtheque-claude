using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PurchaseIntentConfiguration : IEntityTypeConfiguration<PurchaseIntent>
{
    public void Configure(EntityTypeBuilder<PurchaseIntent> builder)
    {
        builder.HasKey(p => p.Id);

        // Cascade: an intent is owned by its album aggregate, which alone creates and removes it
        // (Album.RecordAcquisition removes a realized one) — it has no existence without its album.
        builder.HasOne(p => p.Album)
            .WithMany(a => a.PurchaseIntents)
            .HasForeignKey(p => p.AlbumId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Cascade as well: an unowned edition and its intent are inseparable, so deleting the
        // edition takes its intent with it (fonctionnel.md § Intention d'achat).
        builder.HasOne(p => p.Edition)
            .WithMany()
            .HasForeignKey(p => p.EditionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // Database-level defence in depth for the single-row half of the per-album rules
        // (see Album.AddPurchaseIntent): at most one whole-album intent per album, at most one
        // intent per edition. The cross-row half — a whole-album intent excluding intents on the
        // album's editions — and the edition belonging to AlbumId are enforced by the album
        // aggregate only (see choix-implementation.md).
        // Named so it does not replace the plain AlbumId foreign-key index: being filtered, it
        // cannot serve lookups of the album's edition intents (loading the aggregate, cascade
        // when deleting an album).
        builder.HasIndex(p => p.AlbumId, "IX_PurchaseIntents_AlbumId_WholeAlbum")
            .IsUnique()
            .HasFilter($"\"{nameof(PurchaseIntent.EditionId)}\" IS NULL");

        builder.HasIndex(p => p.AlbumId);

        builder.HasIndex(p => p.EditionId)
            .IsUnique();
    }
}
