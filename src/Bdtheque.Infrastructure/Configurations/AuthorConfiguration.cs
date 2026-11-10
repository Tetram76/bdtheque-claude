using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    private const int NameMaxLength = 200;

    // Worst case of Author's sort key: "Last First", both at their maximum length. Guarded by
    // AuthorSortKeyMaxLength_AccommodatesLastAndFirstName.
    private const int SortKeyMaxLength = NameMaxLength * 2 + 1;

    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.LastName).HasMaxLength(NameMaxLength);
        builder.Property(a => a.FirstName).HasMaxLength(NameMaxLength);
        builder.Property(a => a.Pseudonym).HasMaxLength(NameMaxLength);
        builder.Property(a => a.Nationality).HasMaxLength(100);
        builder.Property(a => a.Biography);

        // Two authors never share a display name, regardless of case and accents (modele-metier/auteur-artiste.md): the
        // unique index below compares through this column's case- and accent-insensitive collation, as for a genre label.
        // Its worst case is that of the sort key, "First Last".
        builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(SortKeyMaxLength)
            .UseCollation(BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation);
        builder.HasIndex(a => a.DisplayName).IsUnique();

        builder.Property(a => a.SortKey).IsRequired().HasMaxLength(SortKeyMaxLength);
        builder.Property(a => a.NavigationEntry).IsRequired().HasMaxLength(NavigationEntryColumn.MaxLength);

        // Full list ordered by sort key; navigation filters on the entry, then orders by sort key.
        builder.HasIndex(a => a.SortKey);
        builder.HasIndex(a => new { a.NavigationEntry, a.SortKey });
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Authors_SortKeyNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Author.SortKey)}\")), 0) > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Authors_NavigationEntryValid", NavigationEntryColumn.ValidValuesSql));

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
