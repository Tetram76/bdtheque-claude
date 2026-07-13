using System.ComponentModel.DataAnnotations;

namespace Bdtheque.Api.Security;

/// <summary>
/// Secret partagé entre `frontend` et `api` (cf. contraintes-techniques.md, section
/// Authentification). Fourni via la variable d'environnement <c>Bdtheque__InternalApiKey__Key</c>.
/// </summary>
public sealed class InternalApiKeyOptions
{
    public const string SectionName = "InternalApiKey";

    public const string HeaderName = "X-Internal-Api-Key";

    [Required(AllowEmptyStrings = false)]
    public string Key { get; set; } = string.Empty;
}
