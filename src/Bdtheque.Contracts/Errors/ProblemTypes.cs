namespace Bdtheque.Contracts.Errors;

/// <summary>
/// Values of the <c>type</c> member of every error response of the API (ProblemDetails, RFC 9457):
/// one per error category of fonctionnel.md § Présentation des erreurs, and no other. The category
/// is all the frontend needs to present an error distinctively; the HTTP status only refines the
/// nature of the error within its category.
/// </summary>
public static class ProblemTypes
{
    /// <summary>The request breaks a business rule; the user can fix their input (HTTP 422).</summary>
    public const string Business = "urn:bdtheque:problem:business";

    /// <summary>
    /// The request breaks no rule but cannot succeed given the current state of the data: modified
    /// (HTTP 409) or deleted (HTTP 404) in the meantime.
    /// </summary>
    public const string Functional = "urn:bdtheque:problem:functional";

    /// <summary>The application could not process the request, whatever the user's input.</summary>
    public const string Technical = "urn:bdtheque:problem:technical";

    /// <summary>
    /// Extension member of a <see cref="Business"/> problem holding the code of the broken rule,
    /// from which the frontend's localization produces the user-facing text.
    /// </summary>
    public const string RuleCodeExtension = "ruleCode";

    /// <summary>
    /// Extension member of the problem of a refused deletion (<see cref="Business"/>) or of one whose
    /// impact changed since it was confirmed (<see cref="Functional"/>), holding the current impact of
    /// the deletion (<see cref="Deletion.DeletionImpact"/>).
    /// </summary>
    public const string ImpactExtension = "impact";
}
