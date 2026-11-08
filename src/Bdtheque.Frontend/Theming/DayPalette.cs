using System.Globalization;
using Bdtheque.Contracts.Catalog;

namespace Bdtheque.Frontend.Theming;

/// <summary>
/// The colours of the day of a theme painted by the cover of the edition in the spotlight (« La couverture du jour »,
/// visuel/dashboard/la-couverture-du-jour.md), as custom properties of the page, which its stylesheet reads
/// (choix-implementation/socle/themes.md, couleurs du jour). Every colour read with another is derived here, in the
/// hexadecimal notation of CSS, rather than mixed by the stylesheet: legibility is ensured whatever the cover, even a pale
/// one, and checked by a test on any palette (ThemeContrastTests).
/// </summary>
public sealed class DayPalette
{
    /// <summary>The theme's own colours, without an edition in the spotlight or without its cover: bleu, bleu nuit, corail.</summary>
    public static readonly CoverPalette Own = new("#3D5A80", "#1E2A44", "#FF6B57");

    /// <summary>The nearly black text of the theme, read on a pale accent.</summary>
    public const string Ink = "#15151A";

    private const double Legible = 4.5;

    // The border of a control (WCAG 2.2, 1.4.11).
    private const double NonText = 3;
    private static readonly Rgb White = new(255, 255, 255);
    private static readonly Rgb InkColor = Rgb.Parse(Ink);

    // The light papers of the page, faintly tinted with the dominant colour (couverture.html).
    private static readonly Rgb Paper = Rgb.Parse("#F7F6F3");
    private static readonly Rgb Field = Rgb.Parse("#F4F4F1");

    private DayPalette(IReadOnlyDictionary<string, string> properties) => Properties = properties;

    /// <summary>Each colour by the name of its custom property, without its leading dashes (e.g. <c>day-deep</c>).</summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>The colours as the declarations of a <c>style</c> attribute.</summary>
    public string Style => string.Concat(Properties.Select(property => $"--{property.Key}: {property.Value};"));

    /// <summary>The colours of the day, from those of the cover of the day, <c>null</c> without any.</summary>
    public static DayPalette Of(CoverPalette? cover)
    {
        var palette = cover ?? Own;
        var dominant = Rgb.Parse(palette.Dominant);
        // The API draws it dark enough for a white text; ensured here all the same, as everything read on it.
        var deep = Rgb.Parse(palette.Deep).DarkenedUntil(color => color.ContrastWith(White) >= Legible);
        var (accent, onAccent) = LegibleAccent(Rgb.Parse(palette.Accent));

        var background = dominant.Over(Paper, 0.09);
        var surface = dominant.Over(White, 0.04);
        var tint = dominant.Over(White, 0.16);
        var field = dominant.Over(Field, 0.05);
        Rgb[] lightPapers = [background, surface, field];

        // The header and the stage are of the deep colour: a field is lighter, a reflection of the accent lights a corner,
        // and the secondary texts are a light version of the deep colour, all read with a white text.
        var headerField = White.Over(deep, 0.14).DarkenedUntil(color => color.ContrastWith(White) >= Legible);
        var reflection = accent.Over(deep, 0.35).DarkenedUntil(color => color.ContrastWith(White) >= Legible);
        var lightOnDeep = White.Over(deep, 0.75)
            .LightenedUntil(color => new[] { deep, headerField, reflection }.All(paper => color.ContrastWith(paper) >= Legible));

        return new DayPalette(new Dictionary<string, string>
        {
            ["day-dominant"] = dominant.Hex,
            ["day-deep"] = deep.Hex,
            ["day-accent"] = accent.Hex,
            ["day-text-on-accent"] = onAccent.Hex,
            ["day-background"] = background.Hex,
            ["day-surface"] = surface.Hex,
            ["day-tint"] = tint.Hex,
            ["day-field"] = field.Hex,
            // The links, of the dominant colour, darkened as much as a light paper needs.
            ["day-link"] = dominant.DarkenedUntil(color => lightPapers.All(paper => color.ContrastWith(paper) >= Legible)).Hex,
            // The deep colour as a text on a tint, darkened as much as the tint needs.
            ["day-deep-text"] = deep.DarkenedUntil(color => color.ContrastWith(tint) >= Legible).Hex,
            ["day-header-field"] = headerField.Hex,
            ["day-header-field-border"] = White.Over(deep, 0.5).LightenedUntil(color => color.ContrastWith(deep) >= NonText).Hex,
            ["day-light-on-deep"] = lightOnDeep.Hex,
            // The halo behind the album in the spotlight keeps a part of the deep colour, even when the cover is pale.
            ["day-halo"] = dominant.Over(deep, 0.7).Hex,
            ["day-reflection"] = reflection.Hex,
        });
    }

    /// <summary>
    /// A text white or nearly black on the accent, whichever reaches 4.5:1; an accent of middle tone, which neither reaches,
    /// is darkened until white does.
    /// </summary>
    private static (Rgb Accent, Rgb Text) LegibleAccent(Rgb accent)
    {
        var legible = accent.DarkenedUntil(color => color.ContrastWith(White) >= Legible || color.ContrastWith(InkColor) >= Legible);
        return (legible, legible.ContrastWith(White) >= legible.ContrastWith(InkColor) ? White : InkColor);
    }

    /// <summary>An sRGB colour, and what WCAG 2.2 computes of it.</summary>
    private readonly record struct Rgb(byte R, byte G, byte B)
    {
        // The step by which a colour is darkened or lightened, that of the mockup (couverture.html, legibleAccent).
        private const double Step = 0.05;

        public string Hex => $"#{R:X2}{G:X2}{B:X2}";

        public static Rgb Parse(string hex) => new(Channel(hex, 1), Channel(hex, 3), Channel(hex, 5));

        /// <summary>This colour laid over <paramref name="paper"/>, for <paramref name="share"/> of it.</summary>
        public Rgb Over(Rgb paper, double share) =>
            new(Mix(R, paper.R, share), Mix(G, paper.G, share), Mix(B, paper.B, share));

        public double ContrastWith(Rgb other)
        {
            var (lighter, darker) = Luminance > other.Luminance ? (Luminance, other.Luminance) : (other.Luminance, Luminance);
            return (lighter + 0.05) / (darker + 0.05);
        }

        /// <summary>This colour, darkened step by step until <paramref name="enough"/>; black satisfies any text contrast.</summary>
        public Rgb DarkenedUntil(Func<Rgb, bool> enough)
        {
            var color = this;
            while (!enough(color) && color != default)
                color = new Rgb(Scale(color.R), Scale(color.G), Scale(color.B)) is var darker && darker == color ? default : darker;
            return color;
        }

        /// <summary>This colour, lightened step by step towards white until <paramref name="enough"/>.</summary>
        public Rgb LightenedUntil(Func<Rgb, bool> enough)
        {
            var color = this;
            while (!enough(color) && color != White)
                color = White.Over(color, Step) is var lighter && lighter == color ? White : lighter;
            return color;
        }

        private double Luminance => 0.2126 * Linear(R) + 0.7152 * Linear(G) + 0.0722 * Linear(B);

        private static double Linear(byte channel)
        {
            var value = channel / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        private static byte Scale(byte channel) => (byte)Math.Round(channel * (1 - Step), MidpointRounding.AwayFromZero);

        private static byte Mix(byte color, byte paper, double share) =>
            (byte)Math.Round(color * share + paper * (1 - share), MidpointRounding.AwayFromZero);

        private static byte Channel(string hex, int start) =>
            byte.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
