using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The visuals of an edition, in their presentation order
/// (fonctionnel/regles-metier/ordre-des-visuels-d-une-edition.md). The edition belongs to the aggregate of its
/// album: the version sent back with any
/// upload, rearrangement or deletion is the album's.
/// </summary>
/// <param name="AlbumVersion">Version of the album, sent back with any write on the visuals.</param>
/// <param name="MaxFileSizeBytes">
/// Heaviest file accepted for a visual, which <c>api</c> alone checks: given for the entry to announce it.
/// </param>
public sealed record EditionVisualsForm(Guid EditionId, IReadOnlyList<EditionVisualForm> Visuals, uint AlbumVersion, long MaxFileSizeBytes);

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
