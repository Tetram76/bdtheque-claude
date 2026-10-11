using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class ChangeLogEntryTests
{
    private static readonly RecordLabel Label = new NameLabel("Aventure");

    [Fact]
    public void Creation_RecordsTheRecordWithoutChanges()
    {
        var recordId = Guid.CreateVersion7();

        var entry = ChangeLogEntry.Creation(RecordType.Genre, recordId, Label);

        Assert.Equal(RecordType.Genre, entry.RecordType);
        Assert.Equal(recordId, entry.RecordId);
        Assert.Equal(Label, entry.Label);
        Assert.Equal(ChangeLogOperation.Creation, entry.Operation);
        Assert.Empty(entry.Changes);
    }

    [Fact]
    public void Deletion_RecordsTheRecordWithoutChanges()
    {
        var entry = ChangeLogEntry.Deletion(RecordType.Genre, Guid.CreateVersion7(), Label);

        Assert.Equal(ChangeLogOperation.Deletion, entry.Operation);
        Assert.Empty(entry.Changes);
    }

    [Fact]
    public void Modification_RecordsEachChange()
    {
        var change = new ChangeLogChange(ChangeLogAttribute.Label, new TextValue("Aventure"), new TextValue("Aventures"));

        var entry = ChangeLogEntry.Modification(RecordType.Genre, Guid.CreateVersion7(), Label, [change]);

        Assert.Equal(ChangeLogOperation.Modification, entry.Operation);
        Assert.Equal([change], entry.Changes);
    }

    [Fact]
    public void Modification_WithoutChange_IsRefused()
    {
        // Nothing changed means nothing was written: an empty modification is a programming error.
        Assert.Throws<ArgumentException>(() => ChangeLogEntry.Modification(RecordType.Genre, Guid.CreateVersion7(), Label, []));
    }

    [Fact]
    public void Change_WithNeitherValueBeforeNorAfter_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => new ChangeLogChange(ChangeLogAttribute.Label, null, null));
    }

    [Fact]
    public void Entry_RequiresItsLabel()
    {
        Assert.Throws<ArgumentNullException>(() => ChangeLogEntry.Creation(RecordType.Genre, Guid.CreateVersion7(), null!));
    }
}
