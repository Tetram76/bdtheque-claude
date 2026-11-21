using System.IO.Compression;
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

    public DeploymentPackageTests() => File.WriteAllText(_notes, ReleaseNotes);

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
    public async Task Package_HoldsTheNotesOfTheRelease_AsPublished()
    {
        var notes = (await BuildAsync("0.1.0"))["bdtheque-0.1.0/NOTES-DE-VERSION.md"];

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

    [GeneratedRegex(@"https://github\.com/Tetram76/bdtheque-claude[^\s)]*")]
    private static partial Regex RepositoryLink();

    private Task<ProcessResult> RunAsync(string version) =>
        Processes.PowerShellAsync(
            Path.Combine(RepositoryFiles.Root, Script), ["-Version", version, "-ReleaseNotes", _notes, "-OutputDirectory", _output], RepositoryFiles.Root,
            TimeSpan.FromMinutes(1));
}
