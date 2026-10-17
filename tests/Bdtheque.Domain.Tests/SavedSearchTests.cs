using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class SavedSearchTests
{
    private const string Criteria =
        """{"kind":"group","operator":"And","conditions":[{"kind":"criterion","field":"title","comparison":"Contains","values":["Tintin"]},""" +
        """{"kind":"link","link":"editions","quantifier":{"kind":"Count","comparison":"Between","values":["1","3"]}}]}""";

    private const string TitleLocation = "/conditions/0/values/0";
    private const string CountMinLocation = "/conditions/1/quantifier/values/0";

    [Fact]
    public void Constructor_KeepsItsSearch()
    {
        var search = new SavedSearch("  Albums de Tintin  ", EntityKind.Album, Criteria, [new SearchVariable(" Titre ", TitleLocation)]);

        Assert.Equal("Albums de Tintin", search.Name);
        Assert.Equal(EntityKind.Album, search.Entity);
        Assert.Equal(Criteria, search.Criteria);
        Assert.Equal([new SearchVariable("Titre", TitleLocation)], search.Variables);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        DomainAssert.Violates(DomainRules.SavedSearchNameRequired, () => new SavedSearch(name!, EntityKind.Album, Criteria, []));
    }

    [Fact]
    public void SetName_TrimsValue()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        search.SetName("  Séries  ");

        Assert.Equal("Séries", search.Name);
    }

    [Fact]
    public void SetSearch_UndefinedEntity_Throws()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        Assert.Throws<ArgumentOutOfRangeException>(() => search.SetSearch((EntityKind)999, Criteria, []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    public void SetSearch_CriteriaNotAJsonObject_Throws(string criteria)
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        Assert.ThrowsAny<ArgumentException>(() => search.SetSearch(EntityKind.Album, criteria, []));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetSearch_VariableWithoutName_Throws(string? name)
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        DomainAssert.Violates(
            DomainRules.SavedSearchVariableNameRequired,
            () => search.SetSearch(EntityKind.Album, Criteria, [new SearchVariable(name!, TitleLocation)]));
    }

    [Fact]
    public void SetSearch_TwoVariablesOfTheSameNameIgnoringCaseAndAccents_Throws()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        DomainAssert.Violates(
            DomainRules.SavedSearchVariableNameAlreadyUsed,
            () => search.SetSearch(
                EntityKind.Album, Criteria, [new SearchVariable("Éditions", TitleLocation), new SearchVariable("editions", CountMinLocation)]));
    }

    [Fact]
    public void SetSearch_TwoVariablesOnTheSameConstant_Throws()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        Assert.Throws<ArgumentException>(() => search.SetSearch(
            EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation), new SearchVariable("Autre", TitleLocation)]));
    }

    [Theory]
    // Not a pointer, nothing there, beyond the values, not a value of a comparison, not a constant.
    [InlineData("conditions/0/values/0")]
    [InlineData("")]
    [InlineData("/conditions/2/values/0")]
    [InlineData("/conditions/0/values/1")]
    [InlineData("/conditions/0/field")]
    [InlineData("/conditions/0")]
    [InlineData("/conditions/0/values")]
    public void SetSearch_VariableNotOnAConstantOfTheCriteria_Throws(string location)
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, []);

        Assert.Throws<ArgumentException>(() => search.SetSearch(EntityKind.Album, Criteria, [new SearchVariable("Titre", location)]));
    }

    [Fact]
    public void SetSearch_ReplacesTheWholeSearch()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation)]);
        const string seriesCriteria = """{"kind":"criterion","field":"title","comparison":"StartsWith","values":["A"]}""";

        search.SetSearch(EntityKind.Series, seriesCriteria, [new SearchVariable("Début", "/values/0")]);

        Assert.Equal(EntityKind.Series, search.Entity);
        Assert.Equal(seriesCriteria, search.Criteria);
        Assert.Equal([new SearchVariable("Début", "/values/0")], search.Variables);
    }

    [Fact]
    public void DefaultValue_IsTheConstantTheVariableReplaces()
    {
        var search = new SavedSearch(
            "Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation), new SearchVariable("Minimum", CountMinLocation)]);

        Assert.Equal("Tintin", search.DefaultValueOf(search.Variables[0]));
        Assert.Equal("1", search.DefaultValueOf(search.Variables[1]));
    }

    [Fact]
    public void CriteriaWith_ReplacesTheValuesGiven_AndKeepsTheDefaultOfTheOthers()
    {
        var search = new SavedSearch(
            "Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation), new SearchVariable("Minimum", CountMinLocation)]);

        var criteria = search.CriteriaWith(new Dictionary<string, string> { ["Minimum"] = "2" });

        var replayed = new SavedSearch("Rejeu", EntityKind.Album, criteria, search.Variables);
        Assert.Equal("Tintin", replayed.DefaultValueOf(replayed.Variables[0]));
        Assert.Equal("2", replayed.DefaultValueOf(replayed.Variables[1]));
        Assert.Equal(Criteria, search.Criteria);
    }

    [Fact]
    public void CriteriaWith_NamesTheVariableAsItsDefinitionDoes_IgnoringCaseAndAccents()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, [new SearchVariable("Éditions", CountMinLocation)]);

        var criteria = search.CriteriaWith(new Dictionary<string, string> { ["EDITIONS"] = "2" });

        Assert.Equal("2", SavedSearch.DefaultValueOf(criteria, search.Variables[0]));
    }

    [Fact]
    public void CriteriaWith_TwoValuesForTheSameVariable_Throws()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation)]);

        Assert.Throws<ArgumentException>(() => search.CriteriaWith(new Dictionary<string, string> { ["Titre"] = "A", ["titre"] = "B" }));
    }

    [Fact]
    public void CriteriaWith_UnknownVariable_Throws()
    {
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation)]);

        Assert.Throws<ArgumentException>(() => search.CriteriaWith(new Dictionary<string, string> { ["Auteur"] = "Hergé" }));
    }

    [Fact]
    public void Variables_AreComparedByValue()
    {
        // The change log tells a modification by comparing the values before and after it.
        var search = new SavedSearch("Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation)]);
        var same = new SavedSearch("Albums", EntityKind.Album, Criteria, [new SearchVariable("Titre", TitleLocation)]);

        Assert.Equal(search.Variables, same.Variables);
        Assert.True(search.Variables.Equals((object)same.Variables));
        Assert.Equal(search.Variables.GetHashCode(), same.Variables.GetHashCode());
    }
}
