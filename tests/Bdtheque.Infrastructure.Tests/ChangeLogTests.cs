using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Every write on a record adds its entry to the change log, in the same save (modele-metier.md §
/// Entrée du journal des modifications), recording data rather than texts formatted for display.
/// </summary>
public sealed class ChangeLogTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    private BdthequeDbContext Context => _fixture.Context;

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Creation_IsRecordedAtTheDateOfTheWrite()
    {
        var genre = new Genre("Aventure");
        Context.Genres.Add(genre);
        await Context.SaveChangesAsync();

        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(RecordType.Genre, entry.RecordType);
        Assert.Equal(genre.Id, entry.RecordId);
        Assert.Equal(ChangeLogOperation.Creation, entry.Operation);
        Assert.Equal(new NameLabel("Aventure"), entry.Label);
        Assert.Empty(entry.Changes);
        Assert.Equal(genre.CreatedAt, entry.CreatedAt);
    }

    [Fact]
    public async Task Modification_RecordsEachAttributeChangedWithItsValuesBeforeAndAfter()
    {
        var genre = new Genre("Aventure");
        Context.Genres.Add(genre);
        await Context.SaveChangesAsync();

        genre.SetLabel("Aventures");
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Last();
        Assert.Equal(ChangeLogOperation.Modification, entry.Operation);
        Assert.Equal(new NameLabel("Aventures"), entry.Label);
        Assert.Equal([new ChangeLogChange(ChangeLogAttribute.Label, new TextValue("Aventure"), new TextValue("Aventures"))], entry.Changes);
    }

    [Fact]
    public async Task WriteThatChangesNothing_IsNotRecorded()
    {
        // Every write on an aggregate rewrites its root, even when only a child changes.
        var genre = new Genre("Aventure");
        Context.Genres.Add(genre);
        await Context.SaveChangesAsync();
        var version = Context.VersionOf(genre);
        Context.ChangeTracker.Clear();

        await using (var transaction = await Context.Database.BeginTransactionAsync())
        {
            var root = await Context.LoadAggregateForWriteAsync<Genre>(genre.Id, version);
            root.SetLabel("Aventure");
            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        Assert.Single(await EntriesAsync());
    }

    [Fact]
    public async Task ModificationOfSavedSearch_RecordsItsCriteriaAndItsVariablesWithTheirDefaultValues()
    {
        const string tintin = """{"kind":"criterion","field":"title","comparison":"Contains","values":["Tintin"]}""";
        const string asterix = """{"kind":"criterion","field":"title","comparison":"Contains","values":["Astérix"]}""";
        var search = new SavedSearch("Titre", EntityKind.Album, tintin, [new SearchVariable("Titre", "/values/0")], []);
        Context.SavedSearches.Add(search);
        await Context.SaveChangesAsync();

        // The default value of a variable is the constant it replaces.
        search.SetName("Albums par titre");
        search.SetSearch(EntityKind.Series, asterix, search.Variables, []);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Last();
        Assert.Equal(RecordType.SavedSearch, entry.RecordType);
        Assert.Equal(new NameLabel("Albums par titre"), entry.Label);
        Assert.Equal(
            [
                new ChangeLogChange(ChangeLogAttribute.Name, new TextValue("Titre"), new TextValue("Albums par titre")),
                new ChangeLogChange(ChangeLogAttribute.SearchedEntity, new EnumValue((int)EntityKind.Album), new EnumValue((int)EntityKind.Series)),
                new ChangeLogChange(ChangeLogAttribute.SearchCriteria, new SearchCriteriaValue(tintin), new SearchCriteriaValue(asterix)),
                new ChangeLogChange(
                    ChangeLogAttribute.SearchVariables,
                    new SearchVariablesValue([new SearchVariableValue("Titre", "/values/0", "Tintin")]),
                    new SearchVariablesValue([new SearchVariableValue("Titre", "/values/0", "Astérix")])),
            ],
            entry.Changes);
    }

    [Fact]
    public async Task ModificationOfSavedSearchCriteria_OutsideTheConstantsOfItsVariables_LeavesItsVariablesUnchanged()
    {
        // An attribute read from several properties changes only if its value does, not if one of its properties does.
        const string before = """{"kind":"group","operator":"And","conditions":[{"kind":"criterion","field":"title","comparison":"Contains","values":["Tintin"]}]}""";
        const string after = """{"kind":"group","operator":"Or","conditions":[{"kind":"criterion","field":"title","comparison":"Contains","values":["Tintin"]}]}""";
        var search = new SavedSearch("Titre", EntityKind.Album, before, [new SearchVariable("Titre", "/conditions/0/values/0")], []);
        Context.SavedSearches.Add(search);
        await Context.SaveChangesAsync();

        search.SetSearch(EntityKind.Album, after, search.Variables, []);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Last();
        Assert.Equal([new ChangeLogChange(ChangeLogAttribute.SearchCriteria, new SearchCriteriaValue(before), new SearchCriteriaValue(after))], entry.Changes);
    }

    [Fact]
    public async Task RewriteOfSavedSearch_ThatChangesNothing_IsNotRecorded()
    {
        const string criteria = """{"kind":"criterion","field":"title","comparison":"Contains","values":["Tintin"]}""";
        var search = new SavedSearch("Titre", EntityKind.Album, criteria, [new SearchVariable("Titre", "/values/0")], []);
        Context.SavedSearches.Add(search);
        await Context.SaveChangesAsync();
        var version = Context.VersionOf(search);
        Context.ChangeTracker.Clear();

        await using (var transaction = await Context.Database.BeginTransactionAsync())
        {
            var root = await Context.LoadAggregateForWriteAsync<SavedSearch>(search.Id, version);
            root.SetName("Titre");
            root.SetSearch(EntityKind.Album, criteria, [new SearchVariable("Titre", "/values/0")], []);
            await Context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        Assert.Single(await EntriesAsync());
    }

    [Fact]
    public async Task ModificationOfAlbum_RecordsTextsAndDatesAsData()
    {
        var album = new Album("Le Lotus bleu", null);
        Context.Albums.Add(album);
        await Context.SaveChangesAsync();

        album.SetTitleSeriesAndContributions("Les Cigares du pharaon", null, []);
        album.SetFirstPublicationDate(1934, 12);
        album.SetRating(AlbumRating.VeryGood);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Last();
        Assert.Equal(new AlbumLabel("Les Cigares du pharaon", null, AlbumType.Regular, false, null, null, null), entry.Label);
        Assert.Equal(
            [
                new ChangeLogChange(ChangeLogAttribute.Title, new TextValue("Le Lotus bleu"), new TextValue("Les Cigares du pharaon")),
                new ChangeLogChange(ChangeLogAttribute.SortKey, new TextValue("Lotus bleu [Le]"), new TextValue("Cigares du pharaon [Les]")),
                new ChangeLogChange(ChangeLogAttribute.FirstPublicationDate, null, new PartialDateValue(1934, 12)),
                new ChangeLogChange(ChangeLogAttribute.Rating, null, new EnumValue((int)AlbumRating.VeryGood)),
            ],
            entry.Changes);
    }

    [Fact]
    public async Task ReferenceToAnotherRecord_IsRecordedWithItsLabel()
    {
        var tintin = new Series("Tintin");
        var spirou = new Series("Spirou et Fantasio");
        var album = new Album(null, tintin);
        album.SetVolumeNumber(5);
        Context.AddRange(tintin, spirou, album);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Neither series is tracked any more: their labels are read from the database.
        var reloaded = await Context.Albums.SingleAsync();
        reloaded.SetTitleSeriesAndContributions(null, await Context.Series.SingleAsync(s => s.Id == spirou.Id), []);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Last();
        Assert.Equal(new AlbumLabel(null, "Spirou et Fantasio", AlbumType.Regular, false, 5, null, null), entry.Label);
        Assert.Equal(
            [
                new ChangeLogChange(
                    ChangeLogAttribute.Series,
                    new ReferenceValue(tintin.Id, new NameLabel("Tintin")),
                    new ReferenceValue(spirou.Id, new NameLabel("Spirou et Fantasio"))),
            ],
            entry.Changes);
    }

    [Fact]
    public async Task AssociationAddedOrRemoved_IsAChangeOfTheRecordCarryingIt()
    {
        var aventure = new Genre("Aventure");
        var humour = new Genre("Humour");
        var album = new Album("Gaston", null);
        album.AddGenre(aventure);
        Context.AddRange(aventure, humour, album);
        await Context.SaveChangesAsync();

        album.RemoveGenre(aventure);
        album.AddGenre(humour);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Single(e => e.Operation == ChangeLogOperation.Modification);
        Assert.Equal(RecordType.Album, entry.RecordType);
        Assert.Equal(album.Id, entry.RecordId);
        Assert.Equal(
            new HashSet<ChangeLogChange>
            {
                new(ChangeLogAttribute.Genre, new ReferenceValue(aventure.Id, new NameLabel("Aventure")), null),
                new(ChangeLogAttribute.Genre, null, new ReferenceValue(humour.Id, new NameLabel("Humour"))),
            },
            entry.Changes.ToHashSet());
    }

    [Fact]
    public async Task ContributionsReplaced_AreChangesOfTheirAlbum()
    {
        var herge = new Author("Remi", "Georges", "Hergé");
        var jacobs = new Author("Jacobs", "Edgar P.", null);
        var album = new Album("Le Lotus bleu", null, [(herge, ContributionRole.Scenarist)]);
        Context.AddRange(herge, jacobs, album);
        await Context.SaveChangesAsync();

        album.SetTitleSeriesAndContributions("Le Lotus bleu", null, [(jacobs, ContributionRole.Colorist)]);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Single(e => e.Operation == ChangeLogOperation.Modification);
        Assert.Equal(
            new HashSet<ChangeLogChange>
            {
                new(ChangeLogAttribute.Contribution,
                    new ContributionValue(herge.Id, new AuthorLabel("Remi", "Georges", "Hergé"), ContributionRole.Scenarist), null),
                new(ChangeLogAttribute.Contribution,
                    null, new ContributionValue(jacobs.Id, new AuthorLabel("Jacobs", "Edgar P.", null), ContributionRole.Colorist)),
            },
            entry.Changes.ToHashSet());
    }

    [Fact]
    public async Task VisualsAddedThenRearranged_AreChangesOfTheirEdition()
    {
        var (album, edition) = await CreateOwnedEditionAsync();
        var cover = edition.AppendVisual(VisualType.Cover, "cover.jpg");
        await Context.SaveChangesAsync();

        Assert.Equal(
            [new ChangeLogChange(ChangeLogAttribute.Visual, null, new VisualValue(cover.Id, VisualType.Cover, 0, "cover.jpg"))],
            (await EntriesAsync()).Last().Changes);

        edition.ArrangeVisuals([(cover.Id, VisualType.BackCover)]);
        await Context.SaveChangesAsync();

        var entry = (await EntriesAsync()).Last();
        Assert.Equal(RecordType.Edition, entry.RecordType);
        Assert.Equal(edition.Id, entry.RecordId);
        Assert.Equal(
            [
                new ChangeLogChange(
                    ChangeLogAttribute.Visual,
                    new VisualValue(cover.Id, VisualType.Cover, 0, "cover.jpg"),
                    new VisualValue(cover.Id, VisualType.BackCover, 0, "cover.jpg")),
            ],
            entry.Changes);
        Assert.DoesNotContain(await EntriesAsync(), e => e.RecordId == album.Id && e.Operation == ChangeLogOperation.Modification);
    }

    [Fact]
    public async Task EditionCreatedInItsAlbum_IsRecordedWithTheLabelsOfItsAlbumAndPublisher()
    {
        var (album, edition) = await CreateOwnedEditionAsync();

        var entries = await EntriesAsync();
        var entry = entries.Single(e => e.RecordType == RecordType.Edition);
        Assert.Equal(ChangeLogOperation.Creation, entry.Operation);
        Assert.Equal(
            new EditionLabel(new AlbumLabel("Gaston", null, AlbumType.Regular, false, null, null, null), "Dupuis", "Gaston", 1985, null),
            entry.Label);
        Assert.Equal(edition.Id, entry.RecordId);
        Assert.Single(entries, e => e.RecordId == album.Id);
    }

    [Fact]
    public async Task AcquisitionModified_RecordsAmountsDatesAndEnumerations()
    {
        var (_, edition) = await CreateOwnedEditionAsync();

        edition.SetPublicationYearAndAcquisition(
            1985, new EditionAcquisition(AcquisitionMode.Purchase, new DateOnly(1986, 3, 14), 45.5m, "FRF", false, null, null));
        edition.SetBinding(BindingType.Hardcover);
        await Context.SaveChangesAsync();

        Assert.Equal(
            [
                new ChangeLogChange(ChangeLogAttribute.Binding, null, new EnumValue((int)BindingType.Hardcover)),
                new ChangeLogChange(
                    ChangeLogAttribute.AcquisitionMode, new EnumValue((int)AcquisitionMode.Gift), new EnumValue((int)AcquisitionMode.Purchase)),
                new ChangeLogChange(ChangeLogAttribute.AcquisitionDate, null, new DateValue(new DateOnly(1986, 3, 14))),
                new ChangeLogChange(ChangeLogAttribute.AcquisitionPrice, null, new AmountValue(45.5m, "FRF")),
            ],
            (await EntriesAsync()).Last().Changes);
    }

    [Fact]
    public async Task AlbumDeleted_RecordsTheDeletionOfEveryRecordTakenAlong()
    {
        var (album, edition) = await CreateOwnedEditionAsync();
        var intended = new Edition(album, edition.Publisher);
        var intent = album.AddPurchaseIntent(intended);
        await Context.SaveChangesAsync();

        Context.Albums.Remove(album);
        await Context.SaveChangesAsync();

        var deletions = (await EntriesAsync()).Where(e => e.Operation == ChangeLogOperation.Deletion).ToList();
        Assert.Equal(
            new HashSet<(RecordType, Guid)>
            {
                (RecordType.Album, album.Id), (RecordType.Edition, edition.Id), (RecordType.Edition, intended.Id),
                (RecordType.PurchaseIntent, intent.Id),
            },
            deletions.Select(e => (e.RecordType, e.RecordId)).ToHashSet());
        var intentLabel = Assert.IsType<PurchaseIntentLabel>(deletions.Single(e => e.RecordId == intent.Id).Label);
        Assert.Null(intentLabel.Album);
        Assert.Equal("Dupuis", intentLabel.Edition!.PublisherName);
    }

    [Fact]
    public async Task PublisherDeleted_RecordsTheDeletionOfItsCollections()
    {
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("Lucky Luke");
        Context.Publishers.Add(publisher);
        await Context.SaveChangesAsync();

        Context.Publishers.Remove(publisher);
        await Context.SaveChangesAsync();

        Assert.Contains(
            await EntriesAsync(),
            e => e is { Operation: ChangeLogOperation.Deletion, RecordType: RecordType.PublisherCollection }
                 && e.RecordId == collection.Id && e.Label == new PublisherCollectionLabel("Lucky Luke", "Dargaud"));
    }

    [Fact]
    public async Task PublisherDeletedWithoutItsCollectionsLoaded_IsRefused()
    {
        // The database would delete the collections by cascade, out of sight of the change log.
        var publisher = new Publisher("Dargaud");
        publisher.AddCollection("Lucky Luke");
        Context.Publishers.Add(publisher);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        Context.Publishers.Remove(await Context.Publishers.SingleAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SynchronousSave_IsRefused()
    {
        // The change log reads the labels it records from the database: every save is asynchronous.
        Context.Genres.Add(new Genre("Aventure"));

        Assert.Throws<NotSupportedException>(() => Context.SaveChanges());
    }

    private async Task<(Album Album, Edition Edition)> CreateOwnedEditionAsync()
    {
        var album = new Album("Gaston", null);
        var publisher = new Publisher("Dupuis");
        var collection = publisher.AddCollection("Gaston");
        var edition = new Edition(album, publisher);
        edition.SetPublisher(publisher, collection);
        edition.SetPublicationYear(1985);
        album.RecordAcquisition(edition, AcquisitionMode.Gift);
        Context.AddRange(album, publisher);
        await Context.SaveChangesAsync();
        return (album, edition);
    }

    private async Task<List<ChangeLogEntry>> EntriesAsync() =>
        await Context.Set<ChangeLogEntry>().AsNoTracking().OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToListAsync();
}
