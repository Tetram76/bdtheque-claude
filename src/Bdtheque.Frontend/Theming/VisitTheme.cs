using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Theming;

/// <summary>
/// The rules of the theme of the visit (fonctionnel/index.md § Design et charte graphique, Thème actif): drawn at random
/// among the offered themes at the start of a visit, kept for the whole visit, and drawn again for each new date of the
/// browser, in its current time zone, from the first page displayed at that date.
/// </summary>
public static class VisitTheme
{
    /// <summary>
    /// The visit a request continues: the same one when nothing changes, so that the caller only writes its cookie again
    /// when it changed.
    /// </summary>
    /// <param name="visit">The visit of the browser, <c>null</c> for a new one.</param>
    /// <param name="today">Date in universal time, which bounds the duration of a visit.</param>
    /// <param name="browserToday">Date of the browser; <c>null</c> while its time zone is not known.</param>
    /// <param name="offered">Themes among which the theme is drawn.</param>
    /// <param name="draw">Draws an index below the number it receives.</param>
    public static BrowserVisit Continue(
        BrowserVisit? visit, DateOnly today, DateOnly? browserToday, IReadOnlyList<Theme> offered, Func<int, int> draw)
    {
        if (visit is null || visit.IsOver(today))
            return new BrowserVisit(today, Draw(offered, draw), browserToday);
        if (!offered.Any(theme => theme.Key == visit.ThemeKey)
            || (visit.ThemeDate is { } themeDate && browserToday is { } date && date != themeDate))
            return visit with { ThemeKey = Draw(offered, draw), ThemeDate = browserToday ?? visit.ThemeDate };
        // A theme drawn on the first page, before the browser gave its time zone, is the theme of its first known date:
        // universal time could be another date than the browser's, and drawing again then would change the theme in the
        // middle of that date.
        if (visit.ThemeDate is null && browserToday is not null)
            return visit with { ThemeDate = browserToday };
        return visit;
    }

    /// <summary>The theme of a visit, which <see cref="Continue"/> keeps among the offered ones.</summary>
    public static Theme Of(BrowserVisit visit, IReadOnlyList<Theme> offered) => offered.Single(theme => theme.Key == visit.ThemeKey);

    private static string Draw(IReadOnlyList<Theme> offered, Func<int, int> draw) => offered[draw(offered.Count)].Key;
}
