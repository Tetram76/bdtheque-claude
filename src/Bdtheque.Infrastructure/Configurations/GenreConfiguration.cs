using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(g => g.Id);

        // A genre label is unique regardless of case and accents (modele-metier.md § Genre): the unique index
        // below compares through this column's case- and accent-insensitive collation.
        builder.Property(g => g.Label).IsRequired().HasMaxLength(200)
            .UseCollation(BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation);

        builder.HasIndex(g => g.Label).IsUnique();

        // Database-level defence in depth mirroring the domain invariant (see AuthorConfiguration):
        // IsRequired() only enforces NOT NULL, so a raw-SQL write could still persist ''.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Genres_LabelNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(Genre.Label)}\")), 0) > 0"));
    }
}
