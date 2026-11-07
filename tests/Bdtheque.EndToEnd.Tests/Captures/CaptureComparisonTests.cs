using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests.Captures;

public sealed class CaptureComparisonTests
{
    [Fact]
    public void SameImages_AreIdentical()
    {
        var image = Png(4, 3, SKColors.White);

        var comparison = CaptureComparison.Compare(image, image);

        Assert.True(comparison.Identical);
        Assert.Equal(0, comparison.DifferentPixels);
        Assert.Null(comparison.Difference);
    }

    [Fact]
    public void ImagesThatDifferByOnePixel_AreDifferent_AndTheDifferenceMarksIt()
    {
        var reference = Png(4, 3, SKColors.White);
        var actual = Png(4, 3, SKColors.White, (2, 1, SKColors.Black));

        var comparison = CaptureComparison.Compare(reference, actual);

        Assert.False(comparison.Identical);
        Assert.Equal(1, comparison.DifferentPixels);
        using var difference = SKBitmap.Decode(comparison.Difference);
        Assert.Equal(CaptureComparison.DifferenceMark, difference.GetPixel(2, 1));
        Assert.NotEqual(CaptureComparison.DifferenceMark, difference.GetPixel(0, 0));
    }

    // Every pixel is compared, the last one included, and the image of the differences shows the reference faded elsewhere.
    [Fact]
    public void ImagesThatDifferByTheirLastPixel_AreDifferent_OverTheReferenceFaded()
    {
        var reference = Png(4, 3, SKColors.Black);
        var actual = Png(4, 3, SKColors.Black, (3, 2, SKColors.White));

        var comparison = CaptureComparison.Compare(reference, actual);

        Assert.Equal(1, comparison.DifferentPixels);
        using var difference = SKBitmap.Decode(comparison.Difference);
        Assert.Equal(CaptureComparison.DifferenceMark, difference.GetPixel(3, 2));
        Assert.Equal(new SKColor(191, 191, 191), difference.GetPixel(0, 0));
    }

    [Fact]
    public void ImagesOfDifferentSizes_AreDifferent()
    {
        var comparison = CaptureComparison.Compare(Png(4, 3, SKColors.White), Png(4, 5, SKColors.White));

        Assert.False(comparison.Identical);
        Assert.Equal("4×3 against 4×5", comparison.SizeMismatch);
    }

    private static byte[] Png(int width, int height, SKColor background, params (int X, int Y, SKColor Color)[] pixels)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(background);
        foreach (var (x, y, color) in pixels)
            bitmap.SetPixel(x, y, color);

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
