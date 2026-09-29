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
                InsertAlbumSql, id, null!, null!, (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_BlankTitleWithSeries_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "   ", null!, (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_SortKeyWithoutTitle_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, null!, "Orphan Key", (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_TitleWithoutSortKey_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", null!, (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_ManualSortKeyWithoutTitle_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Albums\" (\"Id\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\", \"SeriesId\") " +
                "VALUES ({0}, NULL, NULL, true, {1}, false, {2})", id, (int)AlbumType.Regular, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_NonPositiveVolumeNumber_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                0, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeOnlyStart_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Omnibus,
                null!, 1, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeStartGreaterThanEnd_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Omnibus,
                null!, 6, 1, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeOnNonOmnibus_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                null!, 1, 6, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_PublicationMonthWithoutYear_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
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
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                null!, null!, null!, 1978, month, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_PublicationYearNonPositive_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                null!, null!, null!, 0, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_ValidRegularAlbum_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
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

    private async Task<Guid> InsertAlbumAsync()
    {
        var albumId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Albums\" (\"Id\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\") " +
            "VALUES ({0}, 'Tintin', 'Tintin', false, {1}, false)", albumId, (int)AlbumType.Regular);
        return albumId;
    }

    private async Task<Guid> InsertAuthorAsync()
    {
        var authorId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"Pseudonym\") VALUES ({0}, 'Hergé')", authorId);
        return authorId;
    }

    private const string InsertContributionSql =
        "INSERT INTO \"Contributions\" (\"Id\", \"AlbumId\", \"SeriesId\", \"AuthorId\", \"Role\") " +
        "VALUES ({0}, {1}, {2}, {3}, {4})";

    [Fact]
    public async Task ContributionCheckConstraint_NeitherAlbumNorSeries_ThrowsAtDatabase()
    {
        var authorId = await InsertAuthorAsync();
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertContributionSql, id, null!, null!, authorId, (int)ContributionRole.Scenarist));
    }

    [Fact]
    public async Task ContributionCheckConstraint_BothAlbumAndSeries_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var seriesId = await InsertSeriesAsync();
        var authorId = await InsertAuthorAsync();
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertContributionSql, id, albumId, seriesId, authorId, (int)ContributionRole.Scenarist));
    }

    [Fact]
    public async Task ContributionCheckConstraint_AlbumOnly_Succeeds()
    {
        var albumId = await InsertAlbumAsync();
        var authorId = await InsertAuthorAsync();
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertContributionSql, id, albumId, null!, authorId, (int)ContributionRole.Scenarist);

        var count = await _fixture.Context.Contributions.CountAsync(c => c.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ContributionCheckConstraint_SeriesOnly_Succeeds()
    {
        var seriesId = await InsertSeriesAsync();
        var authorId = await InsertAuthorAsync();
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertContributionSql, id, null!, seriesId, authorId, (int)ContributionRole.Scenarist);

        var count = await _fixture.Context.Contributions.CountAsync(c => c.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ContributionUniqueIndex_DuplicateAlbumRoleAuthor_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var authorId = await InsertAuthorAsync();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertContributionSql, Guid.CreateVersion7(), albumId, null!, authorId, (int)ContributionRole.Scenarist);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertContributionSql, Guid.CreateVersion7(), albumId, null!, authorId, (int)ContributionRole.Scenarist));
    }

    [Fact]
    public async Task ContributionUniqueIndex_DuplicateSeriesRoleAuthor_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();
        var authorId = await InsertAuthorAsync();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertContributionSql, Guid.CreateVersion7(), null!, seriesId, authorId, (int)ContributionRole.Illustrator);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertContributionSql, Guid.CreateVersion7(), null!, seriesId, authorId, (int)ContributionRole.Illustrator));
    }

    [Fact]
    public async Task ContributionUniqueIndex_SameRoleAuthorDifferentOwnerType_Succeeds()
    {
        // Verifies the two partial unique indexes are independent: an album-owned and a
        // series-owned contribution can share the same Role/AuthorId without colliding.
        var albumId = await InsertAlbumAsync();
        var seriesId = await InsertSeriesAsync();
        var authorId = await InsertAuthorAsync();

        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertContributionSql, Guid.CreateVersion7(), albumId, null!, authorId, (int)ContributionRole.Colorist);
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertContributionSql, Guid.CreateVersion7(), null!, seriesId, authorId, (int)ContributionRole.Colorist);

        var count = await _fixture.Context.Contributions.CountAsync(c => c.AuthorId == authorId);
        Assert.Equal(2, count);
    }

    private async Task<Guid> InsertPublisherAsync(string name = "Casterman")
    {
        var publisherId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Publishers\" (\"Id\", \"Name\") VALUES ({0}, {1})", publisherId, name);
        return publisherId;
    }

    private const string InsertEditionSql =
        "INSERT INTO \"Editions\" (\"Id\", \"AlbumId\", \"PublisherId\", \"IsDedicated\", \"IsColor\", \"IsSecondHand\", \"IsFree\", " +
        "\"PublicationYear\", \"PageCount\", \"AcquisitionMode\", \"AcquisitionDate\", \"AcquisitionAmount\", \"AcquisitionCurrency\") " +
        "VALUES ({0}, {1}, {2}, false, true, false, {3}, {4}, {5}, {6}, {7}, {8}, {9})";

    [Fact]
    public async Task EditionCheckConstraint_NonPositivePublicationYear_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, 0, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_NonPositivePageCount_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, 0, null!, null!, null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_AmountWithoutCurrency_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, (int)AcquisitionMode.Purchase, null!, 10, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_NonPositiveAmount_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, (int)AcquisitionMode.Purchase, null!, 0, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_DateWithoutAcquisitionMode_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, null!, new DateOnly(2020, 1, 1), null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_AmountWithoutAcquisitionMode_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, null!, null!, 10, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_FreeWithAmount_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, true, null!, null!, (int)AcquisitionMode.Gift, null!, 10, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_ValidMinimalRow_Succeeds()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertEditionSql, id, albumId, publisherId, false, null!, null!, null!, null!, null!, null!);

        var count = await _fixture.Context.Editions.CountAsync(e => e.Id == id);
        Assert.Equal(1, count);
    }

    private async Task<Guid> InsertEditionAsync()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();
        var editionId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Editions\" (\"Id\", \"AlbumId\", \"PublisherId\", \"IsDedicated\", \"IsColor\", \"IsSecondHand\", \"IsFree\") " +
            "VALUES ({0}, {1}, {2}, false, true, false, false)", editionId, albumId, publisherId);
        return editionId;
    }

    private const string InsertEditionVisualSql =
        "INSERT INTO \"EditionVisuals\" (\"Id\", \"EditionId\", \"Type\", \"MediaReference\", \"DisplayOrder\") " +
        "VALUES ({0}, {1}, {2}, {3}, {4})";

    [Fact]
    public async Task EditionVisualCheckConstraint_BlankMediaReference_ThrowsAtDatabase()
    {
        var editionId = await InsertEditionAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionVisualSql, id, editionId, (int)VisualType.Cover, "   ", 0));
    }

    [Fact]
    public async Task EditionVisualCheckConstraint_NegativeDisplayOrder_ThrowsAtDatabase()
    {
        var editionId = await InsertEditionAsync();

        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionVisualSql, id, editionId, (int)VisualType.Cover, "cover.jpg", -1));
    }

    [Fact]
    public async Task EditionVisualCheckConstraint_ValidRow_Succeeds()
    {
        var editionId = await InsertEditionAsync();

        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertEditionVisualSql, id, editionId, (int)VisualType.Cover, "cover.jpg", 0);

        var count = await _fixture.Context.EditionVisuals.CountAsync(v => v.Id == id);
        Assert.Equal(1, count);
    }
}
