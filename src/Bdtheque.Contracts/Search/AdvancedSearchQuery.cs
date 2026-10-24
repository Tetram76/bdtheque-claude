namespace Bdtheque.Contracts.Search;

/// <summary>Query parameters of an advanced search, and of the replay of a saved search, beside the paging.</summary>
public static class AdvancedSearchQuery
{
    /// <summary>
    /// IANA name of the time zone in which a date and time is compared, by its day: that of the browser, in which the
    /// frontend presents the dates (fonctionnel/presentation.md § Langue et culture d'affichage). Absent, universal
    /// time. Never part of the criteria, which a saved search replays from any time zone.
    /// </summary>
    public const string TimeZoneParameter = "timeZone";
}
