using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Theming;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// The colours of the day of « La couverture du jour » (choix-implementation/socle/themes.md): those of the spotlight of the
/// day of the browser, which the API draws if it is not drawn yet.
/// </summary>
public sealed class SpotlightDayColorsTests
{
    // 23:30 in universal time: already the next day in Paris.
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 23, 30, 0, TimeSpan.Zero);

    private static readonly CoverPalette Palette = new("#F7E98E", "#B85AA2", "#5A3A1A");

    private readonly ColorsApi _api = new();

    [Fact]
    public async Task Colours_AreThoseOfTheSpotlightOfTheDayOfTheBrowser()
    {
        var colors = await ColorsIn("Europe/Paris").GetAsync();

        Assert.Equal(Palette, colors);
        Assert.Equal(["/catalog/spotlight/colors?day=2026-11-06"], _api.Requested);
    }

    // A first page, whose request does not bring the time zone of the browser yet: the theme keeps its own colours, and nothing
    // is asked, so that no edition is drawn for a day the visitor does not live (the date in universal time, the 5th).
    [Fact]
    public async Task WithoutTheTimeZoneOfTheBrowser_TheThemeKeepsItsOwnColours_WithoutAnyDraw()
    {
        var colors = await ColorsIn(null).GetAsync();

        Assert.Null(colors);
        Assert.Empty(_api.Requested);
    }

    private SpotlightDayColors ColorsIn(string? timeZone) => new(
        new ApiClient(new HttpClient(_api) { BaseAddress = new Uri("http://api") }),
        new BrowserTimeZone { Name = timeZone },
        new FixedClock(Now),
        NullLogger<SpotlightDayColors>.Instance);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ColorsApi : HttpMessageHandler
    {
        public List<string> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new SpotlightColors(Palette)) });
        }
    }
}
