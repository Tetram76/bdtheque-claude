using System.Linq.Expressions;
using System.Reflection;
using Bdtheque.Api.Endpoints;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Search;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using static Bdtheque.Contracts.Search.SearchComparison;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Search;

/// <summary>
/// A data of an entity a criterion compares (fonctionnel/recherche-avancee.md § Données interrogées), translated into the
/// condition of a query.
/// </summary>
/// <remarks>
/// A comparison holds only if the known data establish it (fonctionnel/recherche-avancee.md § Comparaisons): every condition
/// requires the data to be set — "is not set" aside —, so that a NOT of the criterion keeps what it does not
/// establish. EF Core gives the C# meaning to <c>null</c>, which a NOT then negates as such.
/// </remarks>
internal abstract class SearchField(string key, LambdaExpression selector, bool isOptional)
{
    public string Key => key;

    public abstract SearchDataType Type { get; }

    /// <summary>The comparisons of the type, without "is set" and "is not set".</summary>
    protected abstract IReadOnlyList<SearchComparison> TypeComparisons { get; }

    public IReadOnlyList<SearchComparison> Comparisons => isOptional ? [.. TypeComparisons, IsSet, IsNotSet] : TypeComparisons;

    /// <summary>The comparisons of the type to another data, those taking a single value (fonctionnel/recherche-avancee.md § Comparaisons).</summary>
    public virtual IReadOnlyList<SearchComparison> DataComparisons => [];

    public virtual SearchFieldDescription Describe() => new(Key, Type, isOptional, Comparisons, DataComparisons);

    /// <summary>The condition <paramref name="criterion"/> sets on <paramref name="record"/>.</summary>
    /// <exception cref="ArgumentException">
    /// A comparison the type does not allow, or a text option on another type: a technical error, the client
    /// building the criterion from the schema.
    /// </exception>
    public Expression Build(Expression record, SearchCriterion criterion)
    {
        if (!Comparisons.Contains(criterion.Comparison))
            throw new ArgumentException($"{Key} cannot be compared by {criterion.Comparison}.", nameof(criterion));
        if (criterion.MatchCaseAndAccents && Type != SearchDataType.Text)
            throw new ArgumentException($"{Key} is not a text, whose case and accents could be matched.", nameof(criterion));

        var values = SearchValues.Of(criterion.Comparison, criterion.Values);
        var data = Data(record);
        return criterion.Comparison switch
        {
            IsSet => IsSetCondition(data),
            IsNotSet => Expression.Not(IsSetCondition(data)),
            _ => Expression.AndAlso(IsSetCondition(data), Compare(record, data, criterion, values)),
        };
    }

    /// <summary>
    /// The condition <paramref name="comparison"/> sets on <paramref name="record"/>, comparing its data to
    /// <paramref name="other"/> of <paramref name="otherRecord"/>: both must be set.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A comparison the type does not allow to another data, a data of another type, an option the type does not
    /// take, or a date without its precision: a technical error, the client building the criterion from the
    /// schema.
    /// </exception>
    public Expression Build(Expression record, SearchDataComparison comparison, SearchField other, Expression otherRecord)
    {
        if (!DataComparisons.Contains(comparison.Comparison))
            throw new ArgumentException($"{Key} cannot be compared to another data by {comparison.Comparison}.", nameof(comparison));
        if (!IsComparableTo(other))
            throw new ArgumentException($"{Key} cannot be compared to {other.Key}, of another type.", nameof(other));
        if (comparison.MatchCaseAndAccents && Type != SearchDataType.Text)
            throw new ArgumentException($"{Key} is not a text, whose case and accents could be matched.", nameof(comparison));
        if (comparison.DatePrecision is null == (Type == SearchDataType.Date))
            throw new ArgumentException($"A date, and only a date, is compared to another at a precision ({Key}).", nameof(comparison));

        var data = Data(record);
        var otherData = other.Data(otherRecord);
        return Expression.AndAlso(
            Expression.AndAlso(IsSetCondition(data), IsSetCondition(otherData)),
            CompareData(record, data, comparison, other, otherRecord, otherData));
    }

    /// <summary>The comparison of <paramref name="data"/> of <paramref name="record"/>, known to be set.</summary>
    protected abstract Expression Compare(Expression record, Expression data, SearchCriterion criterion, IReadOnlyList<string> values);

    /// <summary>Whether <paramref name="other"/> is of the type of this data, to which it compares.</summary>
    protected virtual bool IsComparableTo(SearchField other) => other.GetType() == GetType();

    /// <summary>
    /// The comparison of <paramref name="data"/> of <paramref name="record"/> to <paramref name="otherData"/> of
    /// <paramref name="otherRecord"/>, of the same type, both known to be set.
    /// </summary>
    protected virtual Expression CompareData(
        Expression record, Expression data, SearchDataComparison comparison, SearchField other, Expression otherRecord, Expression otherData) =>
        throw new ArgumentOutOfRangeException(nameof(comparison));

    /// <summary>The comparison of two values of an order (numbers, periods of dates) by one that takes a single value.</summary>
    protected static Expression Ordered(SearchComparison comparison, Expression left, Expression right) => comparison switch
    {
        EqualTo => Expression.Equal(left, right),
        NotEqualTo => Expression.NotEqual(left, right),
        LessThan => Expression.LessThan(left, right),
        LessThanOrEqualTo => Expression.LessThanOrEqual(left, right),
        GreaterThan => Expression.GreaterThan(left, right),
        GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(left, right),
        _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Not a comparison taking a single value."),
    };

    // Lifted to its nullable type, the data compares the same way whether it is optional or not.
    private Expression Data(Expression record)
    {
        var data = ReplacingExpressionVisitor.Replace(selector.Parameters[0], record, selector.Body);
        return data.Type.IsValueType && Nullable.GetUnderlyingType(data.Type) is null
            ? Expression.Convert(data, typeof(Nullable<>).MakeGenericType(data.Type))
            : data;
    }

    private static Expression IsSetCondition(Expression data) => Expression.NotEqual(data, Expression.Constant(null, data.Type));
}

/// <summary>A text, compared ignoring case and accents unless the criterion asks otherwise.</summary>
/// <param name="normalize">
/// Applied to the value as the selector applies it to the data (e.g. the separators of an ISBN, ignored on
/// both sides).
/// </param>
internal sealed class TextField(string key, LambdaExpression selector, bool isOptional, Func<string, string>? normalize = null)
    : SearchField(key, selector, isOptional)
{
    private const string LikeEscapeCharacter = "\\";

    // The column collation is deterministic: a LIKE or an equality on it respects case and accents.
    private static readonly Expression<Func<string, string, bool>> Like = (text, pattern) => EF.Functions.Like(text, pattern, LikeEscapeCharacter);
    private static readonly Expression<Func<string, string>> IgnoringCaseAndAccents =
        text => EF.Functions.Collate(text, BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation);

    public override SearchDataType Type => SearchDataType.Text;

    private static readonly MethodInfo Replace = typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;
    private static readonly MethodInfo Concat = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;

    protected override IReadOnlyList<SearchComparison> TypeComparisons => [Contains, DoesNotContain, StartsWith, EqualTo, NotEqualTo];

    public override IReadOnlyList<SearchComparison> DataComparisons => TypeComparisons;

    // The pattern is built from the other text as stored: PostgreSQL refuses replace() on a nondeterministic
    // collation, and the collation set explicitly on the compared text prevails over that of the column.
    protected override Expression CompareData(
        Expression record, Expression data, SearchDataComparison comparison, SearchField other, Expression otherRecord, Expression otherData)
    {
        var text = comparison.MatchCaseAndAccents ? data : Inline(IgnoringCaseAndAccents, data);
        var escaped = new[] { LikeEscapeCharacter, "%", "_" }.Aggregate(
            otherData,
            (escaping, character) => Expression.Call(
                escaping, Replace, Expression.Constant(character), Expression.Constant(LikeEscapeCharacter + character)));
        Expression Joined(Expression left, Expression right) => Expression.Call(Concat, left, right);
        var anywhere = Joined(Joined(Expression.Constant("%"), escaped), Expression.Constant("%"));
        return comparison.Comparison switch
        {
            Contains => Inline(Like, text, anywhere),
            DoesNotContain => Expression.Not(Inline(Like, text, anywhere)),
            StartsWith => Inline(Like, text, Joined(escaped, Expression.Constant("%"))),
            EqualTo => Expression.Equal(text, otherData),
            NotEqualTo => Expression.NotEqual(text, otherData),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison)),
        };
    }

    protected override Expression Compare(Expression record, Expression data, SearchCriterion criterion, IReadOnlyList<string> values)
    {
        var value = normalize is null ? values[0] : normalize(values[0]);
        var text = criterion.MatchCaseAndAccents ? data : Inline(IgnoringCaseAndAccents, data);
        return criterion.Comparison switch
        {
            Contains => LikeOf(text, $"%{CatalogFilters.EscapeLike(value)}%"),
            DoesNotContain => Expression.Not(LikeOf(text, $"%{CatalogFilters.EscapeLike(value)}%")),
            StartsWith => LikeOf(text, $"{CatalogFilters.EscapeLike(value)}%"),
            EqualTo => Expression.Equal(text, SearchValues.Parameter(value)),
            NotEqualTo => Expression.NotEqual(text, SearchValues.Parameter(value)),
            _ => throw new ArgumentOutOfRangeException(nameof(criterion)),
        };
    }

    private static Expression LikeOf(Expression text, string pattern) => Inline(Like, text, SearchValues.Parameter(pattern));

    private static Expression Inline(LambdaExpression lambda, params Expression[] arguments)
    {
        var body = lambda.Body;
        for (var i = 0; i < arguments.Length; i++)
            body = ReplacingExpressionVisitor.Replace(lambda.Parameters[i], arguments[i], body);
        return body;
    }
}

/// <summary>A whole number.</summary>
internal sealed class NumberField(string key, LambdaExpression selector, bool isOptional) : SearchField(key, selector, isOptional)
{
    public static readonly IReadOnlyList<SearchComparison> NumberComparisons =
        [EqualTo, NotEqualTo, LessThan, LessThanOrEqualTo, GreaterThan, GreaterThanOrEqualTo, Between];

    public override SearchDataType Type => SearchDataType.Number;

    /// <summary>The comparisons of a number or a date to another data: those of a number but "between", which takes two values.</summary>
    public static readonly IReadOnlyList<SearchComparison> OrderComparisons =
        [EqualTo, NotEqualTo, LessThan, LessThanOrEqualTo, GreaterThan, GreaterThanOrEqualTo];

    protected override IReadOnlyList<SearchComparison> TypeComparisons => NumberComparisons;

    public override IReadOnlyList<SearchComparison> DataComparisons => OrderComparisons;

    protected override Expression Compare(Expression record, Expression data, SearchCriterion criterion, IReadOnlyList<string> values) =>
        CompareNumber(Expression.Convert(data, typeof(int)), criterion.Comparison, values);

    protected override Expression CompareData(
        Expression record, Expression data, SearchDataComparison comparison, SearchField other, Expression otherRecord, Expression otherData) =>
        Ordered(comparison.Comparison, Expression.Convert(data, typeof(int)), Expression.Convert(otherData, typeof(int)));

    /// <summary>The comparison of a number, set, to the values written for it (also that of a number of related records).</summary>
    public static Expression CompareNumber(Expression number, SearchComparison comparison, IReadOnlyList<string> values)
    {
        Expression ValueAt(int index) => SearchValues.Parameter(SearchValues.ParseNumber(values[index]));
        return comparison switch
        {
            EqualTo => Expression.Equal(number, ValueAt(0)),
            NotEqualTo => Expression.NotEqual(number, ValueAt(0)),
            LessThan => Expression.LessThan(number, ValueAt(0)),
            LessThanOrEqualTo => Expression.LessThanOrEqual(number, ValueAt(0)),
            GreaterThan => Expression.GreaterThan(number, ValueAt(0)),
            GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(number, ValueAt(0)),
            Between => Expression.AndAlso(Expression.GreaterThanOrEqual(number, ValueAt(0)), Expression.LessThanOrEqual(number, ValueAt(1))),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Not a comparison of numbers."),
        };
    }
}

/// <summary>A data compared by identity to one or several values: a boolean, a member of an enumeration, a record.</summary>
/// <param name="parse">The value, of the type of the data, that a client wrote.</param>
internal abstract class IdentityField(string key, LambdaExpression selector, bool isOptional, Func<string, object> parse)
    : SearchField(key, selector, isOptional)
{
    protected override IReadOnlyList<SearchComparison> TypeComparisons => [Is, IsNot, IsOneOf, IsNoneOf];

    public override IReadOnlyList<SearchComparison> DataComparisons => [Is, IsNot];

    protected override Expression CompareData(
        Expression record, Expression data, SearchDataComparison comparison, SearchField other, Expression otherRecord, Expression otherData) =>
        comparison.Comparison == Is ? Expression.Equal(data, otherData) : Expression.NotEqual(data, otherData);

    // The values form a single array parameter (= ANY), whatever their number: the same query, cached once.
    protected override Expression Compare(Expression record, Expression data, SearchCriterion criterion, IReadOnlyList<string> values)
    {
        var array = Array.CreateInstance(data.Type, values.Count);
        for (var i = 0; i < values.Count; i++)
            array.SetValue(parse(values[i]), i);
        var isOneOf = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Contains), [data.Type], SearchValues.Parameter(array, array.GetType()), data);
        return criterion.Comparison is Is or IsOneOf ? isOneOf : Expression.Not(isOneOf);
    }
}

internal sealed class BooleanField(string key, LambdaExpression selector, bool isOptional)
    : IdentityField(key, selector, isOptional, value => bool.Parse(value))
{
    public override SearchDataType Type => SearchDataType.Boolean;
}

/// <summary>A member of an enumeration of the domain, written by the name of its counterpart in the contracts.</summary>
internal sealed class EnumerationField<TDomain, TContract>(string key, LambdaExpression selector, bool isOptional)
    : IdentityField(key, selector, isOptional, Parse)
    where TDomain : struct, Enum
    where TContract : struct, Enum
{
    public override SearchDataType Type => SearchDataType.Enumeration;

    public override SearchFieldDescription Describe() =>
        base.Describe() with { Enumeration = typeof(TContract).Name, Members = Enum.GetNames<TContract>() };

    /// <exception cref="ArgumentException">Not a member: a technical error, the client listing the members from the schema.</exception>
    private static object Parse(string value) =>
        Enum.GetNames<TContract>().Contains(value, StringComparer.Ordinal)
            ? EnumMapping.Map<TDomain>(Enum.Parse<TContract>(value))!.Value
            : throw new ArgumentException($"'{value}' is not a member of {typeof(TContract).Name}.", nameof(value));
}

/// <summary>A record, by its identifier.</summary>
internal sealed class RecordField(string key, LambdaExpression selector, bool isOptional, EntityKind record)
    : IdentityField(key, selector, isOptional, value => Guid.Parse(value))
{
    public override SearchDataType Type => SearchDataType.Record;

    /// <summary>The entity of the records the values of a criterion designate.</summary>
    public EntityKind Record => record;

    public override SearchFieldDescription Describe() => base.Describe() with { Record = record };

    protected override bool IsComparableTo(SearchField other) => other is RecordField field && field.Record == record;
}

/// <summary>
/// An amount: only whether it is set, the comparison of its value joining the search with its conversion
/// into euros (suivi-implementation.md, Phase 4).
/// </summary>
internal sealed class AmountField(string key, LambdaExpression selector) : SearchField(key, selector, isOptional: true)
{
    public override SearchDataType Type => SearchDataType.Amount;

    protected override IReadOnlyList<SearchComparison> TypeComparisons => [];

    protected override Expression Compare(Expression record, Expression data, SearchCriterion criterion, IReadOnlyList<string> values) =>
        throw new ArgumentOutOfRangeException(nameof(criterion));
}

/// <summary>
/// A date, known to a precision of its own and compared at that of the value (fonctionnel/recherche-avancee.md § Comparaisons):
/// both are periods — a year, a month, a day —, and a comparison holds only if it holds for every day of the
/// period of the data. Equal: within the period of the value; different: outside it; before or after: before
/// or after the whole of it.
/// </summary>
internal abstract class DateField(string key, LambdaExpression selector, bool isOptional, SearchDatePrecision precision)
    : SearchField(key, selector, isOptional)
{
    public override SearchDataType Type => SearchDataType.Date;

    protected override IReadOnlyList<SearchComparison> TypeComparisons => NumberField.NumberComparisons;

    public override SearchFieldDescription Describe() => base.Describe() with { DatePrecision = precision };

    protected override Expression Compare(Expression record, Expression data, SearchCriterion criterion, IReadOnlyList<string> values)
    {
        var (first, last) = Bounds(record, data);
        var dates = values.Select(SearchValues.ParseDate).ToList();
        Expression StartOf(int index) => Start(dates[index]);
        Expression EndOf(int index) => End(dates[index]);
        return criterion.Comparison switch
        {
            EqualTo => Expression.AndAlso(Expression.GreaterThanOrEqual(first, StartOf(0)), Expression.LessThanOrEqual(last, EndOf(0))),
            NotEqualTo => Expression.OrElse(Expression.LessThan(last, StartOf(0)), Expression.GreaterThan(first, EndOf(0))),
            LessThan => Expression.LessThan(last, StartOf(0)),
            LessThanOrEqualTo => Expression.LessThanOrEqual(last, EndOf(0)),
            GreaterThan => Expression.GreaterThan(first, EndOf(0)),
            GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(first, StartOf(0)),
            Between => Expression.AndAlso(Expression.GreaterThanOrEqual(first, StartOf(0)), Expression.LessThanOrEqual(last, EndOf(1))),
            _ => throw new ArgumentOutOfRangeException(nameof(criterion)),
        };
    }

    public override IReadOnlyList<SearchComparison> DataComparisons => NumberField.OrderComparisons;

    protected override bool IsComparableTo(SearchField other) => other is DateField;

    // Each date covers the periods of the precision chosen from that of its first day to that of its last:
    // several of them for a date known less precisely. Equal: both one and the same period; before or after:
    // every period of one before or after every period of the other.
    protected override Expression CompareData(
        Expression record, Expression data, SearchDataComparison comparison, SearchField other, Expression otherRecord, Expression otherData)
    {
        var comparisonPrecision = comparison.DatePrecision!.Value;
        var (first, last) = Periods(Days(record, data), comparisonPrecision);
        var (otherFirst, otherLast) = Periods(((DateField)other).Days(otherRecord, otherData), comparisonPrecision);
        return comparison.Comparison switch
        {
            EqualTo => Expression.AndAlso(Expression.LessThanOrEqual(last, otherFirst), Expression.GreaterThanOrEqual(first, otherLast)),
            NotEqualTo => Expression.OrElse(Expression.LessThan(last, otherFirst), Expression.GreaterThan(first, otherLast)),
            LessThan => Expression.LessThan(last, otherFirst),
            LessThanOrEqualTo => Expression.LessThanOrEqual(last, otherFirst),
            GreaterThan => Expression.GreaterThan(first, otherLast),
            GreaterThanOrEqualTo => Expression.GreaterThanOrEqual(first, otherLast),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison)),
        };
    }

    /// <summary>The first and the last moment of the period of the data, set.</summary>
    protected abstract (Expression First, Expression Last) Bounds(Expression record, Expression data);

    /// <summary>
    /// The first and the last day of the period of the data, set, as numbers <c>yyyyMMdd</c>, which compare
    /// whatever the kind of date.
    /// </summary>
    protected abstract (Expression First, Expression Last) Days(Expression record, Expression data);

    /// <summary>The day of a date that has a year, a month and a day, as a number <c>yyyyMMdd</c>.</summary>
    protected static Expression DayNumber(Expression date) =>
        Expression.Add(
            Expression.Add(
                Expression.Multiply(Expression.Property(date, nameof(DateOnly.Year)), Expression.Constant(10_000)),
                Expression.Multiply(Expression.Property(date, nameof(DateOnly.Month)), Expression.Constant(100))),
            Expression.Property(date, nameof(DateOnly.Day)));

    // The number yyyyMMdd of the first day of the period each day falls in.
    private static (Expression First, Expression Last) Periods((Expression First, Expression Last) days, SearchDatePrecision precision)
    {
        var length = precision switch
        {
            SearchDatePrecision.Year => 10_000,
            SearchDatePrecision.Month => 100,
            SearchDatePrecision.Day => 1,
            // An integer outside the enumeration, which the JSON converter accepts: a technical error, as any value the schema does not describe.
            _ => throw new ArgumentOutOfRangeException(nameof(precision), precision, "Not a precision of a date."),
        };
        Expression PeriodOf(Expression day) =>
            length == 1 ? day : Expression.Multiply(Expression.Divide(day, Expression.Constant(length)), Expression.Constant(length));
        return (PeriodOf(days.First), PeriodOf(days.Last));
    }

    /// <summary>The first moment of the period of a value, comparable to the bounds of the data.</summary>
    protected abstract Expression Start(DateValue value);

    /// <summary>The last moment of the period of a value, comparable to the bounds of the data.</summary>
    protected abstract Expression End(DateValue value);
}

/// <summary>A date known to the day.</summary>
internal sealed class DayField(string key, LambdaExpression selector, bool isOptional)
    : DateField(key, selector, isOptional, SearchDatePrecision.Day)
{
    protected override (Expression First, Expression Last) Bounds(Expression record, Expression data)
    {
        var day = Expression.Convert(data, typeof(DateOnly));
        return (day, day);
    }

    protected override (Expression First, Expression Last) Days(Expression record, Expression data)
    {
        var day = DayNumber(Expression.Convert(data, typeof(DateOnly)));
        return (day, day);
    }

    protected override Expression Start(DateValue value) => SearchValues.Parameter(value.First);

    protected override Expression End(DateValue value) => SearchValues.Parameter(value.Last);
}

/// <summary>
/// A date and time, compared to dates: its day is that of universal time, as it is stored — the time zone in
/// which the application presents it is yet to be set (suivi-implementation.md, Phase 3).
/// </summary>
internal sealed class InstantField(string key, LambdaExpression selector, bool isOptional)
    : DateField(key, selector, isOptional, SearchDatePrecision.Day)
{
    protected override (Expression First, Expression Last) Bounds(Expression record, Expression data)
    {
        var instant = Expression.Convert(data, typeof(DateTimeOffset));
        return (instant, instant);
    }

    protected override (Expression First, Expression Last) Days(Expression record, Expression data)
    {
        var day = DayNumber(Expression.Property(Expression.Convert(data, typeof(DateTimeOffset)), nameof(DateTimeOffset.UtcDateTime)));
        return (day, day);
    }

    protected override Expression Start(DateValue value) => SearchValues.Parameter(StartOf(value.First));

    // The last microsecond of the day, the precision of PostgreSQL.
    protected override Expression End(DateValue value) => SearchValues.Parameter(StartOf(value.Last.AddDays(1)).AddTicks(-10));

    private static DateTimeOffset StartOf(DateOnly day) => new(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}

/// <summary>
/// A partial date, a year with or without its month (modele-metier.md § Album), or a year alone. Its period
/// and that of the value compare as numbers <c>yyyyMMdd</c>, every month ending on a 31st: the comparisons
/// only need the order of the periods, which a missing day does not change.
/// </summary>
/// <param name="month">The month of <paramref name="year"/>; <c>null</c> for a data known to the year only.</param>
internal sealed class PartialDateField(string key, LambdaExpression year, LambdaExpression? month)
    : DateField(key, year, isOptional: true, month is null ? SearchDatePrecision.Year : SearchDatePrecision.Month)
{
    private const int LastDay = 31;

    protected override (Expression First, Expression Last) Bounds(Expression record, Expression data)
    {
        var yearPart = Expression.Multiply(Expression.Convert(data, typeof(int)), Expression.Constant(10_000));
        Expression MonthOr(int fallback) =>
            month is null
                ? Expression.Constant(fallback)
                : Expression.Coalesce(ReplacingExpressionVisitor.Replace(month.Parameters[0], record, month.Body), Expression.Constant(fallback));
        Expression Key(Expression monthOfYear, int day) =>
            Expression.Add(Expression.Add(yearPart, Expression.Multiply(monthOfYear, Expression.Constant(100))), Expression.Constant(day));
        return (Key(MonthOr(1), 1), Key(MonthOr(12), LastDay));
    }

    // A month ending on a 31st keeps the order of days of a date known to the day, none after the 31st.
    protected override (Expression First, Expression Last) Days(Expression record, Expression data) => Bounds(record, data);

    protected override Expression Start(DateValue value) => SearchValues.Parameter(KeyOf(value.First, value.First.Day));

    protected override Expression End(DateValue value) =>
        SearchValues.Parameter(KeyOf(value.Last, value.Last.Day == DateTime.DaysInMonth(value.Last.Year, value.Last.Month) ? LastDay : value.Last.Day));

    private static int KeyOf(DateOnly date, int day) => (date.Year * 10_000) + (date.Month * 100) + day;
}
