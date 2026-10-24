using System.Text.RegularExpressions;
using Bdtheque.Testing;
using Npgsql;

namespace Bdtheque.Infrastructure.Tests;

public sealed partial class PostgreSqlTestServerTests
{
    [Fact]
    public void Image_MatchesDockerComposeDbService()
    {
        // The persistence tests only vouch for the engine they run on: an image bump in
        // docker-compose.yml that the tests don't follow would leave production untested.
        var compose = RepositoryFiles.Read("docker-compose.yml");

        var composeImage = DbServiceImage().Match(compose);

        Assert.True(composeImage.Success, "No 'image:' line found for the db service in docker-compose.yml.");
        Assert.Equal(PostgreSqlTestServer.Image, composeImage.Groups["image"].Value);
    }

    [Fact]
    public async Task ReleaseConnections_ClosesThePooledConnectionsOfTheDatabase()
    {
        var connectionString = await PostgreSqlTestServer.CreateMigratedDatabaseAsync();
        await using (var pooled = new NpgsqlConnection(connectionString))
            await pooled.OpenAsync();
        Assert.Equal(1, await CountOtherSessionsAsync(connectionString));

        PostgreSqlTestServer.ReleaseConnections(connectionString);

        Assert.Equal(0, await WaitForNoOtherSessionAsync(connectionString));
    }

    // ClearPool returns once the client has closed its connections; each server session only leaves
    // pg_stat_activity when its backend has processed the disconnection and exited, a moment later.
    private static async Task<long> WaitForNoOtherSessionAsync(string connectionString)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        long count;
        while ((count = await CountOtherSessionsAsync(connectionString)) != 0 && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        return count;
    }

    // Counted from an unpooled session, which is excluded from the count.
    private static async Task<long> CountOtherSessionsAsync(string connectionString)
    {
        var unpooled = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;
        await using var connection = new NpgsqlConnection(unpooled);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid()", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [GeneratedRegex(@"^  db:\s*\n    image:\s*(?<image>\S+)", RegexOptions.Multiline)]
    private static partial Regex DbServiceImage();
}
