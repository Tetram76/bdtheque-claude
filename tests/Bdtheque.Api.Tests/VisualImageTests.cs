using Bdtheque.Api.Visuals;
using Bdtheque.Domain.Common;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Validation and conversion of an uploaded visual, in memory, before anything is written
/// (choix-implementation.md § Visuels : stockage et traitement).
/// </summary>
public sealed class VisualImageTests
{
    [Fact]
    public void Prepare_KeepsTheOriginalAndReducesTheDisplayVersionToWebp()
    {
        var jpeg = TestImages.Encode(2400, 1600, SKEncodedImageFormat.Jpeg);

        var prepared = VisualImage.Prepare(jpeg);

        Assert.Equal("jpg", prepared.OriginalExtension);
        Assert.Equal(jpeg, prepared.Original);
        using var codec = SKCodec.Create(new SKMemoryStream(prepared.Display));
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal(new SKSizeI(VisualImage.DisplayMaxEdge, 800), codec.Info.Size);
    }

    [Fact]
    public void Prepare_SmallImage_IsNeverEnlarged()
    {
        var prepared = VisualImage.Prepare(TestImages.Encode(300, 200, SKEncodedImageFormat.Png));

        Assert.Equal("png", prepared.OriginalExtension);
        Assert.Equal(new SKSizeI(300, 200), TestImages.SizeOf(prepared.Display));
    }

    [Fact]
    public void Prepare_PortraitImage_BoundsItsHeight()
    {
        var prepared = VisualImage.Prepare(TestImages.Encode(1000, 3000, SKEncodedImageFormat.Webp));

        Assert.Equal("webp", prepared.OriginalExtension);
        Assert.Equal(new SKSizeI(400, VisualImage.DisplayMaxEdge), TestImages.SizeOf(prepared.Display));
    }

    [Fact]
    public void Prepare_RotatedPhoto_StraightensTheDisplayVersion()
    {
        // A phone stores a portrait photo as a landscape image with an EXIF orientation (6: rotate
        // 90° clockwise); the display version must show it upright. The original is kept as is.
        var jpeg = TestImages.WithExifOrientation(TestImages.EncodeHalves(200, 100, SKColors.Red, SKColors.Blue), 6);

        var prepared = VisualImage.Prepare(jpeg);

        Assert.Equal(jpeg, prepared.Original);
        using var display = SKBitmap.Decode(prepared.Display);
        Assert.Equal(new SKSizeI(100, 200), new SKSizeI(display.Width, display.Height));
        // Rotated clockwise, the left half of the stored image becomes the top half.
        Assert.True(display.GetPixel(50, 40).Red > 200 && display.GetPixel(50, 40).Blue < 60);
        Assert.True(display.GetPixel(50, 160).Blue > 200 && display.GetPixel(50, 160).Red < 60);
    }

    public static TheoryData<string, byte[]> NotSupportedImages => new()
    {
        { "empty", [] },
        { "text", "not an image"u8.ToArray() },
        { "truncated JPEG", TestImages.Encode(400, 300, SKEncodedImageFormat.Jpeg)[..600] },
        { "BMP", TestImages.Bitmap1x1() },
    };

    [Theory]
    [MemberData(nameof(NotSupportedImages))]
    public void Prepare_NotASupportedImage_IsABusinessError(string _, byte[] content)
    {
        var violation = Assert.Throws<DomainRuleViolationException>(() => VisualImage.Prepare(content));

        Assert.Equal(DomainRules.EditionVisualFileNotSupportedImage, violation.Rule);
    }

    [Fact]
    public void EnsureSize_AboveTheMaximum_IsABusinessError()
    {
        VisualImage.EnsureSize(1000, maxBytes: 1000);

        var violation = Assert.Throws<DomainRuleViolationException>(() => VisualImage.EnsureSize(1001, maxBytes: 1000));

        Assert.Equal(DomainRules.EditionVisualFileTooLarge, violation.Rule);
    }
}
