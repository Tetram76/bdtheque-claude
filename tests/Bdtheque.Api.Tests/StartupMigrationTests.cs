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
        // (contraintes-techniques.md § Déploiement): nothing else provisions it.
        _ = _factory.Server;

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Database;

        Assert.Empty(await database.GetPendingMigrationsAsync());
        Assert.Equal(database.GetMigrations(), await database.GetAppliedMigrationsAsync());
    }
}
