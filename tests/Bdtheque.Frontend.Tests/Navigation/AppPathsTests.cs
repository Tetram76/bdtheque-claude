using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Navigation;

/// <summary>
/// The page that modifies a record of each type, which the latest changes of the administration lead to
/// (visuel/administration/contenu.md, Dernières modifications).
/// </summary>
public sealed class AppPathsTests
{
    private static readonly Guid Record = Guid.CreateVersion7();
    private static readonly Guid Parent = Guid.CreateVersion7();

    // Every type of record, with the page that modifies it: a type added to the contracts without its page here fails
    // EveryTypeOfRecord_HasItsPage, so that no type is left on « Pas encore implémenté » once its page exists.
    private static readonly Dictionary<RecordType, string> Expected = new()
    {
        [RecordType.Album] = AppPaths.AdminEditAlbum(Record),
        [RecordType.Edition] = AppPaths.AdminEditEdition(Parent, Record),
        [RecordType.Series] = AppPaths.AdminEditSeries(Record),
        [RecordType.PurchaseIntent] = AppPaths.AdminAlbumEditions(Parent),
        [RecordType.Author] = AppPaths.AdminEditAuthor(Record),
        [RecordType.Publisher] = AppPaths.AdminEditPublisher(Record),
        [RecordType.PublisherCollection] = AppPaths.AdminEditPublisher(Parent),
        [RecordType.Genre] = AppPaths.AdminEditGenre(Record),
        [RecordType.Universe] = AppPaths.AdminEditUniverse(Record),
        [RecordType.SavedSearch] = AppPaths.AdminSavedSearches,
        [RecordType.ApplicationSettings] = AppPaths.AdminSettings,
    };

    public static TheoryData<RecordType> RecordTypes => [.. Enum.GetValues<RecordType>()];

    [Theory]
    [MemberData(nameof(RecordTypes))]
    public void EveryTypeOfRecord_HasItsPage(RecordType type)
    {
        Assert.True(Expected.TryGetValue(type, out var expected), $"No page expected for the type {type}.");
        Assert.Equal(expected, AppPaths.AdminEdit(type, Record, Parent));
    }

    // Their page is addressed by their parent: without it — a record deleted since the entry was read — no page can be built.
    [Theory]
    [InlineData(RecordType.Edition)]
    [InlineData(RecordType.PurchaseIntent)]
    [InlineData(RecordType.PublisherCollection)]
    public void RecordAddressedByItsParent_WithoutIt_LeadsToNoPage(RecordType type) =>
        Assert.Equal(AppPaths.NotYetImplemented, AppPaths.AdminEdit(type, Record, null));

    [Fact]
    public void UnknownTypeOfRecord_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AppPaths.AdminEdit(0, Record, null));

    // The entry forms opened from a record of the consultation say so, and nothing else: the record they lead back to is the
    // one of what they enter, never read from the address (visuel/fiche-album/contenu.md, #658).
    [Fact]
    public void EntryForms_OpenedFromARecord_SaySo()
    {
        Assert.Equal($"{AppPaths.AdminEditAlbum(Record)}?from=record", AppPaths.AdminEditAlbumFromRecord(Record));
        Assert.Equal($"{AppPaths.AdminEditSeries(Record)}?from=record", AppPaths.AdminEditSeriesFromRecord(Record));
        Assert.Equal($"{AppPaths.AdminEditEdition(Parent, Record)}?from=record", AppPaths.AdminEditEditionFromRecord(Parent, Record));
        Assert.True(AppPaths.IsFromRecord("record"));
        Assert.False(AppPaths.IsFromRecord(null));
        Assert.False(AppPaths.IsFromRecord(AppPaths.FromPurchaseIntents));
    }
}
