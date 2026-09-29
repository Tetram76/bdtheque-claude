namespace Bdtheque.Domain.Enums;

/// <summary>
/// Type of visual attached to an edition (Type de visuel). Values are assigned in the fixed
/// display order required by fonctionnel.md § Ordre des visuels d'une édition, so that ordering
/// by type can rely directly on the persisted int value (see choix-implementation.md §
/// Conventions de persistance).
/// </summary>
public enum VisualType
{
    Cover = 1,
    Dedication = 2,
    Endpaper = 3,
    Plate = 4,
    BackCover = 5,
}
