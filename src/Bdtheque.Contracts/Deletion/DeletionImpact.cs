namespace Bdtheque.Contracts.Deletion;

/// <summary>
/// What deleting a record would do (fonctionnel.md § Suppression des entités): the number of
/// records concerned, by entity type and by nature of link — everything the confirmation message,
/// or the error of a refused deletion, has to announce.
/// </summary>
/// <param name="BlockedBy">Records referencing the deleted one: the deletion is refused while any exists.</param>
/// <param name="AssociationsRemoved">Records that lose their association with the deleted one.</param>
/// <param name="DeletedWith">Records deleted along with it.</param>
/// <param name="Fingerprint">
/// Identifies the exact records concerned, not only their counts. Sent back with the deletion, which
/// is refused if the impact has changed since the user confirmed it.
/// </param>
public sealed record DeletionImpact(
    IReadOnlyList<ImpactCount> BlockedBy,
    IReadOnlyList<ImpactCount> AssociationsRemoved,
    IReadOnlyList<ImpactCount> DeletedWith,
    string Fingerprint);

/// <summary>Number of records of one entity type concerned by a deletion.</summary>
public sealed record ImpactCount(EntityKind Kind, int Count);
