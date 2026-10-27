using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

public sealed partial class RuntimeImageTests
{
    [Fact]
    public void Dockerfiles_UseTheSameRuntimeImage()
    {
        // One runtime for every image, among them the loader of the validation data, which reuses the image of
        // `api` rather than pulling another one: a version upgrade must reach them all (gestion-projet/depot.md
        // § Mises à jour des dépendances).
        var images = Directory.GetFiles(Path.Combine(RepositoryFiles.Root, "docker"), "Dockerfile", SearchOption.AllDirectories)
            .SelectMany(file => RuntimeImage().Matches(File.ReadAllText(file))
                .Select(match => (File: Path.GetRelativePath(RepositoryFiles.Root, file), Image: match.Value)))
            .ToList();

        Assert.True(images.Count >= 3);
        Assert.All(images, found => Assert.Equal((found.File, images[0].Image), found));
    }

    [GeneratedRegex(@"mcr\.microsoft\.com/dotnet/aspnet:\S+")]
    private static partial Regex RuntimeImage();
}
