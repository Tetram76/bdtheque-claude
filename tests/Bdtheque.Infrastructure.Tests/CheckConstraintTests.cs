using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Verifies that the database-level CHECK constraint on <c>Authors</c> is enforced.
/// These tests bypass the domain model (which also enforces the rule) and write raw SQL,
/// ensuring that the constraint acts as a genuine defence-in-depth layer.
/// </summary>
public sealed class CheckConstraintTests : IDisposable
{
    // Each test gets its own isolated database so that a failing insert in one test
    // cannot affect the state seen by another.
    private readonly BdthequeDbContextFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AuthorCheckConstraint_NullLastNameAndPseudonym_ThrowsAtDatabase()
    {
        // Insert directly via raw SQL to bypass the domain-layer guard
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, NULL, NULL)",
                id));
    }

    [Fact]
    public async Task AuthorCheckConstraint_LastNameProvided_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, 'Dupont', NULL)",
            id);

        var count = await _fixture.Context.Authors.CountAsync(a => a.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AuthorCheckConstraint_PseudonymProvided_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, NULL, 'Moebius')",
            id);

        var count = await _fixture.Context.Authors.CountAsync(a => a.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AuthorCheckConstraint_BlankLastNameAndNullPseudonym_ThrowsAtDatabase()
    {
        // Blank strings bypass a simple IS NOT NULL check; the constraint uses LENGTH(TRIM(...))
        // to also reject whitespace-only values inserted via raw SQL.
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, '', NULL)",
                id));
    }

    [Fact]
    public async Task AuthorCheckConstraint_BothBlankStrings_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, '   ', '')",
                id));
    }

    [Theory]
    [InlineData("Genres", "Label", "'   '")]
    [InlineData("Universes", "Name", "''")]
    [InlineData("Publishers", "Name", "'   '")]
    public async Task RequiredTextCheckConstraint_BlankValue_ThrowsAtDatabase(string table, string column, string blankLiteral)
    {
        // Mirrors AuthorCheckConstraint_*BlankStrings*: IsRequired() alone only enforces
        // NOT NULL, so raw SQL could otherwise persist a blank label/name.
        var id = Guid.CreateVersion7();
        var sql = $"INSERT INTO \"{table}\" (\"Id\", \"{column}\") VALUES ({{0}}, {blankLiteral})";
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(sql, id));
    }

    [Fact]
    public async Task PublisherCollectionCheckConstraint_BlankName_ThrowsAtDatabase()
    {
        var publisherId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Publishers\" (\"Id\", \"Name\") VALUES ({0}, 'Casterman')", publisherId);

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"PublisherCollections\" (\"Id\", \"Name\", \"PublisherId\") VALUES ({0}, '   ', {1})",
                id, publisherId));
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("SortKey")]
    public async Task SeriesCheckConstraint_BlankTitleOrSortKey_ThrowsAtDatabase(string blankColumn)
    {
        var id = Guid.CreateVersion7();
        var otherColumn = blankColumn == "Title" ? "SortKey" : "Title";
        var sql = $"INSERT INTO \"Series\" (\"Id\", \"{blankColumn}\", \"{otherColumn}\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\") " +
                  $"VALUES ({{0}}, '   ', 'Valeur', false, false, false)";
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(sql, id));
    }

    [Fact]
    public async Task SeriesCheckConstraint_NonPositiveTheoreticalVolumeCount_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Series\" (\"Id\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"TheoreticalVolumeCount\") " +
                "VALUES ({0}, 'Tintin', 'Tintin', false, false, false, 0)",
                id));
    }

    [Fact]
    public async Task SeriesCheckConstraint_TemplateCollectionWithoutTemplatePublisher_ThrowsAtDatabase()
    {
        // Guards the single-table half of the template coherence rule. The cross-table half
        // (the collection must belong to the template publisher) is enforced only by the
        // domain layer (see Series.SetTemplate) — a composite FK would add real schema
        // complexity for a raw-SQL bypass scenario that isn't plausible with a single admin user.
        var publisherId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Publishers\" (\"Id\", \"Name\") VALUES ({0}, 'Casterman')", publisherId);
        var collectionId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"PublisherCollections\" (\"Id\", \"Name\", \"PublisherId\") VALUES ({0}, 'Tintin', {1})",
            collectionId, publisherId);

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Series\" (\"Id\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"TemplatePublisherCollectionId\") " +
                "VALUES ({0}, 'Tintin', 'Tintin', false, false, false, {1})",
                id, collectionId));
    }

    private const string InsertAlbumSql =
        "INSERT INTO \"Albums\" (\"Id\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\", " +
        "\"VolumeNumber\", \"StartVolumeNumber\", \"EndVolumeNumber\", \"FirstPublicationYear\", \"FirstPublicationMonth\", \"SeriesId\") " +
        "VALUES ({0}, {1}, {2}, false, {3}, false, {4}, {5}, {6}, {7}, {8}, {9})";

    [Fact]
    public async Task AlbumCheckConstraint_NoTitleNoSeries_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, null!, null!, nameof(AlbumType.Regular),
                null!, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_BlankTitleWithSeries_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "   ", null!, nameof(AlbumType.Regular),
                null!, null!, null!, null!, null!, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_SortKeyWithoutTitle_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, null!, "Orphan Key", nameof(AlbumType.Regular),
                null!, null!, null!, null!, null!, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_NonPositiveVolumeNumber_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Regular),
                0, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeOnlyStart_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Omnibus),
                null!, 1, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeStartGreaterThanEnd_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Omnibus),
                null!, 6, 1, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeOnNonOmnibus_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Regular),
                null!, 1, 6, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_PublicationMonthWithoutYear_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Regular),
                null!, null!, null!, null!, 6, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task AlbumCheckConstraint_PublicationMonthOutOfRange_ThrowsAtDatabase(int month)
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Regular),
                null!, null!, null!, 1978, month, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_PublicationYearNonPositive_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Regular),
                null!, null!, null!, 0, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_ValidRegularAlbum_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertAlbumSql, id, "Tintin", "Tintin", nameof(AlbumType.Regular),
            5, null!, null!, 1978, 6, null!);

        var count = await _fixture.Context.Albums.CountAsync(a => a.Id == id);
        Assert.Equal(1, count);
    }

    private async Task<Guid> InsertSeriesAsync()
    {
        var seriesId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Series\" (\"Id\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\") " +
            "VALUES ({0}, 'Tintin', 'Tintin', false, false, false)", seriesId);
        return seriesId;
    }
}
