using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.LastName).HasMaxLength(200);
        builder.Property(a => a.FirstName).HasMaxLength(200);
        builder.Property(a => a.Pseudonym).HasMaxLength(200);
        builder.Property(a => a.Nationality).HasMaxLength(100);
        builder.Property(a => a.Biography);

        // Database-level defence in depth mirroring the domain invariant.
        // The domain constructor already enforces this, so this constraint will
        // only trigger if data bypasses the domain model (e.g. raw SQL).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Authors_LastNameOrPseudonym",
            $"\"{nameof(Author.LastName)}\" IS NOT NULL OR \"{nameof(Author.Pseudonym)}\" IS NOT NULL"));
    }
}
