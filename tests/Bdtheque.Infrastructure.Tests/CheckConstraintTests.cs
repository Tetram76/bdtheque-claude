using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Verifies that the database-level constraints (CHECK constraints and unique indexes) are
/// enforced. These tests bypass the domain model (which also enforces each rule) and write raw
/// SQL, ensuring that every constraint acts as a genuine defence-in-depth layer.
/// </summary>
/// <remarks>
/// Each rejection asserts the name of the violated constraint, and each row is built to violate
/// that one constraint only: a bare "some exception was thrown" would keep passing if the insert
/// started failing for an unrelated reason (e.g. a new NOT NULL column), silently leaving the
/// constraint under test unverified.
/// </remarks>
public sealed class CheckConstraintTests : IAsyncLifetime
{
    // Each test gets its own isolated database so that a failing insert in one test
    // cannot affect the state seen by another.
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private static async Task AssertViolatesAsync(string constraintName, Func<Task> write)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(write);
        Assert.Equal(constraintName, exception.ConstraintName);
    }

    private static async Task AssertViolatesNotNullAsync(string columnName, Func<Task> write)
    {
        var exception = await Assert.ThrowsAsync<PostgresException>(write);
        Assert.Equal(PostgresErrorCodes.NotNullViolation, exception.SqlState);
        Assert.Equal(columnName, exception.ColumnName);
    }

    [Fact]
    public async Task AuthorCheckConstraint_NullLastNameAndPseudonym_ThrowsAtDatabase()
    {
        // Insert directly via raw SQL to bypass the domain-layer guard
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Authors_LastNameOrPseudonym", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"LastName\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), NULL, NULL, 'Dupont', 'D')",
                id));
    }

    [Fact]
    public async Task AuthorCheckConstraint_LastNameProvided_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"LastName\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), 'Dupont', NULL, 'Dupont', 'D')",
            id);

        var count = await _fixture.Context.Authors.CountAsync(a => a.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AuthorCheckConstraint_PseudonymProvided_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"LastName\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), NULL, 'Moebius', 'Dupont', 'D')",
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
        await AssertViolatesAsync("CK_Authors_LastNameOrPseudonym", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"LastName\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), '', NULL, 'Dupont', 'D')",
                id));
    }

    [Fact]
    public async Task AuthorCheckConstraint_BothBlankStrings_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Authors_LastNameOrPseudonym", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"LastName\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), '   ', '', 'Dupont', 'D')",
                id));
    }

    [Theory]
    [InlineData("Genres", "Label", "'   '", "CK_Genres_LabelNotBlank")]
    [InlineData("Universes", "Name", "''", "CK_Universes_NameNotBlank")]
    [InlineData("Publishers", "Name", "'   '", "CK_Publishers_NameNotBlank")]
    public async Task RequiredTextCheckConstraint_BlankValue_ThrowsAtDatabase(string table, string column, string blankLiteral, string constraint)
    {
        // Mirrors AuthorCheckConstraint_*BlankStrings*: IsRequired() alone only enforces
        // NOT NULL, so raw SQL could otherwise persist a blank label/name.
        var id = Guid.CreateVersion7();
        var sql = $"INSERT INTO \"{table}\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"{column}\") VALUES ({{0}}, now(), now(), {blankLiteral})";
        await AssertViolatesAsync(constraint, () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(sql, id));
    }

    [Fact]
    public async Task PublisherCollectionCheckConstraint_BlankName_ThrowsAtDatabase()
    {
        var publisherId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Publishers\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Name\") VALUES ({0}, now(), now(), 'Casterman')", publisherId);

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_PublisherCollections_NameNotBlank", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"PublisherCollections\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Name\", \"PublisherId\") VALUES ({0}, now(), now(), '   ', {1})",
                id, publisherId));
    }

    [Theory]
    [InlineData("Title", "CK_Series_TitleNotBlank")]
    [InlineData("SortKey", "CK_Series_SortKeyNotBlank")]
    public async Task SeriesCheckConstraint_BlankTitleOrSortKey_ThrowsAtDatabase(string blankColumn, string constraint)
    {
        var id = Guid.CreateVersion7();
        var otherColumn = blankColumn == "Title" ? "SortKey" : "Title";
        var sql = $"INSERT INTO \"Series\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"{blankColumn}\", \"{otherColumn}\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"ExcludeFromReleaseEstimates\", \"NavigationEntry\") " +
                  $"VALUES ({{0}}, now(), now(), '   ', 'Valeur', false, false, false, false, 'V')";
        await AssertViolatesAsync(constraint, () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(sql, id));
    }

    [Fact]
    public async Task SeriesCheckConstraint_NonPositiveTheoreticalVolumeCount_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Series_TheoreticalVolumeCountPositive", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Series\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"ExcludeFromReleaseEstimates\", \"NavigationEntry\", \"TheoreticalVolumeCount\") " +
                "VALUES ({0}, now(), now(), 'Tintin', 'Tintin', false, false, false, false, 'T', 0)",
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
            "INSERT INTO \"Publishers\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Name\") VALUES ({0}, now(), now(), 'Casterman')", publisherId);
        var collectionId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"PublisherCollections\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Name\", \"PublisherId\") VALUES ({0}, now(), now(), 'Tintin', {1})",
            collectionId, publisherId);

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Series_TemplateCollectionRequiresPublisher", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Series\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"ExcludeFromReleaseEstimates\", \"NavigationEntry\", \"TemplatePublisherCollectionId\") " +
                "VALUES ({0}, now(), now(), 'Tintin', 'Tintin', false, false, false, false, 'T', {1})",
                id, collectionId));
    }

    // The navigation entry is derived in SQL from the sort key ({2}), so that it never breaks a
    // constraint of its own in the tests below: every sort key used starts with an ASCII letter.
    // The cast matches the type deduced for {2} from its SortKey column.
    private const string InsertAlbumSql =
        "INSERT INTO \"Albums\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"NavigationEntry\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\", " +
        "\"VolumeNumber\", \"StartVolumeNumber\", \"EndVolumeNumber\", \"FirstPublicationYear\", \"FirstPublicationMonth\", \"SeriesId\") " +
        "VALUES ({0}, now(), now(), {1}, {2}, UPPER(LEFT(CAST({2} AS character varying), 1)), false, {3}, false, {4}, {5}, {6}, {7}, {8}, {9})";

    [Fact]
    public async Task AlbumCheckConstraint_NoTitleNoSeries_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_TitleRequiredWithoutSeries", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, null!, null!, (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_BlankTitleWithSeries_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        // A sort key is supplied so that the blank title is the only rule broken: a NULL sort key
        // would also break CK_Albums_SortKeyPresenceMatchesTitle.
        await AssertViolatesAsync("CK_Albums_TitleNotBlank", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "   ", "Tintin", (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_SortKeyWithoutTitle_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_SortKeyPresenceMatchesTitle", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, null!, "Orphan Key", (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_TitleWithoutSortKey_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_SortKeyPresenceMatchesTitle", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", null!, (int)AlbumType.Regular,
                null!, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_ManualSortKeyWithoutTitle_ThrowsAtDatabase()
    {
        var seriesId = await InsertSeriesAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_ManualSortKeyRequiresTitle", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Albums\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\", \"SeriesId\") " +
                "VALUES ({0}, now(), now(), NULL, NULL, true, {1}, false, {2})", id, (int)AlbumType.Regular, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_NonPositiveVolumeNumber_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_VolumeNumberPositive", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                0, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeOnlyStart_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_VolumeRangeBothOrNeither", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Omnibus,
                null!, 1, null!, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeStartGreaterThanEnd_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_VolumeRangeOrder", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Omnibus,
                null!, 6, 1, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_VolumeRangeOnNonOmnibus_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_VolumeRangeOmnibusOnly", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                null!, 1, 6, null!, null!, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_PublicationMonthWithoutYear_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_PublicationMonthRequiresYear", () =>
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
        await AssertViolatesAsync("CK_Albums_PublicationMonthRange", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertAlbumSql, id, "Tintin", "Tintin", (int)AlbumType.Regular,
                null!, null!, null!, 1978, month, null!));
    }

    [Fact]
    public async Task AlbumCheckConstraint_PublicationYearNonPositive_ThrowsAtDatabase()
    {
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Albums_PublicationYearPositive", () =>
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

    [Theory]
    [InlineData("Series", "CK_Series_NavigationEntryValid")]
    [InlineData("Albums", "CK_Albums_NavigationEntryValid")]
    [InlineData("Authors", "CK_Authors_NavigationEntryValid")]
    public async Task NavigationEntryCheckConstraint_ValueOutsideEntries_ThrowsAtDatabase(string table, string constraint)
    {
        // Lowercase or accented letters are filed under their uppercase base letter: only
        // A–Z, # and @ are entries (fonctionnel.md § Entrées de la navigation par initiale).
        var sql = table switch
        {
            "Series" => "INSERT INTO \"Series\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"ExcludeFromReleaseEstimates\", \"NavigationEntry\") " +
                        "VALUES ({0}, now(), now(), 'Épervier', 'Épervier', false, false, false, false, 'é')",
            "Albums" => "INSERT INTO \"Albums\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"NavigationEntry\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\") " +
                        $"VALUES ({{0}}, now(), now(), 'Épervier', 'Épervier', 'é', false, {(int)AlbumType.Regular}, false)",
            _ => "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), 'Émile', 'Émile', 'é')",
        };

        await AssertViolatesAsync(constraint, () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(sql, Guid.CreateVersion7()));
    }

    [Fact]
    public async Task AlbumCheckConstraint_NavigationEntryWithoutSortKey_ThrowsAtDatabase()
    {
        // An album with no title takes its series' entry at read time: a stored entry without a
        // sort key would be a stale copy.
        var seriesId = await InsertSeriesAsync();

        await AssertViolatesAsync("CK_Albums_NavigationEntryPresenceMatchesSortKey", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Albums\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"NavigationEntry\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\", \"SeriesId\") " +
                "VALUES ({0}, now(), now(), 'T', false, {1}, false, {2})", Guid.CreateVersion7(), (int)AlbumType.Regular, seriesId));
    }

    [Fact]
    public async Task AlbumCheckConstraint_SortKeyWithoutNavigationEntry_ThrowsAtDatabase()
    {
        await AssertViolatesAsync("CK_Albums_NavigationEntryPresenceMatchesSortKey", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Albums\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\") " +
                "VALUES ({0}, now(), now(), 'Tintin', 'Tintin', false, {1}, false)", Guid.CreateVersion7(), (int)AlbumType.Regular));
    }

    [Fact]
    public async Task AuthorCheckConstraint_BlankSortKey_ThrowsAtDatabase()
    {
        await AssertViolatesAsync("CK_Authors_SortKeyNotBlank", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), 'Hergé', '   ', 'H')",
                Guid.CreateVersion7()));
    }

    private async Task<Guid> InsertSeriesAsync()
    {
        var seriesId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Series\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"IsManualSortKey\", \"IsComplete\", \"ExcludeFromMissingVolumes\", \"ExcludeFromReleaseEstimates\", \"NavigationEntry\") " +
            "VALUES ({0}, now(), now(), 'Tintin', 'Tintin', false, false, false, false, 'T')", seriesId);
        return seriesId;
    }

    private async Task<Guid> InsertAlbumAsync()
    {
        var albumId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Albums\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Title\", \"SortKey\", \"NavigationEntry\", \"IsManualSortKey\", \"Type\", \"IsSpecialIssue\") " +
            "VALUES ({0}, now(), now(), 'Tintin', 'Tintin', 'T', false, {1}, false)", albumId, (int)AlbumType.Regular);
        return albumId;
    }

    private async Task<Guid> InsertAuthorAsync()
    {
        var authorId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Pseudonym\", \"SortKey\", \"NavigationEntry\") VALUES ({0}, now(), now(), 'Hergé', 'Hergé', 'H')", authorId);
        return authorId;
    }

    private const string InsertContributionSql =
        "INSERT INTO \"Contributions\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"AlbumId\", \"SeriesId\", \"AuthorId\", \"Role\") " +
        "VALUES ({0}, now(), now(), {1}, {2}, {3}, {4})";

    [Fact]
    public async Task ContributionCheckConstraint_NeitherAlbumNorSeries_ThrowsAtDatabase()
    {
        var authorId = await InsertAuthorAsync();
        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Contributions_ExactlyOneOfAlbumOrSeries", () =>
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
        await AssertViolatesAsync("CK_Contributions_ExactlyOneOfAlbumOrSeries", () =>
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

        await AssertViolatesAsync("IX_Contributions_AlbumId_Role_AuthorId", () =>
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

        await AssertViolatesAsync("IX_Contributions_SeriesId_Role_AuthorId", () =>
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
            "INSERT INTO \"Publishers\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"Name\") VALUES ({0}, now(), now(), {1})", publisherId, name);
        return publisherId;
    }

    private const string InsertEditionSql =
        "INSERT INTO \"Editions\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"AlbumId\", \"PublisherId\", \"IsDedicated\", \"IsColor\", \"IsSecondHand\", \"IsFree\", " +
        "\"PublicationYear\", \"PageCount\", \"AcquisitionMode\", \"AcquisitionDate\", \"AcquisitionAmount\", \"AcquisitionCurrency\") " +
        "VALUES ({0}, now(), now(), {1}, {2}, false, true, false, {3}, {4}, {5}, {6}, {7}, {8}, {9})";

    [Fact]
    public async Task EditionCheckConstraint_NonPositivePublicationYear_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_PublicationYearPositive", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, 0, null!, null!, null!, null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_NonPositivePageCount_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_PageCountPositive", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, 0, null!, null!, null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_AmountWithoutCurrency_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_AcquisitionAmountCurrencyTogether", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, (int)AcquisitionMode.Purchase, null!, 10, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_NonPositiveAmount_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_AcquisitionAmountPositive", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, (int)AcquisitionMode.Purchase, null!, 0, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_DateWithoutAcquisitionMode_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_AcquisitionModeRequiredForDateOrAmounts", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, null!, new DateOnly(2020, 1, 1), null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_AmountWithoutAcquisitionMode_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_AcquisitionModeRequiredForDateOrAmounts", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, false, null!, null!, null!, null!, 10, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_FreeWithAmount_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_Editions_FreeRequiresNoAmount", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionSql, id, albumId, publisherId, true, null!, null!, (int)AcquisitionMode.Gift, null!, 10, "EUR"));
    }

    // Dated by its publication year, so that only the constraint under test can fail.
    private const string InsertEditionWithInitialValueSql =
        "INSERT INTO \"Editions\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"AlbumId\", \"PublisherId\", \"IsDedicated\", \"IsColor\", " +
        "\"IsSecondHand\", \"IsFree\", \"PublicationYear\", \"AcquisitionMode\", \"InitialValueAmount\", \"InitialValueCurrency\") " +
        "VALUES ({0}, now(), now(), {1}, {2}, false, true, false, {3}, 2000, {4}, {5}, {6})";

    [Theory]
    [InlineData(10, null, "CK_Editions_InitialValueAmountCurrencyTogether")]
    [InlineData(null, "EUR", "CK_Editions_InitialValueAmountCurrencyTogether")]
    [InlineData(0, "EUR", "CK_Editions_InitialValueAmountPositive")]
    public async Task EditionCheckConstraint_InvalidInitialValue_ThrowsAtDatabase(int? amount, string? currency, string constraint)
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        await AssertViolatesAsync(constraint, () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionWithInitialValueSql, Guid.CreateVersion7(), albumId, publisherId, false,
                (int)AcquisitionMode.Purchase, amount!, currency!));
    }

    [Fact]
    public async Task EditionCheckConstraint_InitialValueWithoutAcquisitionMode_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        await AssertViolatesAsync("CK_Editions_AcquisitionModeRequiredForDateOrAmounts", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionWithInitialValueSql, Guid.CreateVersion7(), albumId, publisherId, false, null!, 10, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_FreeWithInitialValue_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        await AssertViolatesAsync("CK_Editions_FreeRequiresNoAmount", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionWithInitialValueSql, Guid.CreateVersion7(), albumId, publisherId, true,
                (int)AcquisitionMode.Gift, 10, "EUR"));
    }

    [Fact]
    public async Task EditionCheckConstraint_FreePurchase_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();

        await AssertViolatesAsync("CK_Editions_PurchaseNotFree", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionWithInitialValueSql, Guid.CreateVersion7(), albumId, publisherId, true,
                (int)AcquisitionMode.Purchase, null!, null!));
    }

    [Fact]
    public async Task EditionCheckConstraint_ValidInitialValue_Succeeds()
    {
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();
        var id = Guid.CreateVersion7();

        await _fixture.Context.Database.ExecuteSqlRawAsync(
            InsertEditionWithInitialValueSql, id, albumId, publisherId, false, (int)AcquisitionMode.Gift, 10, "QZF");

        Assert.Equal(1, await _fixture.Context.Editions.CountAsync(e => e.Id == id));
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

    private async Task<Guid> InsertEditionAsync() =>
        await InsertEditionAsync(await InsertAlbumAsync(), await InsertPublisherAsync());

    private async Task<Guid> InsertEditionAsync(Guid albumId, Guid publisherId)
    {
        var editionId = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Editions\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"AlbumId\", \"PublisherId\", \"IsDedicated\", \"IsColor\", \"IsSecondHand\", \"IsFree\") " +
            "VALUES ({0}, now(), now(), {1}, {2}, false, true, false, false)", editionId, albumId, publisherId);
        return editionId;
    }

    private const string InsertEditionVisualSql =
        "INSERT INTO \"EditionVisuals\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"EditionId\", \"Type\", \"MediaReference\", \"DisplayOrder\") " +
        "VALUES ({0}, now(), now(), {1}, {2}, {3}, {4})";

    [Fact]
    public async Task EditionVisualCheckConstraint_BlankMediaReference_ThrowsAtDatabase()
    {
        var editionId = await InsertEditionAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_EditionVisuals_MediaReferenceNotBlank", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                InsertEditionVisualSql, id, editionId, (int)VisualType.Cover, "   ", 0));
    }

    [Fact]
    public async Task EditionVisualCheckConstraint_NegativeDisplayOrder_ThrowsAtDatabase()
    {
        var editionId = await InsertEditionAsync();

        var id = Guid.CreateVersion7();
        await AssertViolatesAsync("CK_EditionVisuals_DisplayOrderNotNegative", () =>
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

    private const string InsertPurchaseIntentSql =
        "INSERT INTO \"PurchaseIntents\" (\"Id\", \"CreatedAt\", \"ModifiedAt\", \"AlbumId\", \"EditionId\") VALUES ({0}, now(), now(), {1}, {2})";

    [Fact]
    public async Task PurchaseIntentConstraint_NullAlbum_ThrowsAtDatabase()
    {
        // AlbumId is set for both kinds of intent (see PurchaseIntent remarks).
        var editionId = await InsertEditionAsync();
        await AssertViolatesNotNullAsync("AlbumId", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), null!, editionId));
    }

    [Fact]
    public async Task PurchaseIntentUniqueIndex_DuplicateWholeAlbum_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        await _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), albumId, null!);

        await AssertViolatesAsync("IX_PurchaseIntents_AlbumId_WholeAlbum", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), albumId, null!));
    }

    [Fact]
    public async Task PurchaseIntentUniqueIndex_DuplicateEdition_ThrowsAtDatabase()
    {
        var albumId = await InsertAlbumAsync();
        var editionId = await InsertEditionAsync(albumId, await InsertPublisherAsync());
        await _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), albumId, editionId);

        await AssertViolatesAsync("IX_PurchaseIntents_EditionId", () =>
            _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), albumId, editionId));
    }

    [Fact]
    public async Task PurchaseIntentUniqueIndex_DistinctEditionsOfSameAlbum_Succeeds()
    {
        // The whole-album unique index is filtered on EditionId IS NULL: it must not stop
        // several editions of the same album from each carrying their own intent.
        var albumId = await InsertAlbumAsync();
        var publisherId = await InsertPublisherAsync();
        var firstEditionId = await InsertEditionAsync(albumId, publisherId);
        var secondEditionId = await InsertEditionAsync(albumId, publisherId);

        await _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), albumId, firstEditionId);
        await _fixture.Context.Database.ExecuteSqlRawAsync(InsertPurchaseIntentSql, Guid.CreateVersion7(), albumId, secondEditionId);

        var count = await _fixture.Context.PurchaseIntents.CountAsync(p => p.AlbumId == albumId);
        Assert.Equal(2, count);
    }
}
