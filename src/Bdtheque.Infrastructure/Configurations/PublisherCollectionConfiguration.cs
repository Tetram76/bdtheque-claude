using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PublisherCollectionConfiguration : IEntityTypeConfiguration<PublisherCollection>
{
    public void Configure(EntityTypeBuilder<PublisherCollection> builder)
    {
        builder.HasKey(c => c.Id);

        // A collection name is unique within its publisher regardless of case and accents (modele-metier/collection-editeur.md):
        // the constraint below compares through this collation, which only a column, not a constraint, can carry.
        builder.Property(c => c.Name).IsRequired().HasMaxLength(300)
            .UseCollation(BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation);

        // Checked at commit: one save of the publisher can swap the names of two of its collections (#360).
        builder.HasIndex(c => new { c.PublisherId, c.Name }).IsUniqueAtCommit();

        // Database-level defence in depth mirroring the domain invariant (see AuthorConfiguration):
        // IsRequired() only enforces NOT NULL, so a raw-SQL write could still persist ''.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PublisherCollections_NameNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(PublisherCollection.Name)}\")), 0) > 0"));

        // FK is already configured from the Publisher side; IsRequired is declared here
        // for clarity and to be explicit that PublisherId is non-nullable.
        builder.Property(c => c.PublisherId).IsRequired();
    }
}
