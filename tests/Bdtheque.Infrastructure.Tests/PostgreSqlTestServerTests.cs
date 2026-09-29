using System.Text.RegularExpressions;
using Bdtheque.Testing;

namespace Bdtheque.Infrastructure.Tests;

public sealed partial class PostgreSqlTestServerTests
{
    [Fact]
    public void Image_MatchesDockerComposeDbService()
    {
        // The persistence tests only vouch for the engine they run on: an image bump in
        // docker-compose.yml that the tests don't follow would leave production untested.
        var compose = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docker-compose.yml"));

        var composeImage = DbServiceImage().Match(compose);

        Assert.True(composeImage.Success, "No 'image:' line found for the db service in docker-compose.yml.");
        Assert.Equal(PostgreSqlTestServer.Image, composeImage.Groups["image"].Value);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bdtheque.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Bdtheque.slnx) not found.");
    }

    [GeneratedRegex(@"^  db:\s*\n    image:\s*(?<image>\S+)", RegexOptions.Multiline)]
    private static partial Regex DbServiceImage();
}
