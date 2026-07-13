using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Lets design-time tooling (`dotnet ef migrations`) build the <see cref="BdthequeDbContext"/>
/// without depending on the full API startup. The connection string used here only serves
/// migration generation; the real runtime uses the one configured in the `api` container.
/// </summary>
public sealed class BdthequeDbContextFactory : IDesignTimeDbContextFactory<BdthequeDbContext>
{
    public BdthequeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BdthequeDbContext>()
            .UseNpgsql("Host=localhost;Database=bdtheque;Username=bdtheque;Password=bdtheque");

        return new BdthequeDbContext(optionsBuilder.Options);
    }
}
