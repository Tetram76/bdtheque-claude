using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Permet aux outils design-time (`dotnet ef migrations`) de construire le
/// <see cref="BdthequeDbContext"/> sans dépendre du démarrage complet de l'API.
/// La chaîne de connexion utilisée ici ne sert qu'à la génération des migrations ;
/// l'exécution réelle utilise celle configurée dans le conteneur `api`.
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
