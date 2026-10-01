namespace Bdtheque.Domain.Entities.Common;

/// <summary>
/// Marks the root of an aggregate: the entity through which every write on the aggregate goes,
/// and whose version stands for the whole aggregate (see choix-implementation.md § Concurrence
/// d'accès). Its children — editions, visuals, contributions, purchase intents, publisher
/// collections — carry no version of their own.
/// </summary>
public interface IAggregateRoot;
