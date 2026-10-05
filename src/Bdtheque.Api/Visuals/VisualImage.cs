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

    /// <summary>
    /// Most pixels decoded for one visual (about 320 MB in memory): an A3 page scanned at 600 dpi still
    /// fits. Only PNG and GIF can reach it, JPEG and WebP decoding at a reduced scale.
    /// </summary>
    public const long MaxDecodedPixels = 80_000_000;

    // Formats every browser shows, the original being served as is; the extension follows the
    // decoded format.
    private static readonly Dictionary<SKEncodedImageFormat, string> SupportedFormats = new()
    {
        [SKEncodedImageFormat.Jpeg] = "jpg",
        [SKEncodedImageFormat.Png] = "png",
        [SKEncodedImageFormat.Webp] = "webp",
        [SKEncodedImageFormat.Gif] = "gif",
    };

    /// <summary>The refusal of a file heavier than <paramref name="maxBytes"/>, raised as soon as its reading exceeds them.</summary>
    public static DomainRuleViolationException FileTooLarge(long maxBytes) =>
        new(DomainRules.EditionVisualFileTooLarge, $"The file weighs more than the {maxBytes} bytes accepted.");

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

        // Reduced before being straightened, the long edge being the same either way: only the decoded
        // bitmap is ever at full size. Each step returns its input when it has nothing to do.
        var display = Decode(codec);
        try
        {
            display = Replace(display, Reduce(display));
            display = Replace(display, Orient(display, codec.EncodedOrigin));
            using var image = SKImage.FromBitmap(display);
            using var encoded = image.Encode(SKEncodedImageFormat.Webp, DisplayQuality);
            return new PreparedVisual(content, extension, encoded.ToArray());
        }
        finally
        {
            display.Dispose();
        }
    }

    /// <summary>
    /// Decodes the whole image, at the smallest scale the codec offers that still covers the display
    /// version (JPEG and WebP decode natively at a reduced scale, sparing the memory of a full scan).
    /// </summary>
    /// <exception cref="DomainRuleViolationException">
    /// The image would decode to more than <see cref="MaxDecodedPixels"/> pixels, or is incomplete.
    /// </exception>
    private static SKBitmap Decode(SKCodec codec)
    {
        var size = codec.Info.Size;
        var scale = Math.Min(1f, (float)DisplayMaxEdge / Math.Max(size.Width, size.Height));
        var info = codec.Info.WithSize(codec.GetScaledDimensions(scale)).WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Premul);

        // Checked before allocating: a PNG or a GIF, which have no reduced-scale decoding, are decoded
        // at full size whatever the weight of their file.
        if ((long)info.Width * info.Height > MaxDecodedPixels)
            throw new DomainRuleViolationException(
                DomainRules.EditionVisualImageDimensionsTooLarge,
                $"The image would decode to {info.Width}×{info.Height} pixels, more than the {MaxDecodedPixels} accepted.");

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
            return bitmap;

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
            return bitmap;

        var ratio = (double)DisplayMaxEdge / longest;
        var size = new SKSizeI(
            Math.Max(1, (int)Math.Round(bitmap.Width * ratio)), Math.Max(1, (int)Math.Round(bitmap.Height * ratio)));
        return bitmap.Resize(size, new SKSamplingOptions(SKCubicResampler.Mitchell))
               ?? throw new InvalidOperationException("The display version could not be produced.");
    }

    // Disposes the bitmap a step replaced, keeping the one it returned.
    private static SKBitmap Replace(SKBitmap current, SKBitmap next)
    {
        if (!ReferenceEquals(current, next))
            current.Dispose();
        return next;
    }

    private static DomainRuleViolationException NotSupportedImage(string reason) =>
        new(DomainRules.EditionVisualFileNotSupportedImage, reason);
}
