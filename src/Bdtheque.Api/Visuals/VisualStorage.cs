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

    /// <summary>
    /// Checks at startup that the volume can be written, creating its folders: nothing in the
    /// deployment guarantees it — <c>api</c> does not run as root, while Docker creates a missing
    /// host folder as root, and a NAS shared folder belongs to a NAS user. Failing here reports the
    /// misconfiguration at deployment, rather than at the first upload.
    /// </summary>
    /// <exception cref="InvalidOperationException">The volume cannot be written.</exception>
    public void EnsureWritable()
    {
        foreach (var folder in new[] { OriginalsFolder, DisplayFolder })
        {
            var path = Path.Combine(_root, folder);
            try
            {
                Directory.CreateDirectory(path);
                // Creating a folder that already exists checks nothing: a file is written to make sure.
                var probe = Path.Combine(path, $".write-check-{Guid.NewGuid():N}");
                File.WriteAllBytes(probe, []);
                File.Delete(probe);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException(
                    $"The visuals volume cannot be written at '{path}'. Give write access to the user of the 'api' "
                    + $"container (UID {Environment.GetEnvironmentVariable("APP_UID") ?? "of the process"}) on the folder "
                    + "mounted there (VISUELS_HOST_PATH).",
                    exception);
            }
        }
    }

    /// <summary>
    /// Warns when the volume is not mounted from outside the container, which nothing can impose
    /// either: its files would then live in the container, and be lost whenever it is recreated.
    /// </summary>
    public void WarnIfNotMounted()
    {
        if (MountInfo.IsOnMount(_root) == false)
            logger.LogWarning(
                "The visuals volume '{Root}' is not mounted from outside the container: its files will be lost "
                + "whenever the container is recreated. Mount a host folder there (VISUELS_HOST_PATH).",
                _root);
    }

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
