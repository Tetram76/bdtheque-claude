using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// Comparison of a capture with its reference, pixel for pixel: the captures are taken in one fixed environment
/// (Linux, the Chromium of Playwright, the fonts of the application), where a same page always gives the same pixels,
/// so that any difference is a change of the rendering.
/// </summary>
public sealed record CaptureComparison
{
    /// <summary>Paints the pixels that differ on the difference image, which shows the reference faded underneath.</summary>
    public static readonly SKColor DifferenceMark = new(255, 0, 255);

    public bool Identical => DifferentPixels == 0 && SizeMismatch is null;

    public int DifferentPixels { get; private init; }

    /// <summary>The two sizes, when they differ: no pixel is then compared.</summary>
    public string? SizeMismatch { get; private init; }

    /// <summary>The difference image (PNG), when the images differ in some pixels.</summary>
    public byte[]? Difference { get; private init; }

    public static CaptureComparison Compare(byte[] reference, byte[] actual)
    {
        using var expected = SKBitmap.Decode(reference);
        using var captured = SKBitmap.Decode(actual);

        if (expected.Width != captured.Width || expected.Height != captured.Height)
            return new CaptureComparison { SizeMismatch = $"{expected.Width}×{expected.Height} against {captured.Width}×{captured.Height}" };

        using var difference = new SKBitmap(expected.Width, expected.Height);
        var differentPixels = 0;
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                var pixel = expected.GetPixel(x, y);
                if (pixel == captured.GetPixel(x, y))
                    difference.SetPixel(x, y, Faded(pixel));
                else
                {
                    difference.SetPixel(x, y, DifferenceMark);
                    differentPixels++;
                }
            }
        }

        if (differentPixels == 0)
            return new CaptureComparison();

        using var data = difference.Encode(SKEncodedImageFormat.Png, 100);
        return new CaptureComparison { DifferentPixels = differentPixels, Difference = data.ToArray() };
    }

    // Lightened toward white, so that the marks stand out over the page they locate.
    private static SKColor Faded(SKColor pixel) =>
        new((byte)(pixel.Red / 4 + 191), (byte)(pixel.Green / 4 + 191), (byte)(pixel.Blue / 4 + 191));
}
