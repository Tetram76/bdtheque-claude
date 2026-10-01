namespace Bdtheque.Infrastructure;

/// <summary>
/// The requested entity does not exist: an unknown identifier, or a record deleted in the meantime
/// (from another tab or device). Not a business rule violation — the request cannot succeed given
/// the current state of the data (fonctionnel.md § Présentation des erreurs, functional error).
/// </summary>
public sealed class EntityNotFoundException(Type entityType, Guid id)
    : Exception($"No {entityType.Name} with id {id} exists.")
{
    public Type EntityType { get; } = entityType;

    public Guid Id { get; } = id;
}
