using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PublisherCollectionConfiguration : IEntityTypeConfiguration<PublisherCollection>
{
    public void Configure(EntityTypeBuilder<PublisherCollection> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(300);

        builder.HasIndex(c => new { c.PublisherId, c.Name }).IsUnique();

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
