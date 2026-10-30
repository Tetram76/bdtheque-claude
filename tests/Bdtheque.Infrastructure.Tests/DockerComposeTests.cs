using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The NAS runs several versions side by side, each as a Compose project of its own, from the same
/// docker-compose.yml (choix-implementation/deploiement.md § Plusieurs versions sur le NAS).
/// </summary>
public sealed partial class DockerComposeTests
{
    private static readonly string Compose = RepositoryFiles.Read("docker-compose.yml");

    [Fact]
    public void Names_AreLeftToTheProject()
    {
        // A fixed project, container, network or volume name would be shared by all the projects:
        // the second one would take over the containers, or the database, of the first.
        Assert.DoesNotMatch(FixedName(), Compose);
    }

    [Fact]
    public void PublishedPorts_AreSetByEachDeployment()
    {
        var hostPorts = PublishedPort().Matches(Compose).Select(match => match.Groups["host"].Value).ToList();

        Assert.NotEmpty(hostPorts);
        Assert.All(hostPorts, host => Assert.StartsWith("${", host));
    }

    [Fact]
    public void MountedData_BelongsToEachDeployment()
    {
        // A named volume is scoped by the project; a host folder is chosen by each deployment. A shared
        // visuals folder would be emptied by the reconciliation of the other versions.
        var sources = Mount().Matches(Compose).Select(match => match.Groups["source"].Value).ToList();

        Assert.NotEmpty(sources);
        Assert.All(sources, source => Assert.Matches(ProjectScopedSource(), source));
    }

    [GeneratedRegex(@"^\s*(name|container_name):", RegexOptions.Multiline)]
    private static partial Regex FixedName();

    [GeneratedRegex(@"^\s+-\s+""(?<host>[^""]+):\d+""\s*$", RegexOptions.Multiline)]
    private static partial Regex PublishedPort();

    [GeneratedRegex(@"^\s+-\s+(?<source>[^\s""]+):/", RegexOptions.Multiline)]
    private static partial Regex Mount();

    [GeneratedRegex(@"^(\$\{|[A-Za-z0-9][\w-]*$)")]
    private static partial Regex ProjectScopedSource();
}
