using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Point d'entrée EF Core vers PostgreSQL. Ne contient volontairement aucun
/// <see cref="DbSet{TEntity}"/> à ce stade : le modèle métier (entités, configurations,
/// migrations) est un chantier fonctionnel distinct de la mise en place de l'architecture.
/// </summary>
public sealed class BdthequeDbContext(DbContextOptions<BdthequeDbContext> options) : DbContext(options)
{
}
