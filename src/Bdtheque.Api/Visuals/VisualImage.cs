using Bdtheque.Domain.Common;
using SkiaSharp;

namespace Bdtheque.Api.Visuals;

/// <summary>An uploaded visual once validated: its original, kept as is, and its reduced display version.</summary>
/// <param name="OriginalExtension">Extension of the original, from its decoded format, never from the client's file name.</param>
/// <param name="Display">The display version, in WebP.</param>
internal sealed record PreparedVisual(byte[] Original, string OriginalExtension, byte[] Display);

/// <summary>
/// Validation and conversion of an uploaded visual (choix-implementation.md § Visuels : stockage et
/// traitement), entirely in memory, so that nothing is written for a file that is refused.
/// </summary>
internal static class VisualImage
{
    /// <summary>
    /// Longest edge of the display version: enough for a cover shown in full on a large screen,
    /// where the scanned originals weigh several megabytes, too much for a grid on a mobile network.
    /// </summary>
    public const int DisplayMaxEdge = 1200;

    private const int DisplayQuality = 80;

    // Formats every browser shows, the original being served as is; the extension follows the
    // decoded format.
    private static readonly Dictionary<SKEncodedImageFormat, string> SupportedFormats = new()
    {
        [SKEncodedImageFormat.Jpeg] = "jpg",
        [SKEncodedImageFormat.Png] = "png",
        [SKEncodedImageFormat.Webp] = "webp",
        [SKEncodedImageFormat.Gif] = "gif",
    };

    /// <summary>Refuses a file heavier than <paramref name="maxBytes"/>, before it is even read.</summary>
    public static void EnsureSize(long length, long maxBytes)
    {
        if (length > maxBytes)
            throw new DomainRuleViolationException(
                DomainRules.EditionVisualFileTooLarge, $"The file weighs {length} bytes, more than the {maxBytes} accepted.");
    }

    /// <summary>
    /// Decodes <paramref name="content"/> as an image — never trusting the type the client announced —
    /// and produces its display version.
    /// </summary>
    /// <exception cref="DomainRuleViolationException">The file is not a complete image of a supported format.</exception>
    public static PreparedVisual Prepare(byte[] content)
    {
        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data)
                          ?? throw NotSupportedImage("The file is not an image.");
        if (!SupportedFormats.TryGetValue(codec.EncodedFormat, out var extension))
            throw NotSupportedImage($"Images in {codec.EncodedFormat} are not accepted.");

        using var decoded = Decode(codec);
        using var oriented = Orient(decoded, codec.EncodedOrigin);
        using var display = Reduce(oriented);
        using var image = SKImage.FromBitmap(display);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, DisplayQuality);
        return new PreparedVisual(content, extension, encoded.ToArray());
    }

    /// <summary>
    /// Decodes the whole image, at the smallest scale the codec offers that still covers the display
    /// version (JPEG and WebP decode natively at a reduced scale, sparing the memory of a full scan).
    /// </summary>
    private static SKBitmap Decode(SKCodec codec)
    {
        var size = codec.Info.Size;
        var scale = Math.Min(1f, (float)DisplayMaxEdge / Math.Max(size.Width, size.Height));
        var info = codec.Info.WithSize(codec.GetScaledDimensions(scale)).WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Premul);

        var bitmap = new SKBitmap(info);
        // Anything short of a full decoding (e.g. a truncated file) refuses the file: its display
        // version would show a partial image.
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            bitmap.Dispose();
            throw NotSupportedImage("The image could not be decoded in full.");
        }

        return bitmap;
    }

    /// <summary>
    /// Applies the orientation the file declares (EXIF), as a phone stores a portrait photo as a
    /// landscape image with an orientation tag: the display version is shown upright.
    /// </summary>
    private static SKBitmap Orient(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        float w = bitmap.Width, h = bitmap.Height;
        // Maps the pixels of the stored image onto the upright one (x' = ScaleX·x + SkewX·y + TransX,
        // y' = SkewY·x + ScaleY·y + TransY).
        SKMatrix? matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => null,
        };
        if (matrix is null)
            return bitmap.Copy();

        // The last four origins swap width and height.
        var oriented = origin >= SKEncodedOrigin.LeftTop
            ? new SKBitmap(bitmap.Info.WithSize(bitmap.Height, bitmap.Width))
            : new SKBitmap(bitmap.Info);
        using var canvas = new SKCanvas(oriented);
        canvas.SetMatrix(matrix.Value);
        canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);
        return oriented;
    }

    /// <summary>Bounds the longest edge to <see cref="DisplayMaxEdge"/>, never enlarging a smaller image.</summary>
    private static SKBitmap Reduce(SKBitmap bitmap)
    {
        var longest = Math.Max(bitmap.Width, bitmap.Height);
        if (longest <= DisplayMaxEdge)
            return bitmap.Copy();

        var ratio = (double)DisplayMaxEdge / longest;
        var size = new SKSizeI(
            Math.Max(1, (int)Math.Round(bitmap.Width * ratio)), Math.Max(1, (int)Math.Round(bitmap.Height * ratio)));
        return bitmap.Resize(size, new SKSamplingOptions(SKCubicResampler.Mitchell))
               ?? throw new InvalidOperationException("The display version could not be produced.");
    }

    private static DomainRuleViolationException NotSupportedImage(string reason) =>
        new(DomainRules.EditionVisualFileNotSupportedImage, reason);
}
