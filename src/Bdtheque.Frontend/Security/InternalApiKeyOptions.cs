using System.ComponentModel.DataAnnotations;

namespace Bdtheque.Frontend.Security;

/// <summary>
/// Secret partagé avec `api` (cf. contraintes-techniques.md, section Authentification).
/// Fourni via la variable d'environnement <c>Bdtheque__InternalApiKey__Key</c>.
/// </summary>
public sealed class InternalApiKeyOptions
{
    public const string SectionName = "InternalApiKey";

    public const string HeaderName = "X-Internal-Api-Key";

    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;
}
