using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Lets design-time tooling (`dotnet ef migrations`) build the <see cref="BdthequeDbContext"/>
/// without depending on the full API startup. Generating a migration or comparing the model with
/// them never connects to a database: no connection string, hence no credentials, is given here.
/// The provider comes from <see cref="BdthequeDbContext"/> itself; the schema is applied by the
/// `api` container at startup, with the connection string of its deployment.
/// </summary>
public sealed class BdthequeDbContextFactory : IDesignTimeDbContextFactory<BdthequeDbContext>
{
    public BdthequeDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BdthequeDbContext>().Options, TimeProvider.System);
}
