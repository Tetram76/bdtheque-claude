using System.Globalization;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>Values formatted in the display culture of the request, French of France (fonctionnel/presentation/).</summary>
public sealed class DisplayFormatterTests : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly DisplayFormatter _formatter =
        new ServiceCollection().AddLogging().AddLocalization().AddScoped<BrowserTimeZone>().AddScoped<DisplayFormatter>()
            .BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<DisplayFormatter>();

    public DisplayFormatterTests() => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

    public void Dispose() => CultureInfo.CurrentCulture = _previous;

    [Fact]
    public void Number_GroupsItsThousands() =>
        Assert.Equal(12345.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")), _formatter.Number(12345));

    [Fact]
    public void Amount_InEuros_UsesTheCurrencyOfTheCulture() =>
        Assert.Equal(12.5m.ToString("C", CultureInfo.GetCultureInfo("fr-FR")), _formatter.Amount(12.5m, "EUR"));

    [Theory]
    [InlineData("USD", "USD")]
    [InlineData("FRF", "F")]
    [InlineData("QZF", "AF")]
    public void Amount_InAnotherCurrency_IsFollowedByItsSymbol(string currency, string symbol) =>
        Assert.Equal($"1{Group}000,00 {symbol}", _formatter.Amount(1000m, currency));

    // With the decimals of its currency, and not always two (fonctionnel/valorisation/gestion-des-devises.md, précision d'un montant);
    // read back from the database with four decimals, trailing zeros included. {0} is the group separator, {1} the space before
    // the symbol.
    [Theory]
    [InlineData("1500.0000", "JPY", "1{0}500{1}JPY")]
    [InlineData("1.5000", "KWD", "1,500{1}KWD")]
    [InlineData("12.3400", "CLF", "12,3400{1}CLF")]
    [InlineData("45.9000", "FRF", "45,90{1}F")]
    public void Amount_HasTheDecimalsOfItsCurrency(string amount, string currency, string expected) =>
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, expected, Group, ' '),
            _formatter.Amount(decimal.Parse(amount, CultureInfo.InvariantCulture), currency));

    [Fact]
    public void Amount_InEuros_ReadBackWithFourDecimals_HasTwo() =>
        Assert.Equal(12.5m.ToString("C", CultureInfo.GetCultureInfo("fr-FR")), _formatter.Amount(12.5000m, "EUR"));

    [Fact]
    public void Date_IsWrittenAsInFrance() =>
        Assert.Equal("05/03/2020", _formatter.Date(new DateOnly(2020, 3, 5)));

    [Theory]
    [InlineData(3, "mars 2020")]
    [InlineData(null, "2020")]
    public void PartialDate_KeepsItsPrecision(int? month, string expected) =>
        Assert.Equal(expected, _formatter.PartialDate(2020, month));

    private static string Group => CultureInfo.GetCultureInfo("fr-FR").NumberFormat.NumberGroupSeparator;
}
