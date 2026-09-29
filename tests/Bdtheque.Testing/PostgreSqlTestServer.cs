using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bdtheque.Testing;

/// <summary>
/// Runs every persistence test against the same PostgreSQL image as production, so that what
/// is tested is what ships: the real migrations (not <c>EnsureCreated</c>), and PostgreSQL's own
/// collations, numeric precision and constraint enforcement — none of which another engine can
/// stand in for (see gestion-projet.md § Outillage .NET).
/// </summary>
/// <remarks>
/// One container is started per test process, then the migrations are applied once to a
/// template database. Each test database is a clone of that template (<c>CREATE DATABASE …
/// TEMPLATE</c>): isolated per test, yet far cheaper than replaying every migration each time.
/// The container is removed by Testcontainers' resource reaper when the test process exits.
/// </remarks>
public static class PostgreSqlTestServer
{
    /// <summary>
    /// Must match the <c>db</c> service image in docker-compose.yml — pinned by
    /// <c>PostgreSqlTestServerTests.Image_MatchesDockerComposeDbService</c>.
    /// </summary>
    public const string Image = "postgres:17-alpine";

    private const string TemplateDatabase = "bdtheque_template";

    private static readonly Lazy<Task<PostgreSqlContainer>> Container = new(StartAsync);

    /// <summary>Creates a fresh database holding the fully migrated schema.</summary>
    public static Task<string> CreateMigratedDatabaseAsync() => CreateDatabaseAsync(TemplateDatabase);

    /// <summary>
    /// Creates a fresh, schema-less database — the state of a first deployment, before the
    /// <c>api</c> container applies the migrations at startup.
    /// </summary>
    public static Task<string> CreateEmptyDatabaseAsync() => CreateDatabaseAsync("template0");

    private static async Task<string> CreateDatabaseAsync(string template)
    {
        var container = await Container.Value;
        var database = $"test_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\" TEMPLATE \"{template}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        return ConnectionStringFor(container, database);
    }

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        var container = new PostgreSqlBuilder(Image)
            .WithDatabase("postgres")
            .Build();
        await container.StartAsync();

        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{TemplateDatabase}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        // Pooling is disabled for the template: PostgreSQL refuses to clone a database while any
        // session is still connected to it, and a pooled connection would stay open after this.
        var templateConnectionString = new NpgsqlConnectionStringBuilder(ConnectionStringFor(container, TemplateDatabase))
        {
            Pooling = false,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<BdthequeDbContext>().UseNpgsql(templateConnectionString).Options;
        await using (var context = new BdthequeDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        return container;
    }

    private static string ConnectionStringFor(PostgreSqlContainer container, string database) =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;
}
