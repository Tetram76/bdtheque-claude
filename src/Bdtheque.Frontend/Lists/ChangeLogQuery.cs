using System.Globalization;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the change log (visuel/journal/journal-des-modifications.md), which its address carries: the search on the
/// label of the records, the type of record, the operation, the period and a single record, whose history it then is. The
/// change log has no navigation by initial, being in the order of time, and no dialog opens over it.
/// </summary>
/// <param name="From">The first day of the period, included, in the time zone of the browser.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="RecordId">The record whose history the change log is restricted to.</param>
public sealed record ChangeLogQuery(
    string? Text = null,
    RecordType? RecordType = null,
    ChangeLogOperation? Operation = null,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? RecordId = null,
    int Page = 1,
    bool FiltersOpen = false) : ListQuery(Text, null, Page, FiltersOpen)
{
    // The names of the API.
    internal const string RecordTypeName = "recordType";
    internal const string OperationName = "operation";
    internal const string FromName = "from";
    internal const string ToName = "to";
    internal const string RecordName = "record";

    // The day as an input of type date gives it and the API reads it.
    private const string DayFormat = "yyyy-MM-dd";

    private static readonly ListCriteriaFunctions Functions = new(
        AudienceFunctions.AdminChangeLogSearch, AudienceFunctions.AdminChangeLogFilter, null, AudienceFunctions.AdminChangeLogPage);

    protected override string Path => AppPaths.AdminChangeLog;

    protected override string ApiRoute => "/admin/change-log";

    protected override ListCriteriaFunctions CriteriaFunctions => Functions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (RecordTypeName, RecordType?.ToString()), (OperationName, Operation?.ToString()), (FromName, Day(From)), (ToName, Day(To)),
        (RecordName, RecordId),
    ];

    /// <summary>
    /// The page of the change log of the API with these criteria, its days in the time zone of the browser
    /// (<paramref name="timeZone"/>, its IANA name), universal time as long as the browser has not given it.
    /// </summary>
    public string ApiPath(string? timeZone) =>
        timeZone is null ? ApiPath() : $"{ApiPath()}&{AdvancedSearchQuery.TimeZoneParameter}={Uri.EscapeDataString(timeZone)}";

    /// <summary>The change log without its filters, back on its first page, its search kept: « Tout effacer ».</summary>
    public ChangeLogQuery WithoutFilters() => new(Text);

    /// <summary>The value of a day in the address, the format an input of type date reads.</summary>
    public static string? Day(DateOnly? day) => day?.ToString(DayFormat, CultureInfo.InvariantCulture);

    /// <summary>The criteria of the change log read from its address.</summary>
    public static ChangeLogQuery Parse(string query)
    {
        var address = new Address(query);
        DateOnly? DayOf(string name) =>
            DateOnly.TryParseExact(address.Text(name), DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : null;

        return new ChangeLogQuery(
            address.Search,
            address.Enum<RecordType>(RecordTypeName),
            address.Enum<ChangeLogOperation>(OperationName),
            DayOf(FromName),
            DayOf(ToName),
            address.Id(RecordName),
            address.Page,
            address.FiltersOpen);
    }
}
