using System.Reflection;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The reference captures, kept in the repository (<c>Captures/References/{theme}/{screen}.png</c>), and what a failed
/// comparison leaves for the reviewer (<c>TestResults/captures</c>, an artifact of the CI): the capture taken and the
/// difference image. With <see cref="UpdateVariable"/> set, a capture becomes the new reference instead of being compared:
/// a wanted change of the rendering updates the references it concerns, which the pull request shows before and after.
/// </summary>
public static class CaptureStore
{
    public const string UpdateVariable = "BDTHEQUE_UPDATE_CAPTURES";

    private static readonly string ProjectDirectory = typeof(CaptureStore).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "ProjectDirectory").Value!;

    public static string ReferencesDirectory { get; } = Path.Combine(ProjectDirectory, "Captures", "References");

    public static string ResultsDirectory { get; } = Path.Combine(ProjectDirectory, "TestResults", "captures");

    private static bool Updating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

    /// <summary>Compares the capture with its reference, or makes it the reference when updating.</summary>
    public static void Verify(string theme, string screen, byte[] capture)
    {
        var reference = Path.Combine(ReferencesDirectory, theme, $"{screen}.png");
        if (Updating)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reference)!);
            File.WriteAllBytes(reference, capture);
            return;
        }

        var name = $"{theme}-{screen}";
        if (!File.Exists(reference))
        {
            Keep($"{name}.actual.png", capture);
            Assert.Fail($"No reference capture for {name}: run scripts/Test-Captures.ps1 -Update, and review the new reference.");
        }

        var comparison = CaptureComparison.Compare(File.ReadAllBytes(reference), capture);
        if (comparison.Identical)
            return;

        Keep($"{name}.actual.png", capture);
        if (comparison.Difference is { } difference)
            Keep($"{name}.difference.png", difference);
        Assert.Fail(comparison.SizeMismatch is { } sizes
            ? $"The capture of {name} changed size ({sizes})."
            : $"The capture of {name} differs from its reference in {comparison.DifferentPixels} pixels.");
    }

    private static void Keep(string fileName, byte[] content)
    {
        Directory.CreateDirectory(ResultsDirectory);
        File.WriteAllBytes(Path.Combine(ResultsDirectory, fileName), content);
    }
}
