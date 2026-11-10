using System.Globalization;
using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;

namespace Bdtheque.ValidationData.Tests;

/// <summary>
/// The validation data loaded with the pale cover in the spotlight, on demand at the start of the validation environment
/// (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md): « La couverture du jour »
/// (visuel/dashboard/la-couverture-du-jour.md, Lisibilité) is then judged on its most demanding case.
/// </summary>
public sealed class PaleCoverSpotlightTests(PaleCoverSpotlightTests.LoadedApi api) : IClassFixture<PaleCoverSpotlightTests.LoadedApi>
{
    private readonly HttpClient _client = api.CreateApiClient();

    // The cover of the days around the load, which colours every page of the theme, whatever the time zone of the browser, is
    // pale, with an accent of middle tone, which neither a white nor a nearly black text reaches.
    [Fact]
    public async Task SpotlightOfTheDaysAroundTheLoad_IsAPaleCover_WithAnAccentOfMiddleTone()
    {
        var today = DateOnly.FromDateTime(LoadedApiFixture.Origin.UtcDateTime);

        foreach (var day in new[] { today.AddDays(-1), today, today.AddDays(1) })
        {
            var palette = (await _client.GetFromJsonAsync<SpotlightColors>($"/catalog/spotlight/colors?day={day:yyyy-MM-dd}"))!.Palette!;

            Assert.True(Luminance(palette.Dominant) > 0.6, $"The cover of {day} is not pale: {palette.Dominant}.");
            Assert.True(ContrastRatio(palette.Accent, "#FFFFFF") < 4.5 && ContrastRatio(palette.Accent, "#15151A") < 4.5,
                $"The accent of {day} is not of middle tone: {palette.Accent}.");
        }
    }

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

    /// <summary>The validation data as a start of the validation environment loads it on demand of the pale cover.</summary>
    public sealed class LoadedApi() : LoadedApiFixture(paleCoverInTheSpotlight: true);
}
