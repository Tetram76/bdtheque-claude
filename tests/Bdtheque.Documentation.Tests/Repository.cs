namespace Bdtheque.Documentation.Tests;

/// <summary>The repository whose documentation, scripts and tools the tests run.</summary>
internal static class Repository
{
    public static string Root { get; } = FindRoot();

    public static string Script(string name) => Path.Combine(Root, "scripts", name);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bdtheque.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Bdtheque.slnx) not found.");
    }
}
