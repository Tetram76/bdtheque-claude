using System.Runtime.InteropServices;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// Comparison of a capture with its reference, pixel for pixel: the captures are taken in one fixed environment
/// (Linux, the Chromium of Playwright, the fonts of the application), where a same page gives the same pixels, so that
/// a difference is a change of the rendering. Only one unit of a component is let pass: the edges of a layer composited in
/// 3D (an album in volume, turned with a perspective) vary by that much from one run of the CI to the next, which no
/// setting of the browser corrects (choix-implementation/socle/themes.md, captures de référence).
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

    // Both images in one layout of their pixels, whatever their files hold: a pixel is then one 32-bit word, compared at once.
    private static readonly SKImageInfo Layout = new(1, 1, SKColorType.Bgra8888, SKAlphaType.Unpremul);

    public static CaptureComparison Compare(byte[] reference, byte[] actual)
    {
        using var expected = Decode(reference);
        using var captured = Decode(actual);

        if (expected.Width != captured.Width || expected.Height != captured.Height)
            return new CaptureComparison { SizeMismatch = $"{expected.Width}×{expected.Height} against {captured.Width}×{captured.Height}" };

        // Read in place, as whole buffers: a capture of a full page holds millions of pixels, which a call to the native
        // library for each one (GetPixel, SetPixel) takes seconds to go through.
        var expectedPixels = MemoryMarshal.Cast<byte, uint>(expected.GetPixelSpan());
        var capturedPixels = MemoryMarshal.Cast<byte, uint>(captured.GetPixelSpan());
        if (expectedPixels.SequenceEqual(capturedPixels))
            return new CaptureComparison();

        using var difference = new SKBitmap(expected.Info);
        var differencePixels = MemoryMarshal.Cast<byte, uint>(difference.GetPixelSpan());
        var mark = Word(DifferenceMark);
        var differentPixels = 0;
        for (var i = 0; i < expectedPixels.Length; i++)
        {
            if (Close(expectedPixels[i], capturedPixels[i]))
                differencePixels[i] = Faded(expectedPixels[i]);
            else
            {
                differencePixels[i] = mark;
                differentPixels++;
            }
        }

        if (differentPixels == 0)
            return new CaptureComparison();

        using var data = difference.Encode(SKEncodedImageFormat.Png, 100);
        return new CaptureComparison { DifferentPixels = differentPixels, Difference = data.ToArray() };
    }

    private static SKBitmap Decode(byte[] png)
    {
        using var data = SKData.CreateCopy(png);
        using var codec = SKCodec.Create(data)
            ?? throw new ArgumentException("Not an image the comparison can decode.", nameof(png));
        return SKBitmap.Decode(codec, Layout.WithSize(codec.Info.Width, codec.Info.Height));
    }

    // The same pixel, or one whose components differ by one unit at most.
    private static bool Close(uint expected, uint captured)
    {
        for (var shift = 0; shift < 32; shift += 8)
        {
            if (Math.Abs((int)(expected >> shift & 0xFF) - (int)(captured >> shift & 0xFF)) > 1)
                return false;
        }

        return true;
    }

    // A pixel of the layout above: blue, green, red, alpha, in the order of the bytes in memory.
    private static uint Word(SKColor color) =>
        BitConverter.ToUInt32([color.Blue, color.Green, color.Red, color.Alpha]);

    // Lightened toward white, so that the marks stand out over the page they locate.
    private static uint Faded(uint pixel)
    {
        Span<byte> bytes = stackalloc byte[4];
        BitConverter.TryWriteBytes(bytes, pixel);
        return Word(new SKColor((byte)(bytes[2] / 4 + 191), (byte)(bytes[1] / 4 + 191), (byte)(bytes[0] / 4 + 191)));
    }
}
