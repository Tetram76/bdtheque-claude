namespace Bdtheque.Contracts.Admin;

/// <summary>
/// Where <c>api</c> serves the media of a visual waiting for the saving of its edition
/// (choix-implementation/domaine/visuels-stockage-et-traitement.md, Zone d'attente): for the administration only, while the
/// visual is kept, the entry presenting it as the visuals already saved.
/// </summary>
public static class PendingVisualMediaRoutes
{
    /// <summary>The reduced WebP version, shown by default.</summary>
    public static string Display(Guid pendingVisualId) => $"/admin/visuals/pending/{pendingVisualId}/display";

    /// <summary>The original, as uploaded.</summary>
    public static string Original(Guid pendingVisualId) => $"/admin/visuals/pending/{pendingVisualId}/original";
}
