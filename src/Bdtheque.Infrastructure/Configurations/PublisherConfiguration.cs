using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Website).HasMaxLength(2048);

        builder.HasIndex(p => p.Name).IsUnique();

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
