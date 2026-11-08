using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using SkiaSharp;

namespace Bdtheque.Api.Visuals;

/// <summary>
/// Draws from an image its three colours (visuel/dashboard/la-couverture-du-jour.md): computed once, at upload, from
/// the display version (choix-implementation/domaine/visuels-stockage-et-traitement.md).
/// </summary>
/// <remarks>
/// The pixels are counted by colour, close colours together; the colours are then grouped by hue, so that the
/// many shades of a drawn background count as one colour. A frank colour wins over the paper and the ink (white,
/// black, greys), which a page painted with would show nothing of the cover; only a cover without any keeps its greys.
/// No hue is ever invented: a cover with a single hue has an accent of that hue.
/// </remarks>
internal static class VisualColors
{
    // About as many pixels as a 100×100 thumbnail: enough to weigh the colours of a cover, whatever its size.
    private const int TargetSamples = 10_000;

    // Pixels told apart by their 4 high bits per channel: 4,096 colours, the shades of a flat colour together.
    private const int ChannelShift = 4;

    // Chroma (max − min of the channels) from which a colour is frank rather than a grey: white paper, black ink
    // and their greys stay below it, as do the faded tints of a yellowed paper.
    private const double FrankChroma = 0.2;

    // Share of the pixels from which the frank colours prevail over the greys: below, a few spots on a black and
    // white cover.
    private const double FrankShare = 0.10;

    // Share of the pixels a colour needs to be an accent: below, noise (a logo, a sticker, the edge of a scan).
    private const double AccentShare = 0.01;

    // Width of a family of hues, and least distance between the hues of the dominant and of the accent.
    private const double HueFamily = 30;
    private const double AccentHueDistance = 45;

    // Lightness of the deep colour at most: dark enough for white text to reach 4.5:1 whatever the hue (yellow,
    // the brightest at a given lightness, reaches 5.2:1 at full saturation).
    private const double DeepLightness = 0.22;

    // Lightness of the accent of a cover without another hue, on the other side of its dominant.
    private const double LightAccent = 0.72;
    private const double DarkAccent = 0.32;

    /// <summary>
    /// The palette of <paramref name="image"/>, whose fully transparent pixels are ignored — unless it has no other, an
    /// accepted image all the same.
    /// </summary>
    public static VisualPalette PaletteOf(SKBitmap image)
    {
        var colors = CountColors(image, ignoreTransparent: true);
        if (colors.Count == 0)
            colors = CountColors(image, ignoreTransparent: false);
        var total = colors.Sum(c => c.Count);

        var frank = colors.Where(c => c.Chroma >= FrankChroma).ToList();
        var dominant = (frank.Sum(c => c.Count) >= FrankShare * total ? MostPresentFamily(frank) : null)
                       ?? colors.MaxBy(c => c.Count)!;

        var accent = MostPresentFamily(frank.Where(c =>
            c.Count >= AccentShare * total && HueDistance(c.Hue, dominant.Hue) >= AccentHueDistance));

        return new VisualPalette(
            dominant.Rgb,
            FromHsl(dominant.Hue, dominant.Saturation, Math.Min(dominant.Lightness, DeepLightness)),
            accent?.Rgb ?? FromHsl(dominant.Hue, dominant.Saturation, dominant.Lightness < 0.5 ? LightAccent : DarkAccent));
    }

    /// <summary>
    /// The most present colour of the family of hues that counts the most pixels: the shades of a background
    /// drawn in gradient add up, rather than each losing to a smaller flat colour.
    /// </summary>
    private static ColorCount? MostPresentFamily(IEnumerable<ColorCount> colors) =>
        colors
            .GroupBy(c => (int)(c.Hue / HueFamily))
            .MaxBy(family => family.Sum(c => c.Count))
            ?.MaxBy(c => c.Count);

    private static List<ColorCount> CountColors(SKBitmap image, bool ignoreTransparent)
    {
        var step = Math.Max(1, (int)Math.Sqrt((double)image.Width * image.Height / TargetSamples));
        var counts = new Dictionary<int, (int Count, long R, long G, long B)>();
        for (var y = 0; y < image.Height; y += step)
        {
            for (var x = 0; x < image.Width; x += step)
            {
                var pixel = image.GetPixel(x, y);
                // A half-transparent edge counts as its colour; only what is not shown at all is ignored.
                if (ignoreTransparent && pixel.Alpha == 0)
                    continue;

                var key = (pixel.Red >> ChannelShift << 8 | pixel.Green >> ChannelShift) << 4 | pixel.Blue >> ChannelShift;
                var (count, r, g, b) = counts.GetValueOrDefault(key);
                counts[key] = (count + 1, r + pixel.Red, g + pixel.Green, b + pixel.Blue);
            }
        }

        return counts.Values
            .Select(c => new ColorCount(
                new RgbColor((byte)(c.R / c.Count), (byte)(c.G / c.Count), (byte)(c.B / c.Count)), c.Count))
            .ToList();
    }

    private static double HueDistance(double a, double b)
    {
        var distance = Math.Abs(a - b) % 360;
        return Math.Min(distance, 360 - distance);
    }

    private static RgbColor FromHsl(double hue, double saturation, double lightness)
    {
        var color = SKColor.FromHsl((float)hue, (float)(saturation * 100), (float)(lightness * 100));
        return new RgbColor(color.Red, color.Green, color.Blue);
    }

    /// <summary>A colour of the image and how many of its sampled pixels have it.</summary>
    private sealed record ColorCount(RgbColor Rgb, int Count)
    {
        public double Chroma { get; } = (Math.Max(Rgb.R, Math.Max(Rgb.G, Rgb.B)) - Math.Min(Rgb.R, Math.Min(Rgb.G, Rgb.B))) / 255.0;

        public double Hue => Hsl.Hue;
        public double Saturation => Hsl.Saturation;
        public double Lightness => Hsl.Lightness;

        private (double Hue, double Saturation, double Lightness) Hsl { get; } = HslOf(Rgb);

        private static (double Hue, double Saturation, double Lightness) HslOf(RgbColor rgb)
        {
            new SKColor(rgb.R, rgb.G, rgb.B).ToHsl(out var hue, out var saturation, out var lightness);
            return (hue, saturation / 100, lightness / 100);
        }
    }
}
