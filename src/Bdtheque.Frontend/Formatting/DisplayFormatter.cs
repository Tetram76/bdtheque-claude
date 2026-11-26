using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// Formats the data shown to the user in the display culture of the request
/// (fonctionnel/presentation/langue-et-culture-d-affichage.md), positioned by the request localization
/// middleware.
/// </summary>
public sealed class DisplayFormatter(IStringLocalizer<CurrencySymbols> symbols, BrowserTimeZone timeZone)
{
    public string Number(long value) => value.ToString("N0", CultureInfo.CurrentCulture);

    /// <summary>
    /// An amount in its own currency (ISO 4217 code, or <c>QZF</c> for the old franc,
    /// choix-implementation/domaine/representation-de-la-devise.md). The currency of the culture keeps the culture's
    /// own currency format; any
    /// other one, which the culture does not know, is followed by its symbol, its code when it has none.
    /// </summary>
    /// <remarks>
    /// Two decimals, unless <paramref name="exact"/>: then every decimal that counts, up to the four an amount keeps (as its
    /// entry shows it, Components/Controls/AmountField.razor), so that two amounts that differ never read the same.
    /// </remarks>
    public string Amount(decimal amount, string currency, bool exact = false)
    {
        var culture = CultureInfo.CurrentCulture;
        var decimals = exact ? Math.Max(2, SignificantDecimals(amount)) : 2;
        if (string.Equals(currency, new RegionInfo(culture.Name).ISOCurrencySymbol, StringComparison.Ordinal))
            return amount.ToString($"C{decimals}", culture);

        var symbol = symbols[currency];
        return string.Create(culture, $"{amount.ToString($"N{decimals}", culture)} {(symbol.ResourceNotFound ? currency : symbol.Value)}");
    }

    // The decimals of an amount without its trailing zeros: 12.3410 has three.
    private static int SignificantDecimals(decimal amount)
    {
        var digits = amount.ToString(CultureInfo.InvariantCulture);
        var point = digits.IndexOf('.', StringComparison.Ordinal);
        return point < 0 ? 0 : digits.TrimEnd('0').Length - point - 1;
    }

    public string Date(DateOnly date) => date.ToString("d", CultureInfo.CurrentCulture);

    /// <summary>A date and time, in the time zone of the browser (<see cref="BrowserTimeZone"/>).</summary>
    public string DateTime(DateTimeOffset instant) => timeZone.ToLocal(instant).ToString("g", CultureInfo.CurrentCulture);

    /// <summary>The date of an instant, in the time zone of the browser (<see cref="BrowserTimeZone"/>).</summary>
    public string LocalDate(DateTimeOffset instant) => timeZone.ToLocal(instant).ToString("d", CultureInfo.CurrentCulture);

    /// <summary>A date known to the year, or to the month and year (modele-metier/album.md, date de première publication).</summary>
    public string PartialDate(int year, int? month) =>
        month is { } known
            ? new DateOnly(year, known, 1).ToString("Y", CultureInfo.CurrentCulture)
            : year.ToString(CultureInfo.InvariantCulture);
}
