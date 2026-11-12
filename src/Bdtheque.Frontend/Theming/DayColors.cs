using System.Globalization;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Formatting;

namespace Bdtheque.Frontend.Theming;

/// <summary>Where the colours of the day come from, for a theme painted by them (<see cref="Theme.PaintedByTheDay"/>).</summary>
public interface IDayColorsSource
{
    /// <summary>The colours of the cover of the day; <c>null</c> without any, the theme then keeping its own.</summary>
    Task<CoverPalette?> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The colours of the cover of the edition in the spotlight of the day of the browser, which the API draws if it is not
/// drawn yet (choix-implementation/domaine/visuels-stockage-et-traitement.md, couleurs du jour). They never fail a page:
/// without them, the theme keeps its own colours, as for a collection without cover.
/// </summary>
public sealed class SpotlightDayColors(ApiClient api, BrowserTimeZone timeZone, TimeProvider clock, ILogger<SpotlightDayColors> logger)
    : IDayColorsSource
{
    public async Task<CoverPalette?> GetAsync(CancellationToken cancellationToken = default)
    {
        // Without the date of the browser (cookie refused, no script), the theme keeps its own colours: asking for another
        // day would draw and record an edition for a day the visitor does not live.
        if (timeZone.Today(clock) is not { } today)
            return null;

        var day = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        try
        {
            return (await api.GetAsync<SpotlightColors>($"/catalog/spotlight/colors?day={day}", cancellationToken)).Palette;
        }
        catch (ApiProblemException exception)
        {
            logger.LogWarning(exception, "The page is presented in the own colours of its theme, without those of the day.");
            return null;
        }
    }
}
