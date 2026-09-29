using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A media item attached to an edition (Visuel d'édition): cover, dedication, endpaper, plate
/// or back cover. Display order is the rank among visuals of the same type; the type itself
/// orders visuals per fonctionnel.md § Ordre des visuels d'une édition. An
/// <see cref="EditionVisual"/> cannot exist without its <see cref="Entities.Edition"/>.
/// </summary>
public sealed class EditionVisual : EntityBase
{
    public Guid EditionId { get; private set; }
    public Edition Edition { get; private set; } = null!;

    public VisualType Type { get; private set; }
    public string MediaReference { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    // EF Core parameterless constructor
    private EditionVisual() { }

    public EditionVisual(Edition edition, VisualType type, string mediaReference, int displayOrder)
    {
        ArgumentNullException.ThrowIfNull(edition);
        EnumGuard.EnsureDefined(type, nameof(type));

        Edition = edition;
        EditionId = edition.Id;
        Type = type;
        SetMediaReference(mediaReference);
        SetDisplayOrder(displayOrder);
    }

    public void SetType(VisualType type)
    {
        EnumGuard.EnsureDefined(type, nameof(type));
        Type = type;
    }

    public void SetMediaReference(string mediaReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaReference);
        MediaReference = mediaReference.Trim();
    }

    /// <summary>
    /// Sets the display rank among visuals of the same type (fonctionnel.md § Ordre des
    /// visuels d'une édition) — adjustable manually by the user to reorder them.
    /// </summary>
    public void SetDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(displayOrder), displayOrder, "Display order must not be negative.");
        DisplayOrder = displayOrder;
    }
}
