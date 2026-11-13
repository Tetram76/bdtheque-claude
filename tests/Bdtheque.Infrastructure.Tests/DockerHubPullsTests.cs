using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// What the images and the workflows that build them download from Docker Hub, whose pull quota fails the CI
/// whatever a pull request holds (gestion-projet/depot/integration-continue-ci.md, quotas consommés par le CI).
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed partial class DockerHubPullsTests
{
    [Fact]
    public void Dockerfiles_PullNoImageFromDockerHub()
    {
        // A syntax directive makes BuildKit pull its Dockerfile frontend from Docker Hub at each build, under the
        // account the workflow logged in with; the frontend built into BuildKit reads these Dockerfiles as well.
        var dockerfiles = Directory.GetFiles(Path.Combine(RepositoryFiles.Root, "docker"), "Dockerfile", SearchOption.AllDirectories);

        Assert.NotEmpty(dockerfiles);
        Assert.All(dockerfiles, file =>
        {
            var content = File.ReadAllText(file);
            var name = Path.GetRelativePath(RepositoryFiles.Root, file);
            Assert.False(SyntaxDirective().IsMatch(content), $"{name} declares a syntax directive.");

            var stages = StageName().Matches(content).Select(match => match.Groups["name"].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.All(
                FromImage().Matches(content).Select(match => match.Groups["image"].Value).Where(image => !stages.Contains(image)),
                image => Assert.StartsWith("mcr.microsoft.com/", image, StringComparison.Ordinal));
        });
    }

    [Theory]
    [InlineData(".github/workflows/ci.yml")]
    [InlineData(".github/workflows/docker-publish.yml")]
    public void ImageAnalyses_ReadNoPublishedImage(string workflow)
    {
        // The analysis of a built image compares it with no published one: each comparison pulled that image under
        // the account (choix-implementation/deploiement/publication-des-images-docker.md).
        var content = RepositoryFiles.Read(workflow);

        Assert.Contains("docker/scout-action@", content, StringComparison.Ordinal);
        Assert.DoesNotContain("registry://", content, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*#\s*syntax\s*=", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SyntaxDirective();

    [GeneratedRegex(@"^\s*FROM\s+(?:--\S+\s+)*(?<image>\S+)", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex FromImage();

    [GeneratedRegex(@"^\s*FROM\s+.*\s+AS\s+(?<name>\S+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex StageName();
}
