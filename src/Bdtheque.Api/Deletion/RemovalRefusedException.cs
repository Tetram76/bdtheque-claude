using Bdtheque.Contracts.Deletion;
using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Deletion;

/// <summary>
/// The removal of a record by the form of another, refused as its deletion would be, because other records still reference it
/// (e.g. a collection removed from the form of its publisher): a business error of its own rule, carrying the impact that names
/// the blocking records, and the record, which the content of the request no longer cites
/// (choix-implementation/ecritures/suppression-des-entites-mise-en-oeuvre.md).
/// </summary>
/// <param name="rule">The rule broken, one of <see cref="DomainRules"/>.</param>
/// <param name="recordId">The record whose removal is refused.</param>
internal sealed class RemovalRefusedException(string rule, Guid recordId, DeletionImpact impact)
    : Exception($"Record {recordId} is referenced and cannot be removed.")
{
    public string Rule { get; } = rule;

    public Guid RecordId { get; } = recordId;

    public DeletionImpact Impact { get; } = impact;
}
