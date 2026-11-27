namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// scripts/Get-QualityDebt.ps1 and scripts/Remove-QualityDebt.ps1 as an agent runs them, its output redirected: the
/// first lists the entries along the way of the quality tracking issue that bear on the files a branch changes, the
/// second removes merged entries from that issue (gestion-projet/depot/dette-qualite-au-fil-de-l-eau.md). GitHub is
/// replaced by a fake gh, first on the path, that serves the issue from a file and records the description written.
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed class QualityDebtScriptTests : IAsyncLifetime
{
    private const string AlbumMutant = "mutant src/Bdtheque.Domain/Entities/Album.cs Statement mutation: EnsureOwnIntent(intent); → ;";
    private const string AlbumStyle = "style IDE0005 src/Bdtheque.Domain/Entities/Album.cs";
    private const string SeriesMutant = "mutant src/Bdtheque.Domain/Entities/Series.cs Statement mutation: MergeGuard.EnsureDistinct(other, this); → ;";
    private const string EditionSonar = "sonar csharpsquid:S3776 src/Bdtheque.Domain/Entities/Edition.cs";

    // The form the weekly audit writes (.github/workflows/quality-audit.yml), a priority entry on a changed file included:
    // only the entries along the way are a chantier's to fix.
    private static readonly string Body = string.Join('\n',
        "Dette qualité du code de `main`.",
        "",
        "Dernier contrôle : 2026-11-26",
        "",
        "## Prioritaire",
        "",
        "<!-- to-treat -->",
        "```text",
        EditionSonar,
        "```",
        "<!-- end-to-treat -->",
        "",
        "## Au fil de l'eau",
        "",
        "<!-- list -->",
        "```text",
        AlbumMutant,
        SeriesMutant,
        AlbumStyle,
        "```",
        "<!-- end -->");

    private GitFixture _git = null!;
    private string _gitHub = null!;

    public async Task InitializeAsync()
    {
        _git = await GitFixture.CreateAsync();
        foreach (var entity in new[] { "Album", "Series", "Edition", "Genre" })
            _git.Write($"src/Bdtheque.Domain/Entities/{entity}.cs", $"class {entity} {{ }}\n");
        await _git.CommitAsync("base");
        await _git.GitAsync("switch", "--quiet", "--create", "change");

        _gitHub = Path.Combine(Path.GetTempPath(), $"bdtheque-gh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_gitHub);
        await File.WriteAllTextAsync(Path.Combine(_gitHub, "body.md"), Body);
        WriteFakeGh();
    }

    public async Task DisposeAsync()
    {
        await _git.DisposeAsync();
        Directory.Delete(_gitHub, recursive: true);
    }

    [Fact]
    public async Task EntriesAlongTheWayOfChangedFiles_AreListedUnderTheirFile()
    {
        _git.Write("src/Bdtheque.Domain/Entities/Album.cs", "class Album { int Id; }\n");
        await _git.CommitAsync("change the album");
        // Uncommitted changes count too, and a file with a priority entry only brings none.
        _git.Write("src/Bdtheque.Domain/Entities/Edition.cs", "class Edition { int Id; }\n");

        var result = await GetAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(
            [
                "Quality debt along the way on a changed file (each fixed in this pull request, removed from the tracking issue once merged):",
                "src/Bdtheque.Domain/Entities/Album.cs",
                $"  {AlbumMutant}",
                $"  {AlbumStyle}",
            ],
            Lines(result.Output));
    }

    [Fact]
    public async Task MovedFile_BringsTheEntriesOfItsFormerPath()
    {
        await _git.GitAsync("mv", "src/Bdtheque.Domain/Entities/Series.cs", "src/Bdtheque.Domain/Entities/Serie.cs");
        await _git.CommitAsync("move the series");

        var result = await GetAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Contains($"  {SeriesMutant}", Lines(result.Output));
    }

    [Fact]
    public async Task NoEntryOnAChangedFile_IsSaidExplicitly()
    {
        _git.Write("src/Bdtheque.Domain/Entities/Genre.cs", "class Genre { int Id; }\n");
        await _git.CommitAsync("change a file without debt");

        var result = await GetAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(["No quality debt along the way on a changed file."], Lines(result.Output));
    }

    [Fact]
    public async Task UnreadableTrackingIssue_FailsInsteadOfListingNothing()
    {
        File.Delete(Path.Combine(_gitHub, "body.md"));

        var result = await GetAsync();

        Assert.False(result.Succeeded, result.Output);
    }

    [Fact]
    public async Task RemovedEntries_LeaveEveryOtherLineAsRead()
    {
        var result = await RemoveAsync(AlbumMutant, AlbumStyle);

        Assert.True(result.Succeeded, result.Output);
        var expected = Body.Replace($"{AlbumMutant}\n", "").Replace($"{AlbumStyle}\n", "");
        Assert.Equal(expected, await File.ReadAllTextAsync(Path.Combine(_gitHub, "edited.md")));
    }

    [Fact]
    public async Task EntryTheIssueDoesNotHold_StopsBeforeAnyChange()
    {
        var result = await RemoveAsync(AlbumMutant, "style IDE0005 src/Bdtheque.Domain/Entities/Albums.cs");

        Assert.False(result.Succeeded, result.Output);
        Assert.Contains("Albums.cs", result.Output);
        Assert.False(File.Exists(Path.Combine(_gitHub, "edited.md")));
    }

    // gh as the scripts call it: the tracking issue listed as JSON, its description edited from a file. PowerShell finds a
    // script of the path by its name, gh.ps1 on Windows, an executable gh elsewhere, before the gh installed further on.
    private void WriteFakeGh()
    {
        // A failure stops the fake and, run as its own process outside Windows, exits non-zero, as gh does.
        const string script = """
            $ErrorActionPreference = 'Stop'
            $state = $env:FAKE_GH_STATE
            switch ($args[1]) {
                'list' { @(@{ number = 381; body = [IO.File]::ReadAllText("$state/body.md") }) | ConvertTo-Json -AsArray }
                'edit' { Copy-Item $args[[array]::IndexOf($args, '--body-file') + 1] "$state/edited.md" }
            }
            """;
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(Path.Combine(_gitHub, "gh.ps1"), script);
            return;
        }
        var gh = Path.Combine(_gitHub, "gh");
        File.WriteAllText(gh, $"#!/usr/bin/env pwsh\n{script}\n");
        File.SetUnixFileMode(gh, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private Dictionary<string, string> Environment() => new()
    {
        ["PATH"] = _gitHub + Path.PathSeparator + System.Environment.GetEnvironmentVariable("PATH"),
        ["FAKE_GH_STATE"] = _gitHub,
    };

    // The indentation of an entry under its file is kept; blank lines are not part of the listing.
    private static string[] Lines(string output) =>
        [.. output.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0)];

    private Task<ProcessResult> GetAsync() =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, "scripts", "Get-QualityDebt.ps1"), ["-Base", "main"], _git.Root,
            TimeSpan.FromMinutes(1), Environment());

    private Task<ProcessResult> RemoveAsync(params string[] entries) =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, "scripts", "Remove-QualityDebt.ps1"), ["-Entry", string.Join('\n', entries)],
            _git.Root, TimeSpan.FromMinutes(1), Environment());
}
