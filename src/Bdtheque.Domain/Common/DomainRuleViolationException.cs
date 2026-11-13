namespace Bdtheque.Domain.Common;

/// <summary>
/// Signals that a requested change breaks a business rule of the domain — something the user
/// caused and can fix by correcting their input — as opposed to a technical error (a bug, an
/// unavailable service…). fonctionnel/presentation-des-erreurs.md requires the two to be told
/// apart at a glance, which only a dedicated type makes possible: the .NET argument and
/// operation exceptions are also thrown by the framework and by programming mistakes.
/// </summary>
/// <remarks>
/// Programming errors stay standard .NET exceptions (e.g. a required reference passed as
/// <see langword="null"/>, an undefined enum value): no user input can cause them.
/// </remarks>
public sealed class DomainRuleViolationException(string rule, string message) : Exception(message)
{
    /// <summary>
    /// A violation of <paramref name="rule"/> that concerns <paramref name="attribute"/> of <paramref name="record"/>: the
    /// value the user is to correct, which the error designates to whoever entered it
    /// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
    /// </summary>
    /// <param name="record">The record whose value breaks the rule, possibly the one being created.</param>
    /// <param name="attribute">The name of its property that holds the value (e.g. <c>nameof(Album.Title)</c>).</param>
    public DomainRuleViolationException(string rule, string message, object record, string attribute)
        : this(rule, message)
    {
        Record = record;
        Attribute = attribute;
    }

    /// <summary>
    /// Stable code of the broken rule, one of <see cref="DomainRules"/>. User-facing text is
    /// produced from it by the frontend's localization (gestion-projet/gouvernance/qualite-du-code.md: no user text is
    /// hard-coded); <see cref="Exception.Message"/> is for logs only.
    /// </summary>
    public string Rule { get; } = rule;

    /// <summary>The record whose value breaks the rule; none for a rule that concerns no value of its own.</summary>
    public object? Record { get; }

    /// <summary>The property of <see cref="Record"/> holding the value in cause.</summary>
    public string? Attribute { get; }
}
