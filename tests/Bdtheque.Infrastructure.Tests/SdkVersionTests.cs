using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

public sealed partial class SdkVersionTests
{
    [Fact]
    public void Workflows_UseTheSdkOfTheDockerfiles()
    {
        // The CI vouches for the images only if it builds with their SDK; global.json, a minimum for
        // the development machines, cannot carry that exact version (gestion-projet/depot.md § Mises
        // à jour des dépendances).
        var dockerfiles = Directory.GetFiles(Path.Combine(RepositoryFiles.Root, "docker"), "Dockerfile", SearchOption.AllDirectories);
        var workflows = Directory.GetFiles(Path.Combine(RepositoryFiles.Root, ".github", "workflows"), "*.yml");

        var imageVersions = dockerfiles.SelectMany(file => Versions(SdkImage(), file)).ToList();
        var workflowVersions = workflows.SelectMany(file => Versions(WorkflowSdk(), file)).ToList();

        Assert.NotEmpty(imageVersions);
        Assert.NotEmpty(workflowVersions);
        var expected = imageVersions[0].Version;
        Assert.All(imageVersions.Concat(workflowVersions), found => Assert.Equal((found.File, expected), found));
    }

    private static IEnumerable<(string File, string Version)> Versions(Regex pattern, string file) =>
        pattern.Matches(File.ReadAllText(file))
            .Select(match => (Path.GetRelativePath(RepositoryFiles.Root, file), match.Groups["version"].Value));

    [GeneratedRegex(@"mcr\.microsoft\.com/dotnet/sdk:(?<version>[^\s-]+)")]
    private static partial Regex SdkImage();

    [GeneratedRegex(@"dotnet-version:\s*""?(?<version>[^\s""]+)")]
    private static partial Regex WorkflowSdk();
}
