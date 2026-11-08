using System.Globalization;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Theming;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// The colours of the day of « La couverture du jour » (visuel/dashboard/la-couverture-du-jour.md): those of the cover of
/// the edition in the spotlight, or the theme's own without it, and a text legible on the accent whatever the cover.
/// The contrasts of every colour read together are checked by <see cref="ThemeContrastTests"/>.
/// </summary>
public sealed partial class DayPaletteTests
{
    private const string White = "#FFFFFF";

    [Fact]
    public void WithoutCover_ThePageTakesTheColorsOfTheTheme()
    {
        var properties = DayPalette.Of(null).Properties;

        Assert.Equal("#3D5A80", properties["day-dominant"]);
        Assert.Equal("#1E2A44", properties["day-deep"]);
        Assert.Equal("#FF6B57", properties["day-accent"]);
    }

    [Fact]
    public void ColorsOfTheCover_AreThoseOfThePage()
    {
        var properties = DayPalette.Of(new CoverPalette("#2F5BA8", "#14295A", "#D8342B")).Properties;

        Assert.Equal("#2F5BA8", properties["day-dominant"]);
        Assert.Equal("#14295A", properties["day-deep"]);
        Assert.Equal("#D8342B", properties["day-accent"]);
    }

    [Fact]
    public void PaleAccent_KeepsItsColor_WithANearlyBlackText()
    {
        var properties = DayPalette.Of(new CoverPalette("#2F5BA8", "#14295A", "#F5E663")).Properties;

        Assert.Equal("#F5E663", properties["day-accent"]);
        Assert.Equal(DayPalette.Ink, properties["day-text-on-accent"]);
    }

    [Fact]
    public void DarkAccent_TakesAWhiteText()
    {
        var properties = DayPalette.Of(new CoverPalette("#2F5BA8", "#14295A", "#14295A")).Properties;

        Assert.Equal(White, properties["day-text-on-accent"]);
    }

    [Fact]
    public void MidToneAccent_ThatNeitherTextReaches_IsDarkenedUntilWhiteReachesIt()
    {
        // A grey of luminance about 0.19: 3.9:1 with white, 4.3:1 with the nearly black text.
        var properties = DayPalette.Of(new CoverPalette("#2F5BA8", "#14295A", "#7A7A7A")).Properties;

        Assert.NotEqual("#7A7A7A", properties["day-accent"]);
        Assert.Equal(White, properties["day-text-on-accent"]);
        Assert.True(ContrastRatio(White, properties["day-accent"]) >= 4.5);
    }

    [Fact]
    public void DeepColorTooLightForAWhiteText_IsDarkened()
    {
        var properties = DayPalette.Of(new CoverPalette("#F5E663", "#F5E663", "#2F5BA8")).Properties;

        Assert.True(ContrastRatio(White, properties["day-deep"]) >= 4.5);
    }

    [Fact]
    public void EveryColor_IsInTheHexadecimalNotationOfCss()
    {
        var properties = DayPalette.Of(new CoverPalette("#2F5BA8", "#14295A", "#D8342B")).Properties;

        Assert.All(properties.Values, value => Assert.Matches(HexColor(), value));
    }

    [Fact]
    public void Style_SetsEachColorAsACustomProperty()
    {
        var palette = DayPalette.Of(null);

        Assert.Contains("--day-deep: #1E2A44;", palette.Style, StringComparison.Ordinal);
        Assert.Equal(palette.Properties.Count, palette.Style.Split(';', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    internal static double ContrastRatio(string first, string second)
    {
        var (lighter, darker) = (Luminance(first), Luminance(second)) is var (a, b) && a > b ? (a, b) : (b, a);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int start)
        {
            var value = int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }

    [GeneratedRegex("^#[0-9A-F]{6}$")]
    private static partial Regex HexColor();
}
