using System.ComponentModel.DataAnnotations;

namespace Bdtheque.Api.Visuals;

/// <summary>The visuals volume (contraintes-techniques.md § Stockage des visuels) and the upload limit.</summary>
internal sealed class VisualStorageOptions
{
    public const string SectionName = "Visuals";

    /// <summary>Directory where the visuals volume is mounted, writable by <c>api</c>.</summary>
    [Required]
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Heaviest file accepted for a visual, checked by <c>api</c> itself: a heavier file is a business
    /// error, which the administrator corrects by reducing the scan.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; set; } = 30 * 1024 * 1024;
}
