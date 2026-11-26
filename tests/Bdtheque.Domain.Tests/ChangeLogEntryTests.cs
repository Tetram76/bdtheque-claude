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
        DomainAssert.Throws<ArgumentException>(() => ChangeLogEntry.Modification(RecordType.Genre, Guid.CreateVersion7(), Label, []));
    }

    [Fact]
    public void Change_OfAnElementAddedOrRemoved_HasAValueOnOneSideOnly()
    {
        var genre = new ReferenceValue(Guid.CreateVersion7(), Label);

        var added = new ChangeLogChange(ChangeLogAttribute.Genre, null, genre);
        var removed = new ChangeLogChange(ChangeLogAttribute.Genre, genre, null);

        Assert.Equal((null, genre), (added.Before, added.After));
        Assert.Equal((genre, null), (removed.Before, removed.After));
    }

    [Fact]
    public void SearchVariablesValue_IsComparedByItsVariables()
    {
        var variables = new SearchVariablesValue([new SearchVariableValue("Année", "/conditions/0/value", "1980")]);

        Assert.Equal(variables, new SearchVariablesValue([new SearchVariableValue("Année", "/conditions/0/value", "1980")]));
        Assert.NotEqual(variables, new SearchVariablesValue([new SearchVariableValue("Année", "/conditions/0/value", "1990")]));
        Assert.False(variables.Equals(null));
    }

    [Fact]
    public void Change_WithNeitherValueBeforeNorAfter_IsRefused()
    {
        DomainAssert.Throws<ArgumentException>(() => new ChangeLogChange(ChangeLogAttribute.Label, null, null));
    }

    [Fact]
    public void Entry_RequiresItsLabel()
    {
        DomainAssert.Throws<ArgumentNullException>(() => ChangeLogEntry.Creation(RecordType.Genre, Guid.CreateVersion7(), null!));
    }
}
