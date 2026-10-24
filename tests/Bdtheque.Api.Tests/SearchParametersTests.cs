using Bdtheque.Api.Search;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Bdtheque.Contracts.Search.SearchComparison;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The values of the criteria are sent to the database as parameters, never written into the SQL: a same form
/// of criteria remains a single query, cached once, whatever its values, their number included
/// (choix-implementation/recherche-avancee.md § Recherche avancée : mise en œuvre, Traduction).
/// </summary>
public sealed class SearchParametersTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SearchParametersTests(ApiWebApplicationFactory factory) => _factory = factory;

    // Each sort of value: the same criteria with two sets of values, and the values that must not be in the SQL.
    private static readonly Dictionary<string, (EntityKind Entity, SearchCondition Criteria, SearchCondition OtherValues, string[] Values)> Cases = new()
    {
        ["text"] = (EntityKind.Album, Criterion("title", EqualTo, "zqxalpha"), Criterion("title", EqualTo, "zqxbeta"), ["zqxalpha", "zqxbeta"]),
        ["pattern"] = (EntityKind.Album, Criterion("title", Contains, "zqxalpha"), Criterion("title", Contains, "zqxbeta"), ["zqxalpha", "zqxbeta"]),
        ["number"] = (EntityKind.Album, Criterion("volumeNumber", Between, "314159", "271828"), Criterion("volumeNumber", Between, "161803", "141421"),
            ["314159", "271828", "161803", "141421"]),
        ["partial date"] = (EntityKind.Album, Criterion("firstPublicationDate", Between, "1687-04", "1789-07"),
            Criterion("firstPublicationDate", Between, "1515-09", "1610-05"), ["1687", "1789", "1515", "1610"]),
        ["day"] = (EntityKind.Edition, Criterion("acquisitionDate", Between, "1687-04-12", "1789-07-14"),
            Criterion("acquisitionDate", Between, "1515-09-13", "1610-05-14"), ["1687", "1789", "1515", "1610"]),
        ["date and time"] = (EntityKind.Album, Criterion("createdAt", Between, "1687-04-12", "1789-07-14"),
            Criterion("createdAt", Between, "1515-09-13", "1610-05-14"), ["1687", "1789", "1515", "1610"]),
        ["quantifier"] = (EntityKind.Album, new SearchLink("editions", null, new SearchQuantifier(SearchQuantifierKind.Count, GreaterThan, ["314159"])),
            new SearchLink("editions", null, new SearchQuantifier(SearchQuantifierKind.Count, GreaterThan, ["271828"])), ["314159", "271828"]),
        ["members"] = (EntityKind.Album, Criterion("rating", IsOneOf, "Good", "Poor"), Criterion("rating", IsOneOf, "VeryGood", "Good", "Average", "Poor", "VeryPoor"), []),
    };

    // By their name only, which the test explorer lists one by one, unlike criteria it cannot serialize.
    public static TheoryData<string> CaseNames => [.. Cases.Keys];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task Values_AreParameters_KeepingTheSameQuery(string kind)
    {
        var (entity, criteria, otherValues, values) = Cases[kind];
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();

        var sql = Sql(context, entity, criteria).Query;

        Assert.Equal(sql, Sql(context, entity, otherValues).Query);
        Assert.All(values, value => Assert.DoesNotContain(value, sql));
        Assert.True(sql.Contains('@'), kind);
    }

    /// <summary>The values of "is one of" form a single parameter, whatever their number.</summary>
    [Fact]
    public async Task Values_OfIsOneOf_AreASingleParameter()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var few = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid().ToString()).ToArray();
        var many = Enumerable.Range(0, 70).Select(_ => Guid.NewGuid().ToString()).ToArray();

        var (query, parameterCount) = Sql(context, EntityKind.Album, Criterion("record", IsOneOf, few));

        Assert.Equal(1, parameterCount);
        Assert.Equal(query, Sql(context, EntityKind.Album, Criterion("record", IsOneOf, many)).Query);
        Assert.All(few, value => Assert.DoesNotContain(value, query));
    }

    private static SearchCriterion Criterion(string field, SearchComparison comparison, params string[] values) => new(field, comparison, values);

    private static (string Query, int ParameterCount) Sql(BdthequeDbContext context, EntityKind entity, SearchCondition criteria) => entity switch
    {
        EntityKind.Album => Sql<Album>(context, entity, criteria),
        EntityKind.Edition => Sql<Edition>(context, entity, criteria),
        _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "No case of the tests searches it."),
    };

    // The SQL of the query, without the declarations of its parameters that ToQueryString writes before it as
    // comments, and the number of those parameters.
    private static (string Query, int ParameterCount) Sql<T>(BdthequeDbContext context, EntityKind entity, SearchCondition criteria)
        where T : class
    {
        var condition = SearchTranslator.Translate<T>(AdvancedSearchSchema.Of(entity), criteria, new SearchScope(context, 0));
        var lines = context.Set<T>().AsNoTracking().Where(condition).ToQueryString().Split('\n');
        var declarations = lines.TakeWhile(line => line.StartsWith("-- ", StringComparison.Ordinal)).Count();
        return (string.Join('\n', lines.Skip(declarations)), declarations);
    }
}
