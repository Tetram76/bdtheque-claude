using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// Trace of one write on a record (Entrée du journal des modifications, modele-metier.md): added
/// automatically with the write, in the same operation, then never modified nor deleted. Its creation
/// date is the date of the write.
/// </summary>
/// <remarks>
/// Nothing in an entry is a text fixed at the time of the write: the type of record, the operation,
/// the attributes, the values and the label are presented in the display culture in force when the
/// change log is read (fonctionnel/presentation.md § Langue et culture d'affichage).
/// </remarks>
public sealed class ChangeLogEntry : EntityBase
{
    public RecordType RecordType { get; private set; }

    // Kept after the record is deleted: no foreign key ties the entry to the record.
    public Guid RecordId { get; private set; }

    public RecordLabel Label { get; private set; } = null!;
    public ChangeLogOperation Operation { get; private set; }

    /// <summary>For a modification only: each attribute changed, with its value before and after.</summary>
    public IReadOnlyList<ChangeLogChange> Changes { get; private set; } = [];

    // EF Core parameterless constructor
    private ChangeLogEntry() { }

    private ChangeLogEntry(
        RecordType recordType, Guid recordId, RecordLabel label, ChangeLogOperation operation, IEnumerable<ChangeLogChange> changes)
    {
        ArgumentNullException.ThrowIfNull(label);
        RecordType = recordType;
        RecordId = recordId;
        Label = label;
        Operation = operation;
        Changes = changes.ToList().AsReadOnly();
    }

    public static ChangeLogEntry Creation(RecordType recordType, Guid recordId, RecordLabel label) =>
        new(recordType, recordId, label, ChangeLogOperation.Creation, []);

    public static ChangeLogEntry Deletion(RecordType recordType, Guid recordId, RecordLabel label) =>
        new(recordType, recordId, label, ChangeLogOperation.Deletion, []);

    /// <param name="changes">At least one change: a write that changes nothing is not a modification.</param>
    public static ChangeLogEntry Modification(
        RecordType recordType, Guid recordId, RecordLabel label, IReadOnlyCollection<ChangeLogChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
            throw new ArgumentException("A modification records at least one change.", nameof(changes));

        return new ChangeLogEntry(recordType, recordId, label, ChangeLogOperation.Modification, changes);
    }
}
