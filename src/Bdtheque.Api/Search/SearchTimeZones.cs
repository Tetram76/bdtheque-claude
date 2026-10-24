namespace Bdtheque.Api.Search;

/// <summary>The time zone a search is asked in (<c>AdvancedSearchQuery.TimeZoneParameter</c>), universal time without one.</summary>
internal static class SearchTimeZones
{
    /// <exception cref="BadHttpRequestException">
    /// Not the IANA name of a time zone, which PostgreSQL knows too, unlike a Windows name: a technical error, the
    /// frontend sending that of the browser.
    /// </exception>
    public static TimeZoneInfo Resolve(string? name)
    {
        if (name is null)
            return TimeZoneInfo.Utc;
        if (TimeZoneInfo.TryFindSystemTimeZoneById(name, out var zone) && zone.HasIanaId)
            return zone;
        throw new BadHttpRequestException($"'{name}' is not the IANA name of a time zone.");
    }
}
