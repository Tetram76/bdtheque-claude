using Bdtheque.Api.Search;
using Bdtheque.Contracts.Search;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The saved searches and the change log keep criteria in the JSON form of the advanced search contract,
/// never purged from the change log, into which the variables point: renaming a member or a discriminator
/// of the contract fails here instead of making the criteria already saved unreadable.
/// </summary>
public sealed class SearchCriteriaJsonTests
{
    private static readonly SearchCondition Criteria = new SearchGroup(SearchOperator.Or,
    [
        new SearchNot(new SearchCriterion("title", SearchComparison.Contains, ["Épée"], MatchCaseAndAccents: true)),
        new SearchLink("editions", new SearchCriterion("isbn", SearchComparison.IsSet),
            new SearchQuantifier(SearchQuantifierKind.Count, SearchComparison.Between, ["1", "3"])),
        new SearchLink("series"),
        new SearchDataComparison("firstPublicationDate", SearchComparison.LessThan, new SearchOperand("createdAt", ["series"]),
            DatePrecision: SearchDatePrecision.Month),
    ]);

    private const string Json =
        """{"kind":"group","operator":"Or","conditions":[""" +
        """{"kind":"not","condition":{"kind":"criterion","field":"title","comparison":"Contains","values":["Épée"],"matchCaseAndAccents":true}},""" +
        """{"kind":"link","link":"editions","condition":{"kind":"criterion","field":"isbn","comparison":"IsSet","matchCaseAndAccents":false},"quantifier":""" +
        """{"kind":"Count","comparison":"Between","values":["1","3"]}},""" +
        """{"kind":"link","link":"series"},""" +
        """{"kind":"dataComparison","field":"firstPublicationDate","comparison":"LessThan","other":{"field":"createdAt","links":["series"]}""" +
        ""","matchCaseAndAccents":false,"datePrecision":"Month"}]}""";

    [Fact]
    public void Criteria_HaveTheirPinnedJsonForm()
    {
        Assert.Equal(Json, SearchCriteriaJson.Serialize(Criteria));
        Assert.Equal(Json, SearchCriteriaJson.Serialize(SearchCriteriaJson.Deserialize(Json)));
    }

    [Fact]
    public void ReadingBack_DoesNotDependOnTheOrderOfTheKeys()
    {
        const string reordered = """{"conditions":[{"link":"series","kind":"link"}],"operator":"And","kind":"group"}""";

        var criteria = Assert.IsType<SearchGroup>(SearchCriteriaJson.Deserialize(reordered));

        Assert.Equal(SearchOperator.And, criteria.Operator);
        Assert.Equal(new SearchLink("series"), Assert.Single(criteria.Conditions));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("not json")]
    public void NotCriteria_AreRefused(string json)
    {
        Assert.Throws<ArgumentException>(() => SearchCriteriaJson.Deserialize(json));
    }
}
