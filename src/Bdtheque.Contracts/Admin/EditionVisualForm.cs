using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The visuals of an edition, in their presentation order
/// (fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md). The edition belongs to the aggregate of its
/// album: the version sent back with any
/// upload, rearrangement or deletion is the album's.
/// </summary>
/// <param name="AlbumVersion">Version of the album, sent back with any write on the visuals.</param>
public sealed record EditionVisualsForm(Guid EditionId, IReadOnlyList<EditionVisualForm> Visuals, uint AlbumVersion);

/// <summary>A visual of an edition; <c>api</c> serves its media (<see cref="Catalog.VisualMediaRoutes"/>).</summary>
/// <param name="DisplayOrder">Rank among the visuals of the same type.</param>
public sealed record EditionVisualForm(Guid Id, VisualType Type, int DisplayOrder);

/// <summary>
/// Names of the fields of the multipart form uploading a visual (<c>POST …/editions/{editionId}/visuals</c>).
/// </summary>
public static class UploadVisualFields
{
    /// <summary>The image file.</summary>
    public const string File = "file";

    /// <summary>The <see cref="VisualType"/> of the visual, by name.</summary>
    public const string Type = "type";

    /// <summary>Version of the album the form was read at.</summary>
    public const string AlbumVersion = "albumVersion";
}

/// <summary>
/// The arrangement of the visuals of an edition, as the user ordered them: each visual takes its
/// type, and its rank among the visuals of that type is its position in the list.
/// </summary>
/// <param name="Visuals">Every visual of the edition, exactly once.</param>
/// <param name="AlbumVersion">Version of the album the visuals were read at.</param>
public sealed record ArrangeVisualsRequest(IReadOnlyList<VisualArrangement> Visuals, uint AlbumVersion);

/// <summary>The place of one visual in an arrangement.</summary>
public sealed record VisualArrangement(Guid Id, VisualType Type);

/// <summary>
/// A visual of an edition as its entry gives it, saved with the edition
/// (choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente): a visual of the edition, kept, or a pending
/// visual, which becomes one; its place in the list is its rank among the visuals of its type.
/// </summary>
/// <param name="Id">A visual of the edition, kept; <c>null</c> for a pending visual.</param>
/// <param name="PendingId">A pending visual (<see cref="PendingVisualForm"/>), which becomes a visual of the edition.</param>
public sealed record EditionVisualEntry(Guid? Id, Guid? PendingId, VisualType Type);

/// <summary>
/// A file uploaded for a visual while an edition is entered (<c>POST /admin/visuals/pending</c>, form field
/// <see cref="UploadVisualFields.File"/>), kept until <paramref name="ExpiresAt"/> for the saving of the edition.
/// </summary>
public sealed record PendingVisualForm(Guid Id, DateTimeOffset ExpiresAt);

/// <summary>What the entry of the visuals announces, which api alone checks (<c>GET /admin/visuals/limits</c>).</summary>
/// <param name="MaxFileSizeBytes">Heaviest file accepted for a visual.</param>
/// <param name="PendingLifetime">How long a pending visual is kept for the saving of its edition.</param>
public sealed record VisualLimits(long MaxFileSizeBytes, TimeSpan PendingLifetime);
