using System.Globalization;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Theming;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// Every pair of colors of the contracts meant to be read together reaches its contrast, in every offered theme: 4.5:1
/// for a text, 3:1 for the border of a control (WCAG 2.2, 1.4.3 and 1.4.11). A theme only redefines the contracts of the
/// base theme (choix-implementation/socle/themes.md), so its colors are those of the base theme
/// overlaid with its own.
/// </summary>
public sealed partial class ThemeContrastTests
{
    private const double Text = 4.5;
    private const double NonText = 3;

    private static readonly (string Foreground, string Background, double Minimum)[] Pairs =
    [
        ("text", "background", Text),
        ("text", "surface", Text),
        ("text", "surface-raised", Text),
        ("text-secondary", "background", Text),
        ("text-secondary", "surface", Text),
        ("text-muted", "background", Text),
        ("text-muted", "surface", Text),
        ("text-muted", "surface-raised", Text),
        ("text-on-inverse", "surface-inverse", Text),
        ("accent-text", "background", Text),
        ("accent-text", "surface", Text),
        ("text-on-accent", "accent", Text),
        ("text-on-alert", "alert", Text),
        ("text-on-info", "info", Text),
        ("text-on-delete", "delete", Text),
        ("text-on-failure", "failure", Text),
        ("text-on-tag", "tag", Text),
        ("control-border", "background", NonText),
        ("control-border", "surface", NonText),
        ("control-border", "surface-raised", NonText),
        // A control: its text, its placeholder (visuel/liste-albums/controles.md) and the border of a control in error.
        ("text", "control-background", Text),
        ("text-muted", "control-background", Text),
        ("placeholder", "control-background", Text),
        ("control-border", "control-background", NonText),
        ("alert", "surface", NonText),
        ("alert", "surface-raised", NonText),
        ("alert", "control-background", NonText),
        // An item of a menu that deletes, in the color of deletion on the menu (visuel/liste-albums/contenu.md).
        ("delete", "surface-raised", Text),
        .. Enumerable.Range(1, 7).Select(index => ("text-on-palette", $"palette-{index}", Text)),
        .. Enumerable.Range(1, 7).Select(index => ($"text-on-monogram-{index}", $"monogram-{index}", Text)),
        // The header: its texts, its search field, the tag of the administration and the help button.
        ("text-on-header", "header", Text),
        ("text-on-header", "header-field", Text),
        ("header-placeholder", "header-field", Text),
        ("header-field-border", "header", NonText),
        ("text-on-header-tag", "header-tag", Text),
        ("text-on-help", "help", Text),
        // A notice within a block (e.g. the authors of an album taken from its series): its text, and its frame, which alone
        // tells it from the block.
        ("text-on-notice", "notice", Text),
        ("notice-border", "notice", NonText),
        // The tile of a type of record of the home page of the administration: its name, detail and links.
        ("text", "tile", Text),
        ("text-muted", "tile", Text),
        ("accent-text", "tile", Text),
        // The genres and the universes of a record, each in colors of their own.
        ("text-on-genre", "genre", Text),
        ("text-on-universe", "universe", Text),
        // A block of its own tint holds what a surface holds: texts, links and the borders of its controls. The border of a
        // control in error is read against the inside of the control (alert on control-background, above), which suffices.
        .. Enumerable.Range(1, 14).SelectMany(index => new[]
        {
            ("text", $"block-tint-{index}", Text),
            ("text-secondary", $"block-tint-{index}", Text),
            ("text-muted", $"block-tint-{index}", Text),
            ("accent-text", $"block-tint-{index}", Text),
            ("control-border", $"block-tint-{index}", NonText),
        }),
    ];

    // A theme painted by the day: the stage of the home page, of the deep colour (surface-inverse), white texts and their
    // light version, on the deep colour, on the reflection of the accent in a corner of the stage, and on a field of the header.
    private static readonly (string Foreground, string Background, double Minimum)[] DayPairs =
    [
        ("text-on-inverse", "day-reflection", Text),
        ("day-light-on-deep", "surface-inverse", Text),
        ("day-light-on-deep", "day-reflection", Text),
        ("day-light-on-deep", "header-field", Text),
    ];

    // Covers of every kind (visuel/dashboard/la-couverture-du-jour.md, Lisibilité): none (the theme's own colours), pale
    // (yellow, white), of middle tone, dark, grey, saturated, and a deep colour the API would have left too light.
    private static readonly Dictionary<string, CoverPalette?> Covers = new()
    {
        ["none"] = null,
        ["blue"] = new("#2F5BA8", "#14295A", "#D8342B"),
        ["pale yellow"] = new("#F5E663", "#5C5208", "#F2D23C"),
        ["white"] = new("#FFFFFF", "#383838", "#FAFAFA"),
        ["middle tone"] = new("#7A7A7A", "#383838", "#7A7A7A"),
        ["black"] = new("#000000", "#000000", "#000000"),
        ["red"] = new("#FF0000", "#700000", "#00FF00"),
        ["cyan"] = new("#00FFFF", "#007070", "#FF00FF"),
        ["light deep"] = new("#F5E663", "#F5E663", "#2F5BA8"),
    };

    public static TheoryData<string, string> OfferedThemes
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var theme in ThemeCatalog.Offered)
            foreach (var cover in theme.PaintedByTheDay ? [.. Covers.Keys] : (string[])["none"])
                data.Add(theme.Key, cover);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(OfferedThemes))]
    public void ColorsReadTogether_ReachTheirContrast(string themeKey, string cover)
    {
        var theme = ThemeCatalog.Offered.Single(theme => theme.Key == themeKey);
        var colors = ColorsOf(theme, theme.PaintedByTheDay ? DayPalette.Of(Covers[cover]) : null);

        var failures = Pairs.Concat(theme.PaintedByTheDay ? DayPairs : [])
            .Select(pair => (pair, ratio: ContrastRatio(colors[pair.Foreground], colors[pair.Background])))
            .Where(result => result.ratio < result.pair.Minimum)
            .Select(result => $"{result.pair.Foreground} on {result.pair.Background}: {result.ratio:0.00}:1 < {result.pair.Minimum}:1");

        Assert.Empty(failures);
    }

    [Fact]
    public void BaseTheme_GivesAValueToEveryColorOfThePairs()
    {
        var colors = ColorsOf(ThemeCatalog.Base);

        Assert.All(Pairs.SelectMany(pair => new[] { pair.Foreground, pair.Background }), name => Assert.True(colors.ContainsKey(name), name));
    }

    [Fact]
    public void ContrastRatio_IsTheOneOfWcag()
    {
        Assert.Equal(21, ContrastRatio("#000000", "#FFFFFF"), 2);
        Assert.Equal(4.54, ContrastRatio("#767676", "#FFFFFF"), 2);
    }

    // The colours of the day, set by the page on its root element, are read by the declarations of the theme: overlaid on
    // them, they keep their own names (day-…).
    private static Dictionary<string, string> ColorsOf(Theme theme, DayPalette? day = null)
    {
        var declarations = Declarations("base.css");
        if (theme.Stylesheet is { } stylesheet)
        {
            foreach (var (name, value) in Declarations(Path.GetFileName(stylesheet)))
                declarations[name] = value;
        }

        foreach (var (name, value) in day?.Properties ?? new Dictionary<string, string>())
            declarations[name] = value;

        return declarations
            .Where(declaration => declaration.Key.StartsWith("color-", StringComparison.Ordinal) || declaration.Key.StartsWith("day-", StringComparison.Ordinal))
            .ToDictionary(
                declaration => declaration.Key.StartsWith("color-", StringComparison.Ordinal) ? declaration.Key["color-".Length..] : declaration.Key,
                declaration => Resolve(declarations, declaration.Value));
    }

    // The first declaration of a property is its default value; a later one only adapts it to a wider screen.
    private static Dictionary<string, string> Declarations(string fileName) =>
        CustomProperty().Matches(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "themes", fileName)))
            .GroupBy(match => match.Groups["name"].Value)
            .ToDictionary(group => group.Key, group => group.First().Groups["value"].Value.Trim());

    private static string Resolve(Dictionary<string, string> declarations, string value) =>
        VarReference().Match(value) is { Success: true } reference ? Resolve(declarations, declarations[reference.Groups["name"].Value]) : value;

    private static double ContrastRatio(string first, string second)
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

    [GeneratedRegex(@"--(?<name>[a-z0-9-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex CustomProperty();

    [GeneratedRegex(@"^var\(--(?<name>[a-z0-9-]+)\)$")]
    private static partial Regex VarReference();
}
