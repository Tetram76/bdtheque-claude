using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Records;

/// <summary>
/// The state of the record of an album its address carries (visuel/fiche-album/contenu.md): the visual open in the viewer,
/// named by its edition and its position, and the action the address records.
/// </summary>
public sealed class AlbumRecordQueryTests
{
    private static readonly Guid AlbumId = Guid.Parse("0197a0b2-0000-7000-8000-000000000001");
    private static readonly Guid EditionId = Guid.Parse("0197a0b2-0000-7000-8000-000000000002");

    [Fact]
    public void Parse_ReadsTheEditionAndTheVisual() =>
        Assert.Equal(new AlbumRecordQuery(EditionId, 3), AlbumRecordQuery.Parse($"?edition={EditionId}&visual=3"));

    [Theory]
    [InlineData("")]
    [InlineData("?edition=nothing&visual=0")]
    [InlineData("?visual=-2")]
    [InlineData("?visual=two")]
    public void Parse_LeavesOutWhatItCannotRead(string query) =>
        Assert.Equal(new AlbumRecordQuery(), AlbumRecordQuery.Parse(query));

    [Fact]
    public void Visual_OpensTheViewer_OnItsEditionAndPosition() =>
        Assert.Equal($"/albums/{AlbumId}?edition={EditionId}&visual=4", AlbumRecordQuery.ForVisual(EditionId, 4).Href(AlbumId));

    [Fact]
    public void Record_WithoutState_IsItsPlainAddress() =>
        Assert.Equal($"/albums/{AlbumId}", new AlbumRecordQuery().Href(AlbumId));

    [Fact]
    public void Action_IsTheVisualShown_WithItsEdition() =>
        Assert.Equal((AudienceFunctions.AlbumRecordVisual, $"edition={EditionId}&visual=4"), AlbumRecordQuery.ForVisual(EditionId, 4).Action);

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Address_NamingHalfAVisual_IsTheRecordAlone_WithoutAction(bool withEdition, bool withVisual)
    {
        var query = new AlbumRecordQuery(withEdition ? EditionId : null, withVisual ? 2 : null);

        Assert.Equal($"/albums/{AlbumId}", query.Href(AlbumId));
        Assert.Null(query.Action);
    }

    [Fact]
    public void Action_IsNone_ForTheRecordAsItOpens() =>
        Assert.Null(new AlbumRecordQuery().Action);
}
