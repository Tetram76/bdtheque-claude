namespace Bdtheque.Documentation.Tests;

/// <summary>
/// A throwaway Git repository for a test of a script, removed after it. Its commits are not signed: they never leave the
/// machine, and the signature of this repository's commits is configured for it alone.
/// </summary>
internal sealed class GitFixture : IAsyncDisposable
{
    private GitFixture(string root) => Root = root;

    public string Root { get; }

    public static async Task<GitFixture> CreateAsync()
    {
        var fixture = new GitFixture(Path.Combine(Path.GetTempPath(), $"bdtheque-git-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(fixture.Root);
        await fixture.GitAsync("init", "--quiet", "--initial-branch=main");
        return fixture;
    }

    public void Write(string path, string content)
    {
        var file = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }

    public Task<string> GitAsync(params string[] arguments) =>
        Processes.GitAsync(Root, ["-c", "user.name=Test", "-c", "user.email=test@example.invalid", "-c", "commit.gpgsign=false", .. arguments]);

    public async Task CommitAsync(string message)
    {
        await GitAsync("add", "--all");
        await GitAsync("commit", "--quiet", "-m", message);
    }

    public ValueTask DisposeAsync()
    {
        // Git marks its objects read-only, which Directory.Delete refuses on Windows.
        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(Root, recursive: true);
        return ValueTask.CompletedTask;
    }
}
