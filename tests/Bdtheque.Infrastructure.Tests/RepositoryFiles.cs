namespace Bdtheque.Infrastructure.Tests;

/// <summary>Files of the repository that the tests confront with what they test.</summary>
internal static class RepositoryFiles
{
    /// <summary>
    /// The category of every test that reads a file of the repository (<c>[Trait("Category", RepositoryFiles.Category)]</c>):
    /// a pull request that changes no source runs these tests alone (gestion-projet/depot/integration-continue-ci.md).
    /// </summary>
    public const string Category = "Repository";

    public static string Root { get; } = FindRoot();

    public static string Read(string relativePath) => File.ReadAllText(Path.Combine(Root, relativePath));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bdtheque.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Bdtheque.slnx) not found.");
    }
}
