using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

[Trait("Category", RepositoryFiles.Category)]
public sealed partial class RuntimeImageTests
{
    [Fact]
    public void Dockerfiles_UseTheSameRuntimeImage()
    {
        // One runtime for every image, among them the loader of the validation data, which reuses the image of
        // `api` rather than pulling another one: a version upgrade must reach them all
        // (gestion-projet/depot/mises-a-jour-des-dependances.md).
        var images = Directory.GetFiles(Path.Combine(RepositoryFiles.Root, "docker"), "Dockerfile", SearchOption.AllDirectories)
            .SelectMany(file => RuntimeImage().Matches(File.ReadAllText(file))
                .Select(match => (File: Path.GetRelativePath(RepositoryFiles.Root, file), Image: match.Value)))
            .ToList();

        Assert.True(images.Count >= 3);
        Assert.All(images, found => Assert.Equal((found.File, images[0].Image), found));
    }

    [Fact]
    public void Workflows_RunOnTheUbuntuOfTheRuntimeImage()
    {
        // The tests of the French order and formatting run on the ICU of the runner, not on that of the images: they vouch
        // for the images only on the same Ubuntu release, whose ICU is the same; `ubuntu-latest` would move on its own
        // (choix-implementation/outillage.md).
        var dockerfile = RepositoryFiles.Read(Path.Combine("docker", "api", "Dockerfile"));
        var codename = RuntimeImage().Match(dockerfile).Groups["codename"].Value;
        Assert.True(UbuntuReleases.TryGetValue(codename, out var release), $"Ubuntu release of '{codename}' unknown to the test.");

        var runners = Directory.GetFiles(Path.Combine(RepositoryFiles.Root, ".github", "workflows"), "*.yml")
            .SelectMany(file => Runner().Matches(File.ReadAllText(file))
                .Select(match => (File: Path.GetRelativePath(RepositoryFiles.Root, file), Runner: match.Groups["runner"].Value)))
            .ToList();

        Assert.NotEmpty(runners);
        Assert.All(runners, found => Assert.Equal((found.File, $"ubuntu-{release}"), found));
    }

    private static readonly Dictionary<string, string> UbuntuReleases = new() { ["noble"] = "24.04", ["resolute"] = "26.04" };

    [GeneratedRegex(@"mcr\.microsoft\.com/dotnet/aspnet:[^\s-]+-(?<codename>[a-z]+)\S*")]
    private static partial Regex RuntimeImage();

    [GeneratedRegex(@"runs-on:\s*(?<runner>\S+)")]
    private static partial Regex Runner();
}
