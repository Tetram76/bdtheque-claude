using Bdtheque.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Provides a shared, in-memory SQLite <see cref="BdthequeDbContext"/> for integration tests.
/// Uses a kept-alive <see cref="SqliteConnection"/> so that the schema persists for the
/// lifetime of the fixture, matching the pattern used in <c>Bdtheque.Api.Tests</c>.
/// </summary>
public sealed class BdthequeDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public BdthequeDbContext Context { get; }

    public BdthequeDbContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BdthequeDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new BdthequeDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
