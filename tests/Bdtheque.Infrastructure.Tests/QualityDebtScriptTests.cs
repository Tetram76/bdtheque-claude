namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The two steps of .github/workflows/quality-debt.yml as the CI runs them (gestion-projet/depot/dette-qualite-au-fil-de-l-eau.md):
/// .github/scripts/Test-QualityDebtHandled.ps1 fails while an entry along the way on a file a pull request changes is not
/// handled in its description, and .github/scripts/Remove-QualityDebt.ps1 removes at its merge the entries it fixed or
/// rejected. GitHub is replaced by a fake gh, first on the path, that serves the tracking issue, the changed files and the
/// description from files, and records the description written.
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed class QualityDebtScriptTests : IAsyncLifetime
{
    private const string AlbumMutant = "mutant src/Bdtheque.Domain/Entities/Album.cs Statement mutation: EnsureOwnIntent(intent); → ;";
    private const string AlbumStyle = "style IDE0005 src/Bdtheque.Domain/Entities/Album.cs";
    private const string SeriesMutant = "mutant src/Bdtheque.Domain/Entities/Series.cs Statement mutation: MergeGuard.EnsureDistinct(other, this); → ;";
    private const string GenreStyle = "style IDE0300 src/Bdtheque.Domain/Entities/Genre.cs";
    private const string EditionSonar = "sonar csharpsquid:S3776 src/Bdtheque.Domain/Entities/Edition.cs";

    // The form the weekly audit writes (.github/workflows/quality-audit.yml), a priority entry on a changed file included:
    // only the entries along the way are a chantier's to handle.
    private static readonly string IssueBody = string.Join('\n',
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
        GenreStyle,
        "```",
        "<!-- end -->");

    private string _gitHub = null!;

    public async Task InitializeAsync()
    {
        _gitHub = Path.Combine(Path.GetTempPath(), $"bdtheque-gh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_gitHub);
        await File.WriteAllTextAsync(Path.Combine(_gitHub, "issue.md"), IssueBody);
        await File.WriteAllTextAsync(Path.Combine(_gitHub, "merged.txt"), "682\n");
        WriteFakeGh();
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_gitHub, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UnhandledEntriesOfChangedFiles_FailTheCheckWithTheLinesToAdd()
    {
        await ChangeAsync(["src/Bdtheque.Domain/Entities/Album.cs", "src/Bdtheque.Domain/Entities/Edition.cs"]);
        await DescribeAsync("Une description sans dette traitée.");

        var result = await CheckAsync();

        Assert.False(result.Succeeded, result.Output);
        Assert.Equal(
            [
                "Quality debt along the way on a file this pull request changes, each fixed in it, or rejected or deferred:",
                $"  NOT HANDLED: {AlbumMutant}",
                $"  NOT HANDLED: {AlbumStyle}",
                "Handle each entry, then say what was done with it in the description of the pull request, between these",
                "markers, by \"fixed\", \"rejected\" or \"deferred #<ticket>\" in place of the placeholder:",
                "<!-- quality-debt -->",
                "```text",
                $"<fixed|rejected|deferred #ticket> {AlbumMutant}",
                $"<fixed|rejected|deferred #ticket> {AlbumStyle}",
                "```",
                "<!-- end-quality-debt -->",
                "Editing the description runs this check again.",
            ],
            Lines(result.Output));
    }

    [Fact]
    public async Task EntriesHandledInTheDescription_PassTheCheck()
    {
        await ChangeAsync(["src/Bdtheque.Domain/Entities/Album.cs", "src/Bdtheque.Domain/Entities/Genre.cs"]);
        await DescribeAsync($"## Description\n\n<!-- quality-debt -->\n```text\nfixed {AlbumMutant}\nrejected {AlbumStyle}\ndeferred #712 {GenreStyle}\n```\n<!-- end-quality-debt -->\n");

        var result = await CheckAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(
            [
                "Quality debt along the way on a file this pull request changes, each fixed in it, or rejected or deferred:",
                $"  fixed: {AlbumMutant}",
                $"  rejected: {AlbumStyle}",
                $"  deferred #712: {GenreStyle}",
            ],
            Lines(result.Output));
    }

    [Fact]
    public async Task PlaceholderCopiedAsIs_HandlesNothing()
    {
        // The lines the check gives must be completed with what was done: copied unchanged, they say nothing.
        await ChangeAsync(["src/Bdtheque.Domain/Entities/Genre.cs"]);
        await DescribeAsync($"<!-- quality-debt -->\n```text\n<fixed|rejected|deferred #ticket> {GenreStyle}\n```\n<!-- end-quality-debt -->\n");

        var result = await CheckAsync();

        Assert.False(result.Succeeded, result.Output);
        Assert.Contains($"  NOT HANDLED: {GenreStyle}", Lines(result.Output));
    }

    [Fact]
    public async Task MovedFile_BringsTheEntriesOfItsFormerPath()
    {
        // The files of a pull request give a moved file at its new path, then at its former one.
        await ChangeAsync(["src/Bdtheque.Domain/Entities/Serie.cs", "src/Bdtheque.Domain/Entities/Series.cs"]);
        await DescribeAsync("");

        var result = await CheckAsync();

        Assert.False(result.Succeeded, result.Output);
        Assert.Contains($"  NOT HANDLED: {SeriesMutant}", Lines(result.Output));
    }

    [Fact]
    public async Task NoEntryOnAChangedFile_IsSaidExplicitly()
    {
        await ChangeAsync(["README.md"]);
        await DescribeAsync("");

        var result = await CheckAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(["No quality debt along the way on a file this pull request changes."], Lines(result.Output));
    }

    [Fact]
    public async Task UnreadableTrackingIssue_FailsInsteadOfListingNothing()
    {
        await ChangeAsync(["src/Bdtheque.Domain/Entities/Album.cs"]);
        await DescribeAsync("");
        File.Delete(Path.Combine(_gitHub, "issue.md"));

        var result = await CheckAsync();

        Assert.False(result.Succeeded, result.Output);
        Assert.DoesNotContain("No quality debt", result.Output);
    }

    [Fact]
    public async Task Merge_RemovesTheFixedAndRejectedEntries_AndLeavesEveryOtherLine()
    {
        await DescribeAsync($"<!-- quality-debt -->\n```text\nfixed {AlbumMutant}\nrejected {AlbumStyle}\ndeferred #712 {GenreStyle}\n```\n<!-- end-quality-debt -->\n");

        var result = await RemoveAsync();

        Assert.True(result.Succeeded, result.Output);
        var expected = IssueBody.Replace($"{AlbumMutant}\n", "").Replace($"{AlbumStyle}\n", "");
        Assert.Equal(expected, await File.ReadAllTextAsync(Path.Combine(_gitHub, "edited.md")));
    }

    [Fact]
    public async Task MergeWithoutHandledDebt_LeavesTheIssueUntouched()
    {
        await DescribeAsync("Une description sans dette traitée.");

        var result = await RemoveAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.False(File.Exists(Path.Combine(_gitHub, "edited.md")));
    }

    [Fact]
    public async Task CommitOfNoMergedPullRequest_LeavesTheIssueUntouched()
    {
        await File.WriteAllTextAsync(Path.Combine(_gitHub, "merged.txt"), "");

        var result = await RemoveAsync();

        Assert.True(result.Succeeded, result.Output);
        Assert.False(File.Exists(Path.Combine(_gitHub, "edited.md")));
    }

    // gh as the scripts call it, the --jq filters already applied: the changed files one per line, the description as
    // text, the numbers of the merged pull requests, the tracking issue as JSON, its description edited from a file. A
    // failure stops the fake and, run as its own process outside Windows, exits non-zero, as gh does. PowerShell finds a
    // script of the path by its name, gh.ps1 on Windows, an executable gh elsewhere, before the gh installed further on.
    private void WriteFakeGh()
    {
        const string script = """
            $ErrorActionPreference = 'Stop'
            $state = $env:FAKE_GH_STATE
            $call = $args -join ' '
            # Each call is matched in full, arguments included, as gh would read them.
            switch -Regex ($call) {
                '^api --paginate repos/\{owner\}/\{repo\}/pulls/\d+/files\?per_page=100 --jq \.\[\] \| \.filename, \(\.previous_filename // empty\)$' { Get-Content "$state/files.txt"; break }
                '^api repos/\{owner\}/\{repo\}/commits/\w+/pulls --jq \.\[\] \| select\(\.merged_at != null\) \| \.number$' { Get-Content "$state/merged.txt"; break }
                '^api repos/\{owner\}/\{repo\}/pulls/\d+ --jq \.body // ""$' { [IO.File]::ReadAllText("$state/pull-request.md"); break }
                '^issue list --label quality-audit --label harness --state all --limit 1 --json number,body$' { @(@{ number = 381; body = [IO.File]::ReadAllText("$state/issue.md") }) | ConvertTo-Json -AsArray; break }
                '^issue edit 381 --body-file \S+$' { Copy-Item $args[-1] "$state/edited.md"; break }
                default { throw "Unexpected gh call: $call" }
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

    private Task ChangeAsync(string[] files) =>
        File.WriteAllTextAsync(Path.Combine(_gitHub, "files.txt"), string.Join('\n', files) + "\n");

    private Task DescribeAsync(string body) => File.WriteAllTextAsync(Path.Combine(_gitHub, "pull-request.md"), body);

    private Dictionary<string, string> Environment() => new()
    {
        ["PATH"] = _gitHub + Path.PathSeparator + System.Environment.GetEnvironmentVariable("PATH"),
        ["FAKE_GH_STATE"] = _gitHub,
    };

    // The indentation of an entry is kept; blank lines are not part of the report.
    private static string[] Lines(string output) =>
        [.. output.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0)];

    private Task<ProcessResult> CheckAsync() => RunAsync("Test-QualityDebtHandled.ps1", ["-PullRequest", "682"]);

    private Task<ProcessResult> RemoveAsync() => RunAsync("Remove-QualityDebt.ps1", ["-Commit", "0559e003"]);

    private Task<ProcessResult> RunAsync(string script, string[] arguments) =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, ".github", "scripts", script), arguments, _gitHub, TimeSpan.FromMinutes(1),
            Environment());
}
