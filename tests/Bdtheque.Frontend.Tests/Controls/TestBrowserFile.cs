using Microsoft.AspNetCore.Components.Forms;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// A file chosen in the browser, as <see cref="InputFile"/> gives it: its content read from memory, within the size its reader
/// allows, as Blazor's own file refuses a larger one (<see cref="IOException"/>).
/// </summary>
/// <remarks>
/// bUnit's <c>UploadFiles</c> raises the choice synchronously, and blocks the test until its handling ends: a handling that
/// awaits its sending, as the documentation of <see cref="InputFile"/> shows, could not be awaited as the other events are
/// (<see cref="CircuitEvents"/>). The choice is raised on the renderer's dispatcher instead, with these files.
/// </remarks>
internal sealed class TestBrowserFile(string name, byte[] content) : IBrowserFile
{
    public string Name { get; } = name;

    public DateTimeOffset LastModified { get; } = DateTimeOffset.UnixEpoch;

    public long Size => content.Length;

    public string ContentType => "application/octet-stream";

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
        Size > maxAllowedSize
            ? throw new IOException($"Supplied file with size {Size} bytes exceeds the maximum of {maxAllowedSize} bytes.")
            : new MemoryStream(content, writable: false);
}
