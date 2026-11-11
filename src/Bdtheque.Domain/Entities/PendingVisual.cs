using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A file uploaded for a visual while an edition is entered, waiting for the saving of the edition, which makes it one of its
/// visuals (visuel/saisie-edition/contenu.md; choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente):
/// validated and converted as any visual, its files written, and kept for <see cref="Lifetime"/>.
/// </summary>
/// <remarks>Not a record: attached to no edition, neither in the change log nor a write on an aggregate.</remarks>
public sealed class PendingVisual : EntityBase
{
    /// <summary>How long a pending visual is kept, waiting for the saving of its edition (decision of the user).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public string MediaReference { get; private set; } = string.Empty;

    /// <summary>The colours drawn from the image at its upload, which the visual it becomes keeps.</summary>
    public VisualPalette Palette { get; private set; } = null!;

    // EF Core parameterless constructor
    private PendingVisual() { }

    public PendingVisual(string mediaReference, VisualPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        MediaReference = DomainText.Required(mediaReference, DomainRules.EditionVisualMediaReferenceRequired, "The file of a visual is required.");
        Palette = palette;
    }

    /// <summary>When the pending visual is no longer kept, from the date of its upload.</summary>
    public DateTimeOffset ExpiresAt => CreatedAt + Lifetime;

    /// <summary>Whether the pending visual is no longer kept at <paramref name="now"/>: it can no longer become a visual.</summary>
    public bool IsExpiredAt(DateTimeOffset now) => now >= ExpiresAt;
}
