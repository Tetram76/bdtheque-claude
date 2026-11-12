using System.Globalization;

namespace Bdtheque.Frontend.Visits;

/// <summary>
/// The visit of the browser: its browsing session, shared by all its tabs and windows, another browser making another
/// visit (fonctionnel/mesure-audience.md, Visites). Kept in a session cookie (<see cref="CookieName"/>), which the
/// browser forgets with its session: the single delimitation of the visit, for its theme
/// (fonctionnel/design-et-charte-graphique.md, Thème actif) as for the audience measurement. It carries nothing that
/// identifies the user.
/// </summary>
/// <param name="Start">Date of the start of the visit, in universal time, which bounds its duration (<see cref="IsOver"/>).</param>
/// <param name="ThemeKey">Theme of the visit.</param>
/// <param name="ThemeDate">
/// Date of the browser for which the theme was drawn; unknown for a browser that does not give its time zone.
/// </param>
/// <param name="AudienceVisit">
/// The visit the audience measurement records the usages of this one in; none before its first usage recorded, nor
/// while the visitor opposes the measurement.
/// </param>
public sealed record BrowserVisit(DateOnly Start, string ThemeKey, DateOnly? ThemeDate, MeasuredVisit? AudienceVisit = null)
{
    public const string CookieName = "Bdtheque.Visit";

    private const string Administrator = "admin";

    /// <summary>
    /// The most a tracer may live (fonctionnel/mesure-audience.md, Durées de conservation): a browser whose session
    /// never ends (restored at each launch, left open on a smartphone) does not keep its visit beyond.
    /// </summary>
    public const int MaximumMonths = 13;

    private const char Separator = '|';
    private const string DateFormat = "yyyy-MM-dd";
    private const string GuidFormat = "N";

    /// <summary>The visit a cookie carries; <c>null</c> for no cookie, or one that is not a visit (altered, or of another form).</summary>
    public static BrowserVisit? Parse(string? value)
    {
        if (value?.Split(Separator) is not [var start, var themeDate, var themeKey, var audienceVisitId, var audienceVisitOf]
            || themeKey.Length == 0 || !TryParseDate(start, out var startDate))
            return null;

        MeasuredVisit? audienceVisit = null;
        if (audienceVisitId.Length > 0)
        {
            if (!Guid.TryParseExact(audienceVisitId, GuidFormat, out var id) || audienceVisitOf is not ("" or Administrator))
                return null;
            audienceVisit = new MeasuredVisit(id, audienceVisitOf == Administrator);
        }
        else if (audienceVisitOf.Length > 0)
            return null;

        if (themeDate.Length == 0)
            return new BrowserVisit(startDate, themeKey, ThemeDate: null, audienceVisit);
        return TryParseDate(themeDate, out var date) ? new BrowserVisit(startDate, themeKey, date, audienceVisit) : null;
    }

    public string Format() => string.Join(Separator,
        Start.ToString(DateFormat, CultureInfo.InvariantCulture),
        ThemeDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "",
        ThemeKey,
        AudienceVisit?.Id.ToString(GuidFormat, CultureInfo.InvariantCulture) ?? "",
        AudienceVisit is { OfAdministrator: true } ? Administrator : "");

    /// <summary>Whether the visit has lasted its maximum duration on the given date, in universal time.</summary>
    public bool IsOver(DateOnly today) => today >= Start.AddMonths(MaximumMonths);

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
