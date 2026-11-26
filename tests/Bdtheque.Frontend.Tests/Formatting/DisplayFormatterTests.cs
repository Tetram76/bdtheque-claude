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

    [Theory]
    [InlineData("12.341", "C3")]
    [InlineData("12.5000", "C2")]
    public void Amount_Exact_InEuros_KeepsEveryDecimalThatCounts_TwoAtLeast(string amount, string format)
    {
        var value = decimal.Parse(amount, CultureInfo.InvariantCulture);
        Assert.Equal(value.ToString(format, CultureInfo.GetCultureInfo("fr-FR")), _formatter.Amount(value, "EUR", exact: true));
    }

    [Theory]
    [InlineData("12.5", "USD", "12,50\u00a0USD")]
    [InlineData("1.2345", "FRF", "1,2345\u00a0F")]
    public void Amount_Exact_InAnotherCurrency_KeepsEveryDecimalThatCounts_TwoAtLeast(string amount, string currency, string expected) =>
        Assert.Equal(expected, _formatter.Amount(decimal.Parse(amount, CultureInfo.InvariantCulture), currency, exact: true));

    [Fact]
    public void Amount_NotExact_ShowsTwoDecimals() =>
        Assert.Equal("12,34 USD", _formatter.Amount(12.341m, "USD"));

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
