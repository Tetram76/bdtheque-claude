using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// Formats the data shown to the user in the display culture of the request (fonctionnel/presentation.md
/// § Langue et culture d'affichage), positioned by the request localization middleware.
/// </summary>
public sealed class DisplayFormatter(IStringLocalizer<CurrencySymbols> symbols, BrowserTimeZone timeZone)
{
    public string Number(long value) => value.ToString("N0", CultureInfo.CurrentCulture);

    /// <summary>
    /// An amount in its own currency (ISO 4217 code, or <c>QZF</c> for the old franc, choix-implementation/domaine.md
    /// § Représentation de la devise). The currency of the culture keeps the culture's own currency format; any
    /// other one, which the culture does not know, is followed by its symbol, its code when it has none.
    /// </summary>
    public string Amount(decimal amount, string currency)
    {
        var culture = CultureInfo.CurrentCulture;
        if (string.Equals(currency, new RegionInfo(culture.Name).ISOCurrencySymbol, StringComparison.Ordinal))
            return amount.ToString("C", culture);

        var symbol = symbols[currency];
        return string.Create(culture, $"{amount:N2} {(symbol.ResourceNotFound ? currency : symbol.Value)}");
    }

    public string Date(DateOnly date) => date.ToString("d", CultureInfo.CurrentCulture);

    /// <summary>A date and time, in the time zone of the browser (<see cref="BrowserTimeZone"/>).</summary>
    public string DateTime(DateTimeOffset instant) => timeZone.ToLocal(instant).ToString("g", CultureInfo.CurrentCulture);

    /// <summary>A date known to the year, or to the month and year (modele-metier.md § Album, date de première publication).</summary>
    public string PartialDate(int year, int? month) =>
        month is { } known
            ? new DateOnly(year, known, 1).ToString("Y", CultureInfo.CurrentCulture)
            : year.ToString(CultureInfo.InvariantCulture);
}
