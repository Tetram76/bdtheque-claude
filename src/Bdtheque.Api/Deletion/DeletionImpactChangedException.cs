using Bdtheque.Contracts.Deletion;

namespace Bdtheque.Api.Deletion;

/// <summary>
/// The impact of a deletion changed since the user confirmed it (a record concerned appeared,
/// disappeared or was replaced, from another tab or device): a functional error carrying the new
/// impact, to be confirmed again. A deletion never does more than what was confirmed.
/// </summary>
internal sealed class DeletionImpactChangedException(DeletionImpact impact)
    : Exception("The impact of the deletion changed since it was confirmed.")
{
    public DeletionImpact Impact { get; } = impact;
}
