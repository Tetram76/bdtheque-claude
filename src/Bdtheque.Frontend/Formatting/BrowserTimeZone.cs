using Microsoft.AspNetCore.Components;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// The time zone of the browser, in which every date and time is presented to the user and read from them
/// (fonctionnel/presentation/langue-et-culture-d-affichage.md), whereas the server stores and handles them in universal time.
/// No request carries it: a script of every page keeps it in a cookie (<see cref="CookieName"/>), which each request
/// brings, so that even a page rendered statically knows it. An interactive component gets it from the request that
/// opened its circuit (<see cref="Name"/>, persisted from the prerendering).
/// </summary>
public sealed class BrowserTimeZone
{
    /// <summary>
    /// Cookie holding the IANA name of the time zone of the browser. Needed to present the data as the user asked for,
    /// it carries nothing that identifies them.
    /// </summary>
    public const string CookieName = "Bdtheque.TimeZone";

    /// <summary>IANA name of the time zone, as the browser gave it; <c>null</c> until it did.</summary>
    [PersistentState]
    public string? Name { get; set; }

    /// <summary>
    /// The time zone in which the dates and times are presented: that of the browser, or universal time as long as the
    /// browser has not given one this application knows by its IANA name (a first page, a cookie refused or altered). Only
    /// for a presentation, which a page marked as such presents again once the time zone is known: a decision or a write
    /// takes <see cref="Today"/>, which is never guessed.
    /// </summary>
    public TimeZoneInfo Zone => Known ?? TimeZoneInfo.Utc;

    private TimeZoneInfo? Known =>
        !string.IsNullOrEmpty(Name) && TimeZoneInfo.TryFindSystemTimeZoneById(Name, out var zone) && zone.HasIanaId ? zone : null;

    public DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    /// <summary>
    /// The date of the browser, which can differ from that of the server around midnight; <c>null</c> as long as the browser
    /// has not given its time zone. Never guessed: each caller decides what it does without it, rather than deciding, or
    /// recording, for a day the visitor does not live (e.g. the edition in the spotlight).
    /// </summary>
    public DateOnly? Today(TimeProvider clock) =>
        Known is { } zone ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime) : null;
}
