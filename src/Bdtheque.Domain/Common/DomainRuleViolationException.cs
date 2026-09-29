namespace Bdtheque.Domain.Common;

/// <summary>
/// Signals that a requested change breaks a business rule of the domain — something the user
/// caused and can fix by correcting their input — as opposed to a technical error (a bug, an
/// unavailable service…). fonctionnel.md § Présentation des erreurs requires the two to be told
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
    /// Stable code of the broken rule, one of <see cref="DomainRules"/>. User-facing text is
    /// produced from it by the frontend's localization (gestion-projet.md: no user text is
    /// hard-coded); <see cref="Exception.Message"/> is for logs only.
    /// </summary>
    public string Rule { get; } = rule;
}
