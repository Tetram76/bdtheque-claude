using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The design-time factory builds a context on the provider of <see cref="BdthequeDbContext"/>, without
/// any connection string: the tooling it serves never connects to a database, and credentials written
/// here would serve nothing.
/// </summary>
public sealed class BdthequeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_UsesNpgsqlWithoutConnectionString()
    {
        using var context = new BdthequeDbContextFactory().CreateDbContext([]);

        Assert.True(context.Database.IsNpgsql());
        Assert.Null(context.Database.GetConnectionString());
    }
}
