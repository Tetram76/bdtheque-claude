namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// A theory whose captures are only comparable on Linux, where the references are taken: the fonts are not rendered
/// alike on another system. Elsewhere it is skipped, the comparison running in a Linux container instead.
/// </summary>
public sealed class LinuxTheoryAttribute : TheoryAttribute
{
    public LinuxTheoryAttribute()
    {
        if (!OperatingSystem.IsLinux())
            Skip = "Captures are compared on Linux only: run scripts/Test-Captures.ps1.";
    }
}
