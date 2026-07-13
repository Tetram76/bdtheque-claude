using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Replaces PostgreSQL with an in-memory SQLite database, to test the application wiring
/// (DI, middlewares, health checks) without depending on a `db` container.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string InternalApiKey = "test-internal-api-key";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalApiKey:Key"] = InternalApiKey,
            }));

        builder.ConfigureServices(services =>
            services.AddDbContext<BdthequeDbContext>(options => options.UseSqlite(_connection)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
