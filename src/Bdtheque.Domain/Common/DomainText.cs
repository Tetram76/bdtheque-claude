using System.Globalization;

namespace Bdtheque.Domain.Common;

/// <summary>Normalization of user-entered text shared by every entity.</summary>
internal static class DomainText
{
    /// <summary>The order of the display culture, French (fonctionnel/presentation/langue-et-culture-d-affichage.md).</summary>
    // Stryker disable once String : ICU orders and compares letters alike in French and in its root collation, case and accents ignored (checked on the Latin, Greek and Cyrillic letters)
    public static readonly CompareInfo FrenchOrder = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;

    /// <summary>Trims the value; a blank one means "not provided" and is stored as <see langword="null"/>.</summary>
    public static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Trims a mandatory value that is not entered by the user (e.g. the file of a visual), rejecting a blank one as a
    /// violation of <paramref name="rule"/>.
    /// </summary>
    public static string Required(string? value, string rule, string message) =>
        NullIfBlank(value) ?? throw new DomainRuleViolationException(rule, message);

    /// <summary>
    /// Trims a mandatory value, rejecting a blank one as a violation of <paramref name="rule"/>, which concerns
    /// <paramref name="attribute"/> of <paramref name="record"/>.
    /// </summary>
    public static string Required(string? value, string rule, string message, object record, string attribute) =>
        NullIfBlank(value) ?? throw new DomainRuleViolationException(rule, message, record, attribute);
}
