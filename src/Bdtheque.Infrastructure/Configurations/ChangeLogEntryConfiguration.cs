using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure.Configurations;

internal sealed class ChangeLogEntryConfiguration : IEntityTypeConfiguration<ChangeLogEntry>
{
    public void Configure(EntityTypeBuilder<ChangeLogEntry> builder)
    {
        builder.ToTable("ChangeLogEntries");
        builder.HasKey(e => e.Id);

        // No foreign key to the record: the entry outlives its deletion (modele-metier.md § Entrée du
        // journal des modifications).
        builder.Property(e => e.RecordId);

        // Data rather than texts, so that they are presented in the display culture in force when they
        // are read (fonctionnel.md § Langue et culture d'affichage): JSON, in the form ChangeLogJson pins.
        builder.Property(e => e.Label)
            .HasColumnType("jsonb")
            .HasConversion(label => ChangeLogJson.SerializeLabel(label), json => ChangeLogJson.DeserializeLabel(json));
        builder.Property(e => e.Changes)
            .HasColumnType("jsonb")
            .HasConversion(
                changes => ChangeLogJson.SerializeChanges(changes),
                json => ChangeLogJson.DeserializeChanges(json),
                // An entry is never modified: compared as a whole, by its JSON form.
                new ValueComparer<IReadOnlyList<ChangeLogChange>>(
                    (left, right) => ChangeLogJson.SerializeChanges(left!) == ChangeLogJson.SerializeChanges(right!),
                    changes => ChangeLogJson.SerializeChanges(changes).GetHashCode(StringComparison.Ordinal),
                    changes => changes));

        // Read from the latest entry to the oldest; entries written by the same save share their date
        // and follow the order of their time-ordered identifiers.
        builder.HasIndex(e => new { e.CreatedAt, e.Id });

        // Mirrors ChangeLogEntry.Modification: only a modification has changes, and always at least one.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ChangeLogEntries_ChangesOfModificationOnly",
            $"(jsonb_array_length(\"{nameof(ChangeLogEntry.Changes)}\") > 0) = (\"{nameof(ChangeLogEntry.Operation)}\" = {(int)ChangeLogOperation.Modification})"));
    }
}
