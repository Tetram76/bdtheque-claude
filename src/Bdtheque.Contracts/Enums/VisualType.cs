using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Type of visual attached to an edition (Type de visuel), its values following the fixed display
/// order of fonctionnel.md § Ordre des visuels d'une édition.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<VisualType>))]
public enum VisualType
{
    Cover = 1,
    Dedication = 2,
    Endpaper = 3,
    Plate = 4,
    BackCover = 5,
}
