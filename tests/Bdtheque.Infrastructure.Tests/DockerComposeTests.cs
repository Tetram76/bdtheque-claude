using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The NAS runs several versions side by side, each as a Compose project of its own, from the
/// docker-compose.yml of its version: every version of the file must let the others run beside it
/// (choix-implementation/deploiement/plusieurs-versions-sur-le-nas.md).
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed partial class DockerComposeTests
{
    private static readonly string Compose = RepositoryFiles.Read("docker-compose.yml");

    // db itself and its only caller, in order.
    private static readonly string[] DbNetworkMembers = ["api", "db"];

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

    [Fact]
    public void PortsOfTheValidation_AreOpenedToTheMachineOnly()
    {
        // The validation environment runs on the machine of the user, with fixed and fictitious secrets, and serves what
        // production never exposes (api and its documentation, the technical documentation): no port of it may be reached
        // from another machine (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md).
        var ports = ValidationPort().Matches(RepositoryFiles.Read("docker/validation/compose.yaml"))
            .Select(match => match.Groups["port"].Value).ToList();

        Assert.NotEmpty(ports);
        Assert.All(ports, port => Assert.StartsWith("127.0.0.1:", port));
    }

    [Fact]
    public void Networks_AreNamedAfterTheServiceTheyGiveAccessTo()
    {
        // One naming rule for every network, so that a network says which container it protects and joining it says
        // that a service calls that container (choix-implementation/deploiement/reseaux-des-conteneurs.md).
        var services = ServiceNetworks(Compose);
        var networks = DeclaredNetworks(Compose);

        Assert.NotEmpty(networks);
        Assert.All(networks, network =>
        {
            Assert.True(services.ContainsKey(network), $"Network '{network}' is named after no service.");
            Assert.Contains(network, services[network]);
        });
    }

    [Fact]
    public void Db_IsReachedByApiOnly()
    {
        // contraintes-techniques/architecture-deploiement/architecture-des-tiers.md: api alone accesses db.
        var members = ServiceNetworks(Compose).Where(service => service.Value.Contains("db")).Select(service => service.Key);

        Assert.Equal(DbNetworkMembers, members.Order());
    }

    [Fact]
    public void NetworksOfTheValidation_AreThoseOfTheDeployment()
    {
        // The validation environment overrides docker-compose.yml: its services join the networks of the deployment,
        // under the same rule, and declare none of their own.
        var validation = RepositoryFiles.Read("docker/validation/compose.yaml");
        var joined = ServiceNetworks(validation).SelectMany(service => service.Value).ToList();

        Assert.NotEmpty(joined);
        Assert.Empty(DeclaredNetworks(validation));
        Assert.All(joined, network => Assert.Contains(network, DeclaredNetworks(Compose)));
    }

    /// <summary>The networks of the top-level <c>networks</c> element.</summary>
    private static List<string> DeclaredNetworks(string compose) =>
        TopLevelElement(compose, "networks") is { } element
            ? [.. TopLevelKey().Matches(element).Select(match => match.Groups["key"].Value)]
            : [];

    /// <summary>Each service of the top-level <c>services</c> element, with the networks it joins.</summary>
    private static Dictionary<string, List<string>> ServiceNetworks(string compose) =>
        ServiceBlock().Matches(TopLevelElement(compose, "services") ?? "").ToDictionary(
            match => match.Groups["service"].Value,
            match => JoinedNetworks().Matches(match.Groups["body"].Value)
                .SelectMany(networks => networks.Groups["network"].Captures.Select(capture => capture.Value))
                .ToList());

    /// <summary>The indented content of a top-level element, up to the next one.</summary>
    private static string? TopLevelElement(string compose, string name) =>
        TopLevel().Matches(compose).FirstOrDefault(match => match.Groups["name"].Value == name)?.Groups["body"].Value;

    [GeneratedRegex(@"^(?<name>[a-z]+):[ \t]*\r?\n(?<body>(?:(?:[ \t]+.*|[ \t]*)(?:\r?\n|$))*)", RegexOptions.Multiline)]
    private static partial Regex TopLevel();

    [GeneratedRegex(@"^  (?<key>[\w-]+):", RegexOptions.Multiline)]
    private static partial Regex TopLevelKey();

    [GeneratedRegex(@"^  (?<service>[\w-]+):[ \t]*\r?\n(?<body>(?:(?:    .*|[ \t]*)(?:\r?\n|$))*)", RegexOptions.Multiline)]
    private static partial Regex ServiceBlock();

    [GeneratedRegex(@"^    networks:[ \t]*\r?\n(?:      -[ \t]+(?<network>[\w-]+)[ \t]*(?:\r?\n|$))+", RegexOptions.Multiline)]
    private static partial Regex JoinedNetworks();

    [GeneratedRegex(@"^\s+-\s+""(?<port>[^""]+:\d+)""\s*$", RegexOptions.Multiline)]
    private static partial Regex ValidationPort();

    [GeneratedRegex(@"^\s*(name|container_name):", RegexOptions.Multiline)]
    private static partial Regex FixedName();

    [GeneratedRegex(@"^\s+-\s+""(?<host>[^""]+):\d+""\s*$", RegexOptions.Multiline)]
    private static partial Regex PublishedPort();

    [GeneratedRegex(@"^\s+-\s+(?<source>[^\s""]+):/", RegexOptions.Multiline)]
    private static partial Regex Mount();

    [GeneratedRegex(@"^(\$\{|[A-Za-z0-9][\w-]*$)")]
    private static partial Regex ProjectScopedSource();
}
