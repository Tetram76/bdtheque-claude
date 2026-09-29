using System.ComponentModel.DataAnnotations;

namespace Bdtheque.Frontend.Security;

/// <summary>
/// Secret shared with `api` (see contraintes-techniques.md, Authentification section).
/// Supplied via the <c>InternalApiKey__Key</c> environment variable.
/// </summary>
public sealed class InternalApiKeyOptions
{
    public const string SectionName = "InternalApiKey";

    public const string HeaderName = "X-Internal-Api-Key";

    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;
}
