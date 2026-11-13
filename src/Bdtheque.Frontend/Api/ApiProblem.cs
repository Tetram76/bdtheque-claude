using Bdtheque.Contracts.Deletion;

namespace Bdtheque.Frontend.Api;

/// <summary>The three categories of errors the user tells apart at first glance (fonctionnel/presentation-des-erreurs.md).</summary>
public enum ProblemCategory
{
    Business,
    Functional,
    Technical,
}

/// <summary>
/// An error the API reported, read from its ProblemDetails response
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md): what its presentation needs,
/// and nothing internal.
/// </summary>
/// <param name="Status">HTTP status, which refines the nature of the error within its category (e.g. 409 or 404).</param>
/// <param name="RuleCode">Code of the broken rule of a business error, from which its text is produced.</param>
/// <param name="Impact">Records concerned by a refused deletion, or by one whose impact changed.</param>
public sealed record ApiProblem(ProblemCategory Category, int? Status, string? RuleCode, DeletionImpact? Impact)
{
    /// <summary>
    /// The fields of a business error, where the request entered the value in cause: JSON Pointers into the content it sent
    /// (e.g. <c>/collections/1/name</c>), the first one to correct first; none for a rule that concerns no value of its own.
    /// </summary>
    public IReadOnlyList<string> Fields { get; init; } = [];

    // Two problems are the same error when they designate the same fields, not the same list.
    public bool Equals(ApiProblem? other) =>
        other is not null
        && (Category, Status, RuleCode, Impact) == (other.Category, other.Status, other.RuleCode, other.Impact)
        && Fields.SequenceEqual(other.Fields);

    public override int GetHashCode() => HashCode.Combine(Category, Status, RuleCode, Impact, Fields.Count);

    public static ApiProblem Technical(int? status = null) => new(ProblemCategory.Technical, status, null, null);
}

/// <summary>A call to the API that did not succeed: the error to present is its <see cref="Problem"/>.</summary>
public sealed class ApiProblemException(ApiProblem problem, Exception? innerException = null)
    : Exception($"The API call failed: {problem.Category} error (status {problem.Status}, rule {problem.RuleCode}).", innerException)
{
    public ApiProblem Problem { get; } = problem;
}
