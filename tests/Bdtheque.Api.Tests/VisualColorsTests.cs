using Bdtheque.Api.Visuals;
using Bdtheque.Domain.Common;
using SkiaSharp;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The colours drawn from a visual (visuel/dashboard/la-couverture-du-jour.md,
/// choix-implementation/domaine/visuels-stockage-et-traitement.md): each case is an image of a few flat colours, whose
/// shares decide which colour plays which part.
/// </summary>
public sealed class VisualColorsTests
{
    private static readonly SKColor Blue = new(0x2F, 0x5B, 0xA8);
    private static readonly SKColor Red = new(0xD8, 0x34, 0x2B);
    private static readonly SKColor White = new(0xFA, 0xFA, 0xF7);

    [Fact]
    public void PaletteOf_TwoColours_TheMostPresentIsDominantTheOtherAccent()
    {
        using var image = Stripes((Blue, 80), (Red, 20));

        var palette = VisualColors.PaletteOf(image);

        Assert.Equal(new RgbColor(0x2F, 0x5B, 0xA8), palette.Dominant);
        Assert.Equal(new RgbColor(0xD8, 0x34, 0x2B), palette.Accent);
    }

    [Fact]
    public void PaletteOf_DeepIsADarkerDominantOfTheSameHue()
    {
        using var image = Stripes((Blue, 80), (Red, 20));

        var palette = VisualColors.PaletteOf(image);

        var (dominantHue, _, _) = Hsl(palette.Dominant);
        var (deepHue, _, deepLightness) = Hsl(palette.Deep);
        Assert.InRange(deepHue, dominantHue - 2, dominantHue + 2);
        Assert.True(deepLightness <= 0.22, $"{palette.Deep} is not deep.");
    }

    [Fact]
    public void PaletteOf_WhiteBackground_AColourStandsOutRatherThanThePaper()
    {
        // The white paper of a cover covers more than its drawing: a page painted white would show nothing of it.
        using var image = Stripes((White, 70), (Blue, 25), (Red, 5));

        var palette = VisualColors.PaletteOf(image);

        Assert.Equal(new RgbColor(0x2F, 0x5B, 0xA8), palette.Dominant);
        Assert.Equal(new RgbColor(0xD8, 0x34, 0x2B), palette.Accent);
    }

    [Fact]
    public void PaletteOf_PaleCover_DeepStillCarriesWhiteText()
    {
        using var image = Stripes((new SKColor(0xF2, 0xE2, 0x7A), 90), (Blue, 10));

        var palette = VisualColors.PaletteOf(image);

        Assert.True(Contrast(palette.Deep, new RgbColor(0xFF, 0xFF, 0xFF)) >= 4.5, $"White on {palette.Deep} is not legible.");
    }

    [Fact]
    public void PaletteOf_SingleHue_AccentIsAnotherShadeOfIt()
    {
        // No hue is invented: a cover in shades of blue has a blue accent, lighter than its dark dominant.
        using var image = Stripes((new SKColor(0x14, 0x29, 0x5A), 85), (new SKColor(0x1C, 0x3A, 0x80), 15));

        var palette = VisualColors.PaletteOf(image);

        var (dominantHue, _, dominantLightness) = Hsl(palette.Dominant);
        var (accentHue, _, accentLightness) = Hsl(palette.Accent);
        Assert.InRange(accentHue, dominantHue - 2, dominantHue + 2);
        Assert.True(accentLightness > dominantLightness + 0.3, $"{palette.Accent} does not stand apart from {palette.Dominant}.");
    }

    [Fact]
    public void PaletteOf_BlackAndWhite_GreysOnly()
    {
        using var image = Stripes((new SKColor(0x18, 0x18, 0x18), 60), (White, 40));

        var palette = VisualColors.PaletteOf(image);

        Assert.Equal(new RgbColor(0x18, 0x18, 0x18), palette.Dominant);
        foreach (var color in (RgbColor[])[palette.Deep, palette.Accent])
        {
            var (_, saturation, _) = Hsl(color);
            Assert.True(saturation < 0.05, $"{color} is not a grey.");
        }
    }

    [Fact]
    public void PaletteOf_SpeckOfAnotherColour_IsNoAccent()
    {
        // A few pixels are noise (a logo, a price sticker), not a colour of the cover.
        using var image = Stripes((Blue, 995), (Red, 5));

        var palette = VisualColors.PaletteOf(image);

        var (accentHue, _, _) = Hsl(palette.Accent);
        var (dominantHue, _, _) = Hsl(palette.Dominant);
        Assert.InRange(accentHue, dominantHue - 2, dominantHue + 2);
    }

    [Fact]
    public void PaletteOf_TransparentPixels_AreIgnored()
    {
        using var image = Stripes((SKColors.Transparent, 70), (Red, 30));

        var palette = VisualColors.PaletteOf(image);

        Assert.Equal(new RgbColor(0xD8, 0x34, 0x2B), palette.Dominant);
    }

    [Fact]
    public void PaletteOf_FullyTransparentImage_StillHasColours()
    {
        // A valid image all the same, accepted at upload: its palette is that of its hidden pixels, not a failure.
        using var image = Stripes((SKColors.Transparent, 100));

        var palette = VisualColors.PaletteOf(image);

        Assert.Equal(new RgbColor(0, 0, 0), palette.Dominant);
    }

    /// <summary>A 100-pixel wide image of horizontal stripes, one row per share point of each colour.</summary>
    private static SKBitmap Stripes(params (SKColor Color, int Share)[] stripes)
    {
        var bitmap = new SKBitmap(new SKImageInfo(100, stripes.Sum(s => s.Share), SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        var top = 0;
        foreach (var (color, share) in stripes)
        {
            using var paint = new SKPaint { Color = color, BlendMode = SKBlendMode.Src };
            canvas.DrawRect(0, top, 100, share, paint);
            top += share;
        }

        return bitmap;
    }

    private static (double Hue, double Saturation, double Lightness) Hsl(RgbColor color)
    {
        new SKColor(color.R, color.G, color.B).ToHsl(out var hue, out var saturation, out var lightness);
        return (hue, saturation / 100, lightness / 100);
    }

    // WCAG 2.2, relative luminance and contrast ratio.
    private static double Contrast(RgbColor a, RgbColor b)
    {
        static double Luminance(RgbColor c)
        {
            static double Linear(byte channel) =>
                channel / 255.0 <= 0.04045 ? channel / 255.0 / 12.92 : Math.Pow((channel / 255.0 + 0.055) / 1.055, 2.4);
            return 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
        }

        var (high, low) = (Math.Max(Luminance(a), Luminance(b)), Math.Min(Luminance(a), Luminance(b)));
        return (high + 0.05) / (low + 0.05);
    }
}
