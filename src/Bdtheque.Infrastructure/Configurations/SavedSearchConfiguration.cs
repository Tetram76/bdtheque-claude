using System.Text.Json;
using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
{
    private static readonly JsonSerializerOptions VariablesJson = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<SavedSearch> builder)
    {
        builder.HasKey(s => s.Id);

        // The searches are told apart by their name alone in the consultation (modele-metier.md § Recherche
        // enregistrée): unique regardless of case and accents, as the label of a genre.
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200)
            .UseCollation(BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation);
        builder.HasIndex(s => s.Name).IsUnique();

        // Kept as text, exactly as written, rather than as jsonb, which would reorder the keys and the
        // spacing: the change log tells a modification by comparing the values, and the same criteria
        // written again must compare equal. Never queried in the database.
        builder.Property(s => s.Criteria).IsRequired();

        builder.Property(s => s.Variables)
            .IsRequired()
            .HasConversion(
                variables => JsonSerializer.Serialize(variables.ToArray(), VariablesJson),
                json => new SearchVariables(JsonSerializer.Deserialize<SearchVariable[]>(json, VariablesJson) ?? Array.Empty<SearchVariable>()),
                new ValueComparer<SearchVariables>(
                    (left, right) => Equals(left, right),
                    variables => variables.GetHashCode(),
                    // Immutable: the snapshot can be the value itself.
                    variables => variables));

        // Database-level defence in depth mirroring the domain invariant (see GenreConfiguration).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SavedSearches_NameNotBlank",
            $"COALESCE(LENGTH(TRIM(\"{nameof(SavedSearch.Name)}\")), 0) > 0"));
    }
}
