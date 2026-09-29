namespace Bdtheque.Domain.Common;

/// <summary>Normalization of user-entered text shared by every entity.</summary>
internal static class DomainText
{
    /// <summary>Trims the value; a blank one means "not provided" and is stored as <see langword="null"/>.</summary>
    public static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Trims a mandatory value, rejecting a blank one as a violation of <paramref name="rule"/>.</summary>
    public static string Required(string? value, string rule, string message) =>
        NullIfBlank(value) ?? throw new DomainRuleViolationException(rule, message);
}
