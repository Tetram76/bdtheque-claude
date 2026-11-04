using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Category of the device of a visit (Type d'appareil, modele-metier/visite-de-l-interface-web.md): with the
/// rendering engine, the only characteristic of the device the audience measurement keeps.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeviceType>))]
public enum DeviceType
{
    Computer = 1,
    Tablet = 2,
    Smartphone = 3,
}
