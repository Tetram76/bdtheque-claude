using System.Globalization;

namespace ThemeProto.Components.Shared;

public static class Fmt
{
    public static string N(int value) => value.ToString("N0", CultureInfo.CurrentCulture);

    public static string Eur(decimal value) =>
        value.ToString(value >= 1000 ? "N0" : "N2", CultureInfo.CurrentCulture) + " €";

    public static string Day(DateOnly date) => date.ToString("d MMM", CultureInfo.CurrentCulture);

    /// <summary>CSS values must use a dot, whatever the display culture.</summary>
    public static string Css(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
