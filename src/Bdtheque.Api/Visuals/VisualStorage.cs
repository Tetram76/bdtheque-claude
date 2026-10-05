using Microsoft.Extensions.Options;

namespace Bdtheque.Api.Visuals;

/// <summary>
/// The files of the visuals on their volume (choix-implementation.md § Visuels : stockage et
/// traitement): the original under <c>originals/</c>, the display version under <c>display/</c>, both
/// named after the media reference of the visual. Files and rows cannot be written atomically: each
/// write is compensated by its caller, and <see cref="VisualReconciliation"/> removes what remains.
/// </summary>
internal sealed class VisualStorage(IOptions<VisualStorageOptions> options, ILogger<VisualStorage> logger)
{
    private const string OriginalsFolder = "originals";
    private const string DisplayFolder = "display";

    private readonly string _root = options.Value.RootPath;

    /// <summary>
    /// A media reference never used before (time-ordered GUID, with the extension of the original):
    /// a file name is never reused, so that <c>frontend</c> can serve it with a long-lived cache.
    /// </summary>
    public static string NewMediaReference(PreparedVisual visual) => $"{Guid.CreateVersion7():N}.{visual.OriginalExtension}";

    /// <summary>Path of the original, relative to the volume.</summary>
    public static string OriginalPath(string mediaReference) => $"{OriginalsFolder}/{mediaReference}";

    /// <summary>Path of the display version, relative to the volume.</summary>
    public static string DisplayPath(string mediaReference) => $"{DisplayFolder}/{StemOf(mediaReference)}.webp";

    /// <summary>The part of a file name, or of a media reference, that both files of a visual share.</summary>
    public static string StemOf(string fileName) => Path.GetFileNameWithoutExtension(fileName);

    /// <summary>Writes both files of a visual; neither is left behind if the second cannot be written.</summary>
    public async Task WriteAsync(string mediaReference, PreparedVisual visual, CancellationToken cancellationToken)
    {
        try
        {
            await WriteFileAsync(OriginalPath(mediaReference), visual.Original, cancellationToken);
            await WriteFileAsync(DisplayPath(mediaReference), visual.Display, cancellationToken);
        }
        catch
        {
            Delete(mediaReference);
            throw;
        }
    }

    /// <summary>
    /// Deletes both files of each visual. A file that cannot be deleted is only logged: it is an
    /// orphan, without effect on the data, which the reconciliation removes later.
    /// </summary>
    public void Delete(params IEnumerable<string> mediaReferences)
    {
        foreach (var mediaReference in mediaReferences)
        {
            DeleteFile(OriginalPath(mediaReference));
            DeleteFile(DisplayPath(mediaReference));
        }
    }

    /// <summary>Every file of the volume, whether a visual references it or not.</summary>
    public IEnumerable<FileInfo> Files() =>
        new[] { OriginalsFolder, DisplayFolder }
            .Select(folder => new DirectoryInfo(Path.Combine(_root, folder)))
            .Where(folder => folder.Exists)
            .SelectMany(folder => folder.EnumerateFiles());

    /// <summary>Deletes a file of the volume, logging a failure as <see cref="Delete"/> does.</summary>
    public void DeleteFile(FileInfo file) => TryDelete(file.FullName);

    private async Task WriteFileAsync(string relativePath, byte[] content, CancellationToken cancellationToken)
    {
        var path = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // CreateNew: a media reference is new by construction, an existing file is never overwritten.
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await stream.WriteAsync(content, cancellationToken);
    }

    private void DeleteFile(string relativePath) => TryDelete(FullPath(relativePath));

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not delete the visual file {Path}; the reconciliation will remove it.", path);
        }
    }

    private string FullPath(string relativePath) => Path.Combine(_root, relativePath);
}
