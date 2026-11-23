using System.Globalization;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Navigation;
using Microsoft.AspNetCore.WebUtilities;

namespace Bdtheque.Frontend.Audience;

/// <summary>The months whose indicators are read, up to the current month.</summary>
public enum AudiencePeriod
{
    CurrentMonth,

    ThreeMonths,

    /// <summary>The current month and the eleven before it: the default, a year of use.</summary>
    TwelveMonths,

    /// <summary>Every month measured, from the first one.</summary>
    All,
}

/// <summary>The users whose visits are counted: everyone, the public visitors, or the administrator (fonctionnel/mesure-audience.md).</summary>
public enum AudienceUsers
{
    All,

    Visitors,

    Administrator,
}

/// <summary>
/// The criteria of the indicators of the audience measurement in the administration (fonctionnel/mesure-audience.md,
/// Indicateurs), which the address of the page carries, the page being rendered statically as the lists: a period, and the
/// compartment of the users, the type of device and the rendering engine, each left out taking in every value.
/// </summary>
public sealed record AudienceIndicatorsQuery(
    AudiencePeriod Period = AudiencePeriod.TwelveMonths,
    AudienceUsers Users = AudienceUsers.All,
    DeviceType? Device = null,
    RenderingEngine? Engine = null,
    int Page = 1)
{
    /// <summary>
    /// The functions a page presents, each with the chart of its frequency: few enough charts to read at a glance, the most
    /// used functions on the first page.
    /// </summary>
    public const int PageSize = 20;

    // The names of the criteria in the address.
    internal const string PeriodName = "period";
    internal const string UsersName = "users";
    internal const string DeviceName = "device";
    internal const string EngineName = "engine";
    internal const string PageName = "page";

    /// <summary>
    /// The criteria of an address (its query, with or without its <c>?</c>); a value it cannot read, which only an address
    /// written by hand carries, is left out, as an unknown one. A value is read by its name, never by its number.
    /// </summary>
    public static AudienceIndicatorsQuery Parse(string query)
    {
        var values = QueryHelpers.ParseQuery(query);
        TEnum? Read<TEnum>(string name) where TEnum : struct, Enum =>
            values.TryGetValue(name, out var value) && value.ToString().Trim() is var text && !int.TryParse(text, out _)
            && Enum.TryParse<TEnum>(text, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : null;

        var page = values.TryGetValue(PageName, out var number) && int.TryParse(number, out var parsed) && parsed >= 1 ? parsed : 1;
        return new AudienceIndicatorsQuery(
            Read<AudiencePeriod>(PeriodName) ?? AudiencePeriod.TwelveMonths, Read<AudienceUsers>(UsersName) ?? AudienceUsers.All,
            Read<DeviceType>(DeviceName), Read<RenderingEngine>(EngineName), page);
    }

    /// <summary>The criteria and the page that differ from the default, in their fixed order: the parameters of the address.</summary>
    public IReadOnlyList<(string Name, string Value)> Parameters()
    {
        var parameters = Criteria();
        if (Page > 1)
            parameters.Add((PageName, Page.ToString(CultureInfo.InvariantCulture)));
        return parameters;
    }

    // The period and the compartment that differ from the default, without the page.
    private List<(string Name, string Value)> Criteria()
    {
        var parameters = new List<(string Name, string Value)>();
        if (Period != AudiencePeriod.TwelveMonths)
            parameters.Add((PeriodName, Period.ToString()));
        if (Users != AudienceUsers.All)
            parameters.Add((UsersName, Users.ToString()));
        if (Device is { } device)
            parameters.Add((DeviceName, device.ToString()));
        if (Engine is { } engine)
            parameters.Add((EngineName, engine.ToString()));
        return parameters;
    }

    /// <summary>The address of the page with these criteria.</summary>
    public string Href() => Parameters() is { Count: > 0 } parameters ? $"{AppPaths.AdminStatistics}?{Join(parameters)}" : AppPaths.AdminStatistics;

    /// <summary>
    /// The first month read, by its first day, for the current month <paramref name="currentMonth"/>; none for every month
    /// measured.
    /// </summary>
    public DateOnly? From(DateOnly currentMonth) => Period switch
    {
        AudiencePeriod.CurrentMonth => currentMonth,
        AudiencePeriod.ThreeMonths => currentMonth.AddMonths(-2),
        AudiencePeriod.TwelveMonths => currentMonth.AddMonths(-11),
        _ => null,
    };

    /// <summary>
    /// The indicators of the API with these criteria, up to the current month <paramref name="currentMonth"/>, the last one any
    /// visit can have begun in.
    /// </summary>
    public string ApiPath(DateOnly currentMonth)
    {
        var parameters = new List<(string Name, string Value)>();
        if (From(currentMonth) is { } from)
            parameters.Add(("from", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        if (Users != AudienceUsers.All)
            parameters.Add(("administrator", Users == AudienceUsers.Administrator ? "true" : "false"));
        if (Device is { } device)
            parameters.Add(("deviceType", device.ToString()));
        if (Engine is { } engine)
            parameters.Add(("renderingEngine", engine.ToString()));
        return parameters.Count == 0 ? "/admin/audience/indicators" : $"/admin/audience/indicators?{Join(parameters)}";
    }

    /// <summary>
    /// The action the request of this address records for the audience measurement (fonctionnel/mesure-audience.md, Fonctions
    /// suivies), with its criteria as its parameters, as each control sets them: a page keeps the criteria, the form of the
    /// criteria returns to the first page. None for the default criteria at the first page, reached from the menu.
    /// </summary>
    public (AudienceFunction Function, string Parameters)? Action => this switch
    {
        { Page: > 1 } => (AudienceFunctions.AdminStatisticsPage, Join(Parameters())),
        _ when Criteria() is { Count: > 0 } criteria => (AudienceFunctions.AdminStatisticsFilter, Join(criteria)),
        _ => null,
    };

    /// <summary>The address of the page <paramref name="page"/> of the functions, with the same criteria.</summary>
    public string PageHref(int page) => (this with { Page = page }).Href();

    /// <summary>The first day of the month of an instant, in universal time, the time the measurement counts its months in.</summary>
    public static DateOnly MonthOf(DateTimeOffset instant)
    {
        var universal = instant.ToUniversalTime();
        return new DateOnly(universal.Year, universal.Month, 1);
    }

    private static string Join(IEnumerable<(string Name, string Value)> parameters) =>
        string.Join('&', parameters.Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(parameter.Value)}"));
}
