using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Display culture, a setting of the application (fonctionnel/presentation.md § Langue et culture d'affichage):
/// French of France, the only one offered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DisplayCulture>))]
public enum DisplayCulture
{
    FrenchFrance = 1,
}
