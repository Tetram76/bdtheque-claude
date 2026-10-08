using Bdtheque.Infrastructure;
using Bdtheque.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Provides a <see cref="BdthequeDbContext"/> on its own freshly migrated PostgreSQL database
/// (see <see cref="PostgreSqlTestServer"/>).
/// </summary>
public sealed class BdthequeDbContextFixture : IAsyncLifetime
{
    private string _connectionString = null!;

    public BdthequeDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _connectionString = await PostgreSqlTestServer.CreateMigratedDatabaseAsync();
        var options = new DbContextOptionsBuilder<BdthequeDbContext>().UseNpgsql(_connectionString);
        StrictQueryWarnings.Apply(options);

        Context = new BdthequeDbContext(options.Options);
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        PostgreSqlTestServer.ReleaseConnections(_connectionString);
    }
}
