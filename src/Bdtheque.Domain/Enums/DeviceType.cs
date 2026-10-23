namespace Bdtheque.Domain.Enums;

/// <summary>
/// Category of the device of a visit (Type d'appareil, modele-metier.md § Visite de l'interface web): with the
/// rendering engine, the only characteristic of the device the audience measurement keeps.
/// </summary>
public enum DeviceType
{
    Computer = 1,
    Tablet = 2,
    Smartphone = 3,
}
