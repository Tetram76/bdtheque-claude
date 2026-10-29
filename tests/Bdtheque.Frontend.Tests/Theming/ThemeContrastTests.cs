using System.Globalization;
using System.Text.RegularExpressions;
using Bdtheque.Frontend.Theming;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// Every pair of colors of the contracts meant to be read together reaches its contrast, in every offered theme: 4.5:1
/// for a text, 3:1 for the border of a control (WCAG 2.2, 1.4.3 and 1.4.11). A theme only redefines the contracts of the
/// base theme (choix-implementation/socle.md § Thèmes : mise en œuvre), so its colors are those of the base theme
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
        // A control: its text, its placeholder (liste-albums.md § Contrôles) and the border of a control in error.
        ("text", "control-background", Text),
        ("text-muted", "control-background", Text),
        ("control-border", "control-background", NonText),
        ("alert", "surface", NonText),
        ("alert", "surface-raised", NonText),
        ("alert", "control-background", NonText),
        // An item of a menu that deletes, in the color of deletion on the menu (liste-albums.md § Contenu).
        ("delete", "surface-raised", Text),
        .. Enumerable.Range(1, 7).Select(index => ("text-on-palette", $"palette-{index}", Text)),
    ];

    public static TheoryData<string> OfferedThemes => [.. ThemeCatalog.Offered.Select(theme => theme.Key)];

    [Theory]
    [MemberData(nameof(OfferedThemes))]
    public void ColorsReadTogether_ReachTheirContrast(string themeKey)
    {
        var colors = ColorsOf(ThemeCatalog.Offered.Single(theme => theme.Key == themeKey));

        var failures = Pairs
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

    private static Dictionary<string, string> ColorsOf(Theme theme)
    {
        var declarations = Declarations("base.css");
        if (theme.Stylesheet is { } stylesheet)
        {
            foreach (var (name, value) in Declarations(Path.GetFileName(stylesheet)))
                declarations[name] = value;
        }

        return declarations
            .Where(declaration => declaration.Key.StartsWith("color-", StringComparison.Ordinal))
            .ToDictionary(declaration => declaration.Key["color-".Length..], declaration => Resolve(declarations, declaration.Value));
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
