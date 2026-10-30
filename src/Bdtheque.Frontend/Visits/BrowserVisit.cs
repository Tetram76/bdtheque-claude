using System.Globalization;

namespace Bdtheque.Frontend.Visits;

/// <summary>
/// The visit of the browser: its browsing session, shared by all its tabs and windows, another browser making another
/// visit (fonctionnel/mesure-audience.md, Visites). Kept in a session cookie (<see cref="CookieName"/>), which the
/// browser forgets with its session: the single delimitation of the visit, for its theme (fonctionnel/index.md § Design
/// et charte graphique, Thème actif) as for the audience measurement (#197). It carries nothing that identifies the user.
/// </summary>
/// <param name="Start">Date of the start of the visit, in universal time, which bounds its duration (<see cref="IsOver"/>).</param>
/// <param name="ThemeKey">Theme of the visit.</param>
/// <param name="ThemeDate">
/// Date of the browser for which the theme was drawn; unknown while the browser has not given its time zone.
/// </param>
public sealed record BrowserVisit(DateOnly Start, string ThemeKey, DateOnly? ThemeDate)
{
    public const string CookieName = "Bdtheque.Visit";

    /// <summary>
    /// The most a tracer may live (fonctionnel/mesure-audience.md, Durées de conservation): a browser whose session
    /// never ends (restored at each launch, left open on a smartphone) does not keep its visit beyond.
    /// </summary>
    public const int MaximumMonths = 13;

    private const char Separator = '|';
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>The visit a cookie carries; <c>null</c> for no cookie, or one that is not a visit (altered, or of another form).</summary>
    public static BrowserVisit? Parse(string? value)
    {
        if (value?.Split(Separator) is not [var start, var themeDate, var themeKey] || themeKey.Length == 0
            || !TryParseDate(start, out var startDate))
            return null;
        if (themeDate.Length == 0)
            return new BrowserVisit(startDate, themeKey, ThemeDate: null);
        return TryParseDate(themeDate, out var date) ? new BrowserVisit(startDate, themeKey, date) : null;
    }

    public string Format() => string.Join(Separator,
        Start.ToString(DateFormat, CultureInfo.InvariantCulture),
        ThemeDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "",
        ThemeKey);

    /// <summary>Whether the visit has lasted its maximum duration on the given date, in universal time.</summary>
    public bool IsOver(DateOnly today) => today >= Start.AddMonths(MaximumMonths);

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
