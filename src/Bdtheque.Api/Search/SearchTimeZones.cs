namespace Bdtheque.Api.Search;

/// <summary>The time zone a search is asked in (<c>AdvancedSearchQuery.TimeZoneParameter</c>), universal time without one.</summary>
internal static class SearchTimeZones
{
    /// <exception cref="BadHttpRequestException">
    /// Not the IANA name of a time zone, which PostgreSQL knows too, unlike a Windows name: a technical error, the
    /// frontend sending that of the browser.
    /// </exception>
    public static TimeZoneInfo Resolve(string? name) =>
        name is null
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.TryFindSystemTimeZoneById(name, out var zone) && zone.HasIanaId
                ? zone
                : throw new BadHttpRequestException($"'{name}' is not the IANA name of a time zone.");
}
