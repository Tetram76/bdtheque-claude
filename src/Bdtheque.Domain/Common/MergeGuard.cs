using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Common;

/// <summary>
/// The records a merge transfers from one record to the other (fonctionnel/regles-metier/fusion-des-fiches.md): two
/// distinct records of the same type, the client choosing them in the list of their type.
/// </summary>
internal static class MergeGuard
{
    /// <exception cref="ArgumentException">The same record twice: a record is never merged with itself.</exception>
    public static void EnsureDistinct(EntityBase merged, EntityBase kept)
    {
        if (merged.Id == kept.Id)
            throw new ArgumentException($"The record {kept.Id} cannot be merged with itself.", nameof(kept));
    }
}
