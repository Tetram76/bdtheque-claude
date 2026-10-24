namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// Where <c>api</c> serves the media of a visual (choix-implementation/domaine.md § Visuels : stockage et
/// traitement): the only access to them, the volume being private to <c>api</c>, as the database is.
/// A visual never changes media, so that its media can be kept by any cache under these addresses.
/// Public data, as the visuals are in the consultation: the administration reads them here too.
/// </summary>
public static class VisualMediaRoutes
{
    /// <summary>The reduced WebP version, shown by default.</summary>
    public static string Display(Guid visualId) => $"/catalog/visuals/{visualId}/display";

    /// <summary>The original, as uploaded.</summary>
    public static string Original(Guid visualId) => $"/catalog/visuals/{visualId}/original";
}
