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
        // COALESCE(LENGTH(TRIM(...)), 0) > 0 rejects null, empty, and whitespace-only values,
        // covering raw-SQL bypasses that might insert blank strings instead of NULL.
        // The domain's NullIfEmpty already normalises blanks to null, so this check is
        // strictly defence-in-depth for out-of-band writes.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Authors_LastNameOrPseudonym",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Author.LastName)}\")), 0) > 0 OR COALESCE(LENGTH(TRIM(\"{nameof(Author.Pseudonym)}\")), 0) > 0"));
    }
}
