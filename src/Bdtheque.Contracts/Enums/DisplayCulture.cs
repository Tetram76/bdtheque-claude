using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Display culture, a setting of the application (fonctionnel/presentation/langue-et-culture-d-affichage.md):
/// French of France, the only one offered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DisplayCulture>))]
public enum DisplayCulture
{
    FrenchFrance = 1,
}
