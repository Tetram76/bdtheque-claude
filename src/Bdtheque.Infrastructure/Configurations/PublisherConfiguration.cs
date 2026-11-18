using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.HasKey(p => p.Id);

        // A publisher name is unique regardless of case and accents (modele-metier/editeur.md), as a genre label.
        builder.Property(p => p.Name).IsRequired().HasMaxLength(300)
            .UseCollation(BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation);
        builder.Property(p => p.Website).HasMaxLength(2048);

        builder.HasIndex(p => p.Name).IsUnique();

        // The navigation by initial filters on the entry, then orders by name (visuel/listes/liste-par-liste.md).
        builder.Property(p => p.NavigationEntry).IsRequired().HasMaxLength(NavigationEntryColumn.MaxLength);
        builder.HasIndex(p => new { p.NavigationEntry, p.Name });
        builder.ToTable(t => t.HasCheckConstraint("CK_Publishers_NavigationEntryValid", NavigationEntryColumn.ValidValuesSql));

        // Database-level defence in depth mirroring the domain invariant (see AuthorConfiguration):
        // IsRequired() only enforces NOT NULL, so a raw-SQL write could still persist ''.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Publishers_NameNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Publisher.Name)}\")), 0) > 0"));

        builder.HasMany(p => p.Collections)
            .WithOne(c => c.Publisher)
            .HasForeignKey(c => c.PublisherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
