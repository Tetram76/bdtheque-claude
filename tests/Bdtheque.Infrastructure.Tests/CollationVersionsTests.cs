using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The text columns sort and compare along ICU collations, whose definition changes with the version
/// of ICU the database image ships: an index built under another version may be corrupt
/// (PostgreSQL documentation, ALTER COLLATION).
/// </summary>
public sealed class CollationVersionsTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Refresh_WithCurrentVersions_DoesNothing()
    {
        var refreshed = await CollationVersions.RefreshAsync(_fixture.Context, CancellationToken.None);

        Assert.Empty(refreshed);
    }

    [Theory]
    [InlineData(BdthequeDbContext.FrenchCollation)]
    [InlineData(BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation)]
    public async Task Refresh_AfterAChangeOfIcu_RebuildsTheIndexesAndRecordsTheCurrentVersion(string collation)
    {
        // What an image bringing another version of ICU leaves behind: the version recorded when the
        // collation was created no longer matches the one the system provides.
        await _fixture.Context.Database.ExecuteSqlAsync($"UPDATE pg_collation SET collversion = '0.0' WHERE collname = {collation}");
        var storageBefore = await GenreLabelIndexStorageAsync();

        var refreshed = await CollationVersions.RefreshAsync(_fixture.Context, CancellationToken.None);

        Assert.Equal([collation], refreshed);
        // Rebuilt: a reindexed index is written to new storage.
        Assert.NotEqual(storageBefore, await GenreLabelIndexStorageAsync());
        Assert.Empty(await CollationVersions.RefreshAsync(_fixture.Context, CancellationToken.None));
    }

    private Task<long> GenreLabelIndexStorageAsync() =>
        _fixture.Context.Database
            .SqlQuery<long>($"SELECT relfilenode::bigint AS \"Value\" FROM pg_class WHERE relname = 'IX_Genres_Label'")
            .SingleAsync();
}
