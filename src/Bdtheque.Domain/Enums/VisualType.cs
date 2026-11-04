namespace Bdtheque.Domain.Enums;

/// <summary>
/// Type of visual attached to an edition (Type de visuel). Values are assigned in the fixed
/// display order required by fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md, so that ordering
/// by type can rely directly on the persisted int value (see
/// choix-implementation/persistance/conventions-de-persistance-ef-core.md).
/// </summary>
public enum VisualType
{
    Cover = 1,
    Dedication = 2,
    Endpaper = 3,
    Plate = 4,
    BackCover = 5,
}
