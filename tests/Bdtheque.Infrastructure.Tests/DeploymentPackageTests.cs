using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The deployment package that .github/scripts/New-DeploymentPackage.ps1 builds for the release of a version
/// (gestion-projet/depot/versions-et-releases.md): the files of the repository needed to deploy that version from its
/// public images on ghcr.io, without the sources (choix-implementation/deploiement/publication-des-images-docker.md).
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed partial class DeploymentPackageTests : IDisposable
{
    private const string Script = ".github/scripts/New-DeploymentPackage.ps1";

    private const string ReleaseNotes = "**Version 0 : développement initial.**\n\n## What's Changed\n* feat(albums): fiche d'un album (#321)\n";

    private readonly string _output = Directory.CreateTempSubdirectory("bdtheque-package-tests-").FullName;
    private readonly string _notes = Path.Combine(Directory.CreateTempSubdirectory("bdtheque-package-notes-").FullName, "notes.md");

    public DeploymentPackageTests()
    {
        File.WriteAllText(_notes, ReleaseNotes);
        WriteReleases();
    }

    private string Releases => Path.Combine(Path.GetDirectoryName(_notes)!, "releases.jsonl");

    [Fact]
    public async Task Package_HoldsTheFilesToDeployTheVersion_InAFolderNamedAfterIt()
    {
        var entries = await BuildAsync("0.1.0");

        Assert.Equal(
            [
                "bdtheque-0.1.0/.env.example", "bdtheque-0.1.0/DEPLOIEMENT.md", "bdtheque-0.1.0/NOTES-DE-VERSION.md",
                "bdtheque-0.1.0/docker-compose.yml",
            ],
            entries.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Notes_AreThoseOfTheRelease_AsPublished_WithoutPreviousRelease()
    {
        var notes = (await BuildAsync("0.1.0"))["bdtheque-0.1.0/NOTES-DE-VERSION.md"];

        Assert.Equal(ReleaseNotes, notes);
    }

    [Fact]
    public async Task Notes_AreFollowedByTheHistory_OfThePreviousVersionsOnly_MostRecentFirst()
    {
        // Whoever skips versions finds in the package what each of them changed (gestion-projet/depot/versions-et-releases.md).
        WriteReleases(Release("v0.0.1", true), Release("v0.1.0"), Release("v0.1.1"), Release("v0.2.0"), Release("v0.3.0"));

        var notes = (await BuildAsync("0.2.0"))["bdtheque-0.2.0/NOTES-DE-VERSION.md"];

        Assert.StartsWith(ReleaseNotes.TrimEnd(), notes, StringComparison.Ordinal);
        Assert.Equal(["v0.1.1", "v0.1.0", "v0.0.1"], HistoryHeadings().Matches(notes).Select(match => match.Groups["tag"].Value));
        Assert.Contains("### Titre v0.0.1 (pré-release)", notes, StringComparison.Ordinal);
    }

    [Fact]
    public async Task History_HoldsOnlyTheChangesOfEachPreviousVersion()
    {
        // The other sections of the notes belong to the version of the package (gestion-projet/depot/versions-et-releases.md).
        WriteReleases(Release("v0.1.0"));

        var notes = (await BuildAsync("0.2.0"))["bdtheque-0.2.0/NOTES-DE-VERSION.md"];
        var history = notes[notes.IndexOf("## Historique", StringComparison.Ordinal)..];

        Assert.Contains("### Titre v0.1.0\n\n#### Consultation\n\n- Fiche de v0.1.0.", history, StringComparison.Ordinal);
        Assert.DoesNotContain("Présentation de v0.1.0", history, StringComparison.Ordinal);
        Assert.DoesNotContain("Compatibilité de v0.1.0", history, StringComparison.Ordinal);
        Assert.DoesNotContain("Changements", history, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Package_IsRefused_WhenAPreviousReleaseHasNoChanges()
    {
        WriteReleases(JsonSerializer.Serialize(new { tag = "v0.1.0", name = "Titre v0.1.0", prerelease = false, body = "Présentation seule.\n" }));

        var result = await RunAsync("0.2.0");

        Assert.False(result.Succeeded, result.Output);
        Assert.Empty(Directory.GetFiles(_output));
    }

    [Fact]
    public async Task History_LeavesTheVersions0Out_From1Point0()
    {
        WriteReleases(Release("v0.1.0"), Release("v0.9.0"), Release("v1.0.0"));

        var notes = (await BuildAsync("1.1.0"))["bdtheque-1.1.0/NOTES-DE-VERSION.md"];

        Assert.Equal(["v1.0.0"], HistoryHeadings().Matches(notes).Select(match => match.Groups["tag"].Value));
    }

    [Fact]
    public async Task History_IsLeftOut_WhenNoPreviousVersionBelongsInIt()
    {
        WriteReleases(Release("v0.9.0"));

        var notes = (await BuildAsync("1.0.0"))["bdtheque-1.0.0/NOTES-DE-VERSION.md"];

        Assert.Equal(ReleaseNotes, notes);
    }

    [Fact]
    public async Task Package_IsRefused_WithoutReleaseNotes()
    {
        File.WriteAllText(_notes, "  \n");

        var result = await RunAsync("0.1.0");

        Assert.False(result.Succeeded, result.Output);
        Assert.Empty(Directory.GetFiles(_output));
    }

    [Fact]
    public async Task Compose_DeploysThePublicImagesOfTheVersion_WithoutBuildingThem()
    {
        var compose = (await BuildAsync("0.1.0"))["bdtheque-0.1.0/docker-compose.yml"];

        Assert.Contains("image: ghcr.io/tetram76/bdtheque-api:0.1.0", compose, StringComparison.Ordinal);
        Assert.Contains("image: ghcr.io/tetram76/bdtheque-frontend:0.1.0", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("tetram76/bdtheque-api:${", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("BDTHEQUE_TAG", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("build:", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("dockerfile:", compose, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compose_KeepsEverythingElseOfTheRepositoryFile()
    {
        var compose = (await BuildAsync("0.1.0"))["bdtheque-0.1.0/docker-compose.yml"];

        Assert.Contains("image: postgres:", compose, StringComparison.Ordinal);
        Assert.Contains("InternalApiKey__Key: ${INTERNAL_API_KEY:?INTERNAL_API_KEY is required}", compose, StringComparison.Ordinal);
        Assert.Contains("\"${FRONTEND_PORT:-8080}:8080\"", compose, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Environment_HasNoImageTagToChoose()
    {
        var environment = (await BuildAsync("0.1.0"))["bdtheque-0.1.0/.env.example"];

        Assert.DoesNotContain("BDTHEQUE_TAG", environment, StringComparison.Ordinal);
        Assert.DoesNotContain("Docker Hub", environment, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_PASSWORD=", environment, StringComparison.Ordinal);
        Assert.Contains("VISUELS_HOST_PATH=", environment, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Procedure_LinksToTheSourcesOfTheVersion()
    {
        // The procedure goes with one version: what it links to is the repository as that version left it.
        var procedure = (await BuildAsync("0.1.0"))["bdtheque-0.1.0/DEPLOIEMENT.md"];
        var links = RepositoryLink().Matches(procedure).Select(match => match.Value).ToList();

        Assert.NotEmpty(links);
        Assert.All(links, link => Assert.StartsWith("https://github.com/Tetram76/bdtheque-claude/blob/v0.1.0/", link, StringComparison.Ordinal));
        Assert.DoesNotContain("{{", procedure, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("v0.1.0")]
    [InlineData("0.1")]
    [InlineData("0.1.0-rc.1")]
    public async Task Package_IsRefused_ForWhatIsNotAVersionNumber(string version)
    {
        var result = await RunAsync(version);

        Assert.False(result.Succeeded, result.Output);
        Assert.Empty(Directory.GetFiles(_output));
    }

    public void Dispose()
    {
        Directory.Delete(_output, recursive: true);
        Directory.Delete(Path.GetDirectoryName(_notes)!, recursive: true);
    }

    private async Task<Dictionary<string, string>> BuildAsync(string version)
    {
        var result = await RunAsync(version);
        Assert.True(result.Succeeded, result.Output);

        using var archive = ZipFile.OpenRead(Path.Combine(_output, $"bdtheque-{version}.zip"));
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries.Where(entry => entry.Name.Length > 0))
        {
            using var reader = new StreamReader(entry.Open());
            entries[entry.FullName] = await reader.ReadToEndAsync();
        }

        return entries;
    }

    /// <summary>A published release, as the publication workflow lists them: one JSON object per line.</summary>
    private static string Release(string tag, bool prerelease = false) =>
        JsonSerializer.Serialize(new
        {
            tag,
            name = $"Titre {tag}",
            prerelease,
            body = $"Présentation de {tag}.\n\n## Compatibilité et montée de version\n\nCompatibilité de {tag}.\n\n## Changements\n\n### Consultation\n\n- Fiche de {tag}.\n",
        });

    private void WriteReleases(params string[] releases) => File.WriteAllLines(Releases, releases);

    [GeneratedRegex(@"^### Titre (?<tag>v[0-9.]+)", RegexOptions.Multiline)]
    private static partial Regex HistoryHeadings();

    [GeneratedRegex(@"https://github\.com/Tetram76/bdtheque-claude[^\s)]*")]
    private static partial Regex RepositoryLink();

    private Task<ProcessResult> RunAsync(string version) =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, Script), ["-Version", version, "-ReleaseNotes", _notes, "-PreviousReleases", Releases, "-OutputDirectory", _output], RepositoryFiles.Root,
            TimeSpan.FromMinutes(1));
}
