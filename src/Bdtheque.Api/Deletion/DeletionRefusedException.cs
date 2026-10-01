using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Deletion;

/// <summary>
/// A deletion refused because other records still reference the deleted one (fonctionnel.md §
/// Suppression des entités): a business error (<see cref="DomainRules.DeletionBlockedByReferences"/>),
/// carrying the impact that names the blocking records for the message shown to the user.
/// </summary>
internal sealed class DeletionRefusedException(DeletionImpact impact)
    : Exception("The deletion is blocked by records referencing the deleted one.")
{
    public DeletionImpact Impact { get; } = impact;
}
