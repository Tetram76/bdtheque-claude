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
    public BdthequeDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await PostgreSqlTestServer.CreateMigratedDatabaseAsync();
        var options = new DbContextOptionsBuilder<BdthequeDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Context = new BdthequeDbContext(options);
    }

    public async Task DisposeAsync() => await Context.DisposeAsync();
}
