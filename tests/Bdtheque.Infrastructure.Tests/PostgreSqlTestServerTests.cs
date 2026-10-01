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
        var compose = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docker-compose.yml"));

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

        Assert.Equal(0, await CountOtherSessionsAsync(connectionString));
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bdtheque.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Bdtheque.slnx) not found.");
    }

    [GeneratedRegex(@"^  db:\s*\n    image:\s*(?<image>\S+)", RegexOptions.Multiline)]
    private static partial Regex DbServiceImage();
}
