using Bdtheque.Api.Visuals;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The checks of the visuals volume at startup: nothing in the deployment guarantees that <c>api</c>
/// can write to it, nor that it is mounted outside the container.
/// </summary>
public sealed class VisualVolumeTests
{
    [Fact]
    public void EnsureWritable_CreatesTheFoldersOfAWritableVolume()
    {
        var root = Path.Combine(Path.GetTempPath(), $"bdtheque-volume-{Guid.NewGuid():N}");
        try
        {
            StorageAt(root).EnsureWritable();

            Assert.True(Directory.Exists(Path.Combine(root, "originals")));
            Assert.True(Directory.Exists(Path.Combine(root, "display")));
            // The probe written to check the access is not left behind.
            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnsureWritable_VolumeThatCannotBeWritten_RefusesToStart()
    {
        // A file where the volume should be: its folders cannot be created, as on a volume the user
        // of the container has no right to write to.
        var root = Path.GetTempFileName();
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => StorageAt(root).EnsureWritable());

            Assert.Contains(root, exception.Message);
        }
        finally
        {
            File.Delete(root);
        }
    }

    [Fact]
    public void MountPoints_AreReadFromMountInfoAndUnescaped()
    {
        string[] mountInfo =
        [
            "512 470 0:51 / / rw,relatime master:1 - overlay overlay rw,lowerdir=/a",
            "531 512 0:66 /volume1/BD /app/visuels rw,relatime - ext4 /dev/md2 rw",
            "540 512 0:70 /data /mnt/my\\040scans rw - ext4 /dev/md2 rw",
        ];

        Assert.Equal(["/", "/app/visuels", "/mnt/my scans"], MountInfo.MountPoints(mountInfo));
    }

    [Theory]
    [InlineData("/app/visuels", true)]
    [InlineData("/app/visuels/", true)]
    [InlineData("/app/visuels/covers", true)]
    [InlineData("/app/visuels2", false)]
    [InlineData("/app", false)]
    public void IsOnMount_IgnoresTheRootOfTheContainer(string path, bool expected)
    {
        string[] mountPoints = ["/", "/etc/hosts", "/app/visuels"];

        Assert.Equal(expected, MountInfo.IsOnMount(path, mountPoints));
    }

    private static VisualStorage StorageAt(string root) =>
        new(Options.Create(new VisualStorageOptions { RootPath = root }), NullLogger<VisualStorage>.Instance);
}
