using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

public sealed class StartupMigrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public StartupMigrationTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Startup_OnEmptyDatabase_AppliesEveryMigration()
    {
        // A fresh deployment must get its schema from the api container's own startup
        // (choix-implementation.md § Application du schéma au démarrage): nothing else provisions it.
        _ = _factory.Server;

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Database;

        Assert.Empty(await database.GetPendingMigrationsAsync());
        Assert.Equal(database.GetMigrations(), await database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Startup_AfterAChangeOfIcu_RebuildsTheIndexesOfTheCollations()
    {
        // An update of the database image may bring another version of ICU, which PostgreSQL only
        // warns about: the api container's startup is the only step every deployment goes through.
        _ = _factory.Server;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Database.ExecuteSqlAsync(
                $"UPDATE pg_collation SET collversion = '0.0' WHERE collname = {BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation}");
        }

        await using var restarted = _factory.WithWebHostBuilder(_ => { });
        _ = restarted.Server;

        await using var check = restarted.Services.CreateAsyncScope();
        Assert.Empty(await CollationVersions.RefreshAsync(check.ServiceProvider.GetRequiredService<BdthequeDbContext>(), CancellationToken.None));
    }
}
