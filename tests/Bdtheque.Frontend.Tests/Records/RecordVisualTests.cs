using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Records;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Records;

/// <summary>
/// The mention of the kind of a record of the consultation, with the visual of its head (visuel/fiche-album/contenu.md):
/// written once for the records of an album, a series and an author, told apart by structure and text, never by look.
/// </summary>
public sealed class RecordVisualTests : BunitContext
{
    public RecordVisualTests() => Services.AddLocalization();

    [Theory]
    [InlineData(RecordType.Album, "Album")]
    [InlineData(RecordType.Series, "Série")]
    [InlineData(RecordType.Author, "Auteur")]
    public void Mention_NamesTheKindOfTheRecord_AboveItsVisual(RecordType kind, string mention)
    {
        var visual = Render<RecordVisual>(parameters => parameters
            .Add(p => p.Kind, kind)
            .AddChildContent("<div class=\"the-visual\"></div>"));

        // The mention, then the visual, alone in their group: nothing of the identity of the record is placed beside it.
        var group = visual.Find(".record-visual");
        Assert.Equal(["record-kind", "the-visual"], group.Children.Select(child => child.ClassName));
        Assert.Equal(mention, group.Children[0].TextContent);
    }

    [Theory]
    [InlineData(RecordType.Edition)]
    [InlineData(RecordType.Publisher)]
    public void Mention_OfAKindWithoutRecordInTheConsultation_IsRefused(RecordType kind)
    {
        // Only the records of an album, a series and an author carry it: any other kind is a mistake of its caller.
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<RecordVisual>(parameters => parameters.Add(p => p.Kind, kind)));
    }
}
