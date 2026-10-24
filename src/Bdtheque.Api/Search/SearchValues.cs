using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Common;
using Microsoft.EntityFrameworkCore;
using static Bdtheque.Contracts.Search.SearchComparison;

namespace Bdtheque.Api.Search;

/// <summary>The values of a criterion, as written by the client (<see cref="SearchCriterion.Values"/>).</summary>
internal static partial class SearchValues
{
    private static readonly MethodInfo EfParameter = typeof(EF).GetMethod(nameof(EF.Parameter))!;

    /// <summary>
    /// The values <paramref name="comparison"/> needs, checked: left empty, a business error — the user
    /// fills in what the criterion compares —; too many, a technical one, the client building the criterion.
    /// </summary>
    public static IReadOnlyList<string> Of(SearchComparison comparison, IReadOnlyList<string>? values)
    {
        values ??= [];
        var (minimum, maximum) = comparison switch
        {
            IsSet or IsNotSet => (0, 0),
            Between => (2, 2),
            IsOneOf or IsNoneOf => (1, int.MaxValue),
            _ => (1, 1),
        };
        if (values.Count > maximum)
            throw new ArgumentException($"{comparison} takes at most {maximum} value(s), not {values.Count}.", nameof(values));
        if (values.Count < minimum || values.Any(string.IsNullOrWhiteSpace))
            throw new DomainRuleViolationException(DomainRules.AdvancedSearchValueMissing, $"{comparison} is missing a value.");
        return values;
    }

    /// <exception cref="FormatException">Not a number: a technical error, the client building the criterion.</exception>
    public static int ParseNumber(string value) => int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    /// <exception cref="FormatException">Neither <c>yyyy</c>, <c>yyyy-MM</c> nor <c>yyyy-MM-dd</c>: a technical error.</exception>
    public static DateValue ParseDate(string value)
    {
        var match = DatePattern().Match(value);
        if (!match.Success)
            throw new FormatException($"'{value}' is not a date written yyyy, yyyy-MM or yyyy-MM-dd.");

        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        if (!match.Groups["month"].Success)
            return new DateValue(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), SearchDatePrecision.Year);
        var month = int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture);
        var start = new DateOnly(year, month, 1);
        if (!match.Groups["day"].Success)
            return new DateValue(start, start.AddMonths(1).AddDays(-1), SearchDatePrecision.Month);
        var day = new DateOnly(year, month, int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture));
        return new DateValue(day, day, SearchDatePrecision.Day);
    }

    /// <summary>
    /// A value sent to the database as a parameter, never written into the SQL: a criterion keeps the same
    /// query, cached once, whatever its value.
    /// </summary>
    public static Expression Parameter<T>(T value) => Parameter(value, typeof(T));

    /// <summary>
    /// A value of a type known at run time only, sent as a parameter (see <see cref="Parameter{T}(T)"/>); an
    /// array remains a single parameter (<c>= ANY</c>), whatever the number of its values.
    /// </summary>
    public static Expression Parameter(object? value, Type type) =>
        Expression.Call(EfParameter.MakeGenericMethod(type), Expression.Constant(value, type));

    [GeneratedRegex(@"^(?<year>\d{4})(-(?<month>\d{2})(-(?<day>\d{2}))?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();
}

/// <summary>
/// A date written at a precision: the days from <paramref name="First"/> to <paramref name="Last"/> (e.g.
/// the whole of March 2020 for <c>2020-03</c>).
/// </summary>
internal sealed record DateValue(DateOnly First, DateOnly Last, SearchDatePrecision Precision);
