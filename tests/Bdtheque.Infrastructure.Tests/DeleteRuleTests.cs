using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Verifies that the database applies the deletion rules of fonctionnel.md § Suppression des
/// entités: <c>ON DELETE CASCADE</c> for every composition and association, <c>RESTRICT</c> for
/// every reference (choix-implementation.md § Suppression des entités : mise en œuvre).
/// </summary>
/// <remarks>
/// Deletions go through <c>ExecuteDeleteAsync</c>, which EF Core sends as a single SQL statement
/// without loading anything: what is removed or refused is the database's doing, not the
/// change tracker's client-side cascade.
/// </remarks>
public sealed class DeleteRuleTests : IAsyncLifetime
{
    private const char Cascade = 'c';
    private const char Restrict = 'r';

    // Every foreign key of the schema, classified by the nature of the link it carries. Listing
    // them all (rather than the changed ones) forces any new foreign key to be classified here:
    // a relation added with EF Core's default delete behaviour would otherwise slip through.
    private static readonly Dictionary<string, char> ExpectedDeleteRules = new()
    {
        // Compositions: the dependent is part of its owner and goes with it.
        ["FK_Contributions_Albums_AlbumId"] = Cascade,
        ["FK_Contributions_Series_SeriesId"] = Cascade,
        ["FK_Editions_Albums_AlbumId"] = Cascade,
        ["FK_EditionVisuals_Editions_EditionId"] = Cascade,
        ["FK_PurchaseIntents_Albums_AlbumId"] = Cascade,
        ["FK_PurchaseIntents_Editions_EditionId"] = Cascade,
        ["FK_PublisherCollections_Publishers_PublisherId"] = Cascade,

        // Associations: the join row disappears with either side.
        ["FK_AlbumGenres_Albums_AlbumId"] = Cascade,
        ["FK_AlbumGenres_Genres_GenresId"] = Cascade,
        ["FK_AlbumUniverses_Albums_AlbumId"] = Cascade,
        ["FK_AlbumUniverses_Universes_UniversesId"] = Cascade,
        ["FK_SeriesGenres_Series_SeriesId"] = Cascade,
        ["FK_SeriesGenres_Genres_GenresId"] = Cascade,
        ["FK_SeriesUniverses_Series_SeriesId"] = Cascade,
        ["FK_SeriesUniverses_Universes_UniversesId"] = Cascade,

        // References: the deletion is refused while the reference exists.
        ["FK_Albums_Series_SeriesId"] = Restrict,
        ["FK_Contributions_Authors_AuthorId"] = Restrict,
        ["FK_Editions_Publishers_PublisherId"] = Restrict,
        ["FK_Editions_PublisherCollections_PublisherCollectionId"] = Restrict,
        ["FK_Series_Publishers_TemplatePublisherId"] = Restrict,
        ["FK_Series_PublisherCollections_TemplatePublisherCollectionId"] = Restrict,
        ["FK_Universes_Universes_ParentId"] = Restrict,
    };

    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task EveryForeignKey_HasTheDeleteRuleOfItsLinkType()
    {
        var actual = await _fixture.Context.Database
            .SqlQuery<ForeignKeyDeleteRule>($"""
                SELECT conname AS "Name", confdeltype AS "DeleteRule"
                FROM pg_constraint
                WHERE contype = 'f' AND connamespace = 'public'::regnamespace
                """)
            .ToDictionaryAsync(fk => fk.Name, fk => fk.DeleteRule);

        Assert.Equal(
            ExpectedDeleteRules.OrderBy(rule => rule.Key, StringComparer.Ordinal),
            actual.OrderBy(rule => rule.Key, StringComparer.Ordinal));
    }

    [Fact]
    public async Task DeleteAlbum_RemovesItsCompositionsAndAssociations_KeepsReferencedEntities()
    {
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", series);
        var author = new Author("Remi", "Georges", "Hergé");
        var publisher = new Publisher("Casterman");
        var genre = new Genre("Aventure");
        var universe = new Universe("Franco-Belge");
        album.AddGenre(genre);
        album.AddUniverse(universe);
        album.SetTitleSeriesAndContributions(album.Title, series, [(author, ContributionRole.Scenarist)]);
        var ownedEdition = new Edition(album, publisher);
        album.RecordAcquisition(ownedEdition, AcquisitionMode.Purchase);
        ownedEdition.AddVisual(VisualType.Cover, "cover.jpg", 0);
        // An intent on an edition reaches the album through both of its foreign keys: deleting
        // the album must succeed whichever cascade path PostgreSQL follows first.
        var wishedEdition = new Edition(album, publisher);
        wishedEdition.AddVisual(VisualType.Cover, "wished-cover.jpg", 0);
        album.AddPurchaseIntent(wishedEdition);
        _fixture.Context.AddRange(series, album, author, publisher, genre, universe, ownedEdition, wishedEdition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await _fixture.Context.Albums.Where(a => a.Id == album.Id).ExecuteDeleteAsync();

        Assert.False(await _fixture.Context.Editions.AnyAsync());
        Assert.False(await _fixture.Context.EditionVisuals.AnyAsync());
        Assert.False(await _fixture.Context.PurchaseIntents.AnyAsync());
        Assert.False(await _fixture.Context.Contributions.AnyAsync());
        Assert.Equal(0, await CountRowsAsync("AlbumGenres"));
        Assert.Equal(0, await CountRowsAsync("AlbumUniverses"));
        Assert.True(await _fixture.Context.Series.AnyAsync(s => s.Id == series.Id));
        Assert.True(await _fixture.Context.Authors.AnyAsync(a => a.Id == author.Id));
        Assert.True(await _fixture.Context.Publishers.AnyAsync(p => p.Id == publisher.Id));
        Assert.True(await _fixture.Context.Genres.AnyAsync(g => g.Id == genre.Id));
        Assert.True(await _fixture.Context.Universes.AnyAsync(u => u.Id == universe.Id));
    }

    [Fact]
    public async Task DeleteEdition_RemovesItsVisualsAndIntent_KeepsTheAlbumAndItsOtherEditions()
    {
        var album = new Album("Le Lotus bleu", null);
        var publisher = new Publisher("Casterman");
        var wishedEdition = new Edition(album, publisher);
        wishedEdition.AddVisual(VisualType.Cover, "cover.jpg", 0);
        album.AddPurchaseIntent(wishedEdition);
        var otherEdition = new Edition(album, publisher);
        album.RecordAcquisition(otherEdition, AcquisitionMode.Purchase);
        _fixture.Context.AddRange(album, publisher, wishedEdition, otherEdition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await _fixture.Context.Editions.Where(e => e.Id == wishedEdition.Id).ExecuteDeleteAsync();

        Assert.False(await _fixture.Context.EditionVisuals.AnyAsync());
        Assert.False(await _fixture.Context.PurchaseIntents.AnyAsync());
        Assert.True(await _fixture.Context.Albums.AnyAsync(a => a.Id == album.Id));
        Assert.True(await _fixture.Context.Editions.AnyAsync(e => e.Id == otherEdition.Id));
    }

    [Fact]
    public async Task DeleteSeries_WithoutAlbums_RemovesItsTemplateContributionsAndAssociations()
    {
        var series = new Series("Tintin");
        var author = new Author("Remi", "Georges", "Hergé");
        var genre = new Genre("Aventure");
        var universe = new Universe("Franco-Belge");
        series.AddGenre(genre);
        series.AddUniverse(universe);
        series.SetTemplateContributions([(author, ContributionRole.Illustrator)]);
        _fixture.Context.AddRange(series, author, genre, universe);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await _fixture.Context.Series.Where(s => s.Id == series.Id).ExecuteDeleteAsync();

        Assert.False(await _fixture.Context.Contributions.AnyAsync());
        Assert.Equal(0, await CountRowsAsync("SeriesGenres"));
        Assert.Equal(0, await CountRowsAsync("SeriesUniverses"));
        Assert.True(await _fixture.Context.Authors.AnyAsync(a => a.Id == author.Id));
    }

    [Fact]
    public async Task DeleteSeries_WithAlbums_IsRefused()
    {
        var series = new Series("Tintin");
        var album = new Album(null, series);
        _fixture.Context.AddRange(series, album);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await AssertRestrictedAsync("FK_Albums_Series_SeriesId",
            () => _fixture.Context.Series.Where(s => s.Id == series.Id).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task DeleteAuthor_StillCredited_IsRefused()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author("Remi", "Georges", "Hergé");
        album.SetTitleSeriesAndContributions(album.Title, null, [(author, ContributionRole.Scenarist)]);
        _fixture.Context.AddRange(album, author);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await AssertRestrictedAsync("FK_Contributions_Authors_AuthorId",
            () => _fixture.Context.Authors.Where(a => a.Id == author.Id).ExecuteDeleteAsync());
    }

    [Fact]
    public async Task DeletePublisherCollection_UsedByAnEdition_IsRefused()
    {
        // The collection is also a composition of its publisher: the reference from the edition
        // must still win when the collection is deleted on its own.
        var album = new Album("Le Lotus bleu", null);
        var publisher = new Publisher("Casterman");
        var collection = publisher.AddCollection("Tintin");
        var edition = new Edition(album, publisher);
        edition.SetPublisher(publisher, collection);
        album.RecordAcquisition(edition, AcquisitionMode.Purchase);
        _fixture.Context.AddRange(album, publisher, collection, edition);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.ChangeTracker.Clear();

        await AssertRestrictedAsync("FK_Editions_PublisherCollections_PublisherCollectionId",
            () => _fixture.Context.PublisherCollections.Where(c => c.Id == collection.Id).ExecuteDeleteAsync());
    }

    private static async Task AssertRestrictedAsync(string constraintName, Func<Task> delete)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(delete);
        Assert.Equal(PostgresErrorCodes.RestrictViolation, exception.SqlState);
        Assert.Equal(constraintName, exception.ConstraintName);
    }

    // Join tables of implicit many-to-many relations are shared-type entities, reached by name.
    private Task<int> CountRowsAsync(string joinTable)
    {
        var joinEntity = _fixture.Context.Model.GetEntityTypes().Single(t => t.GetTableName() == joinTable);
        return _fixture.Context.Set<Dictionary<string, object>>(joinEntity.Name).CountAsync();
    }

    private sealed record ForeignKeyDeleteRule(string Name, char DeleteRule);
}
