using System.Reflection;
using Bdtheque.Contracts.Enums;
using Bdtheque.Domain.Common;
using Bdtheque.Frontend.Components.Errors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Tests.Errors;

/// <summary>
/// The frontend never references the domain (choix-implementation/api/organisation-de-l-api.md, Contrats): only
/// these tests confront its texts with the rule codes the API can report.
/// </summary>
public sealed class RuleMessagesTests
{
    private static readonly IServiceProvider Services = new ServiceCollection().AddLogging().AddLocalization().BuildServiceProvider();

    public static TheoryData<string> RuleCodes() =>
        [.. typeof(DomainRules).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)];

    [Theory]
    [MemberData(nameof(RuleCodes))]
    public void EveryRuleCode_HasItsText(string ruleCode)
    {
        var text = Services.GetRequiredService<IStringLocalizer<RuleMessages>>()[ruleCode];

        Assert.False(text.ResourceNotFound, $"No text for the rule {ruleCode}.");
        Assert.False(string.IsNullOrWhiteSpace(text.Value));
    }

    // One rule for every name unique regardless of case and accents (modele-metier/): presented alike, reminder included, above
    // the form as under the field.
    [Theory]
    [InlineData(DomainRules.GenreLabelAlreadyUsed)]
    [InlineData(DomainRules.AuthorDisplayNameAlreadyUsed)]
    [InlineData(DomainRules.PublisherNameAlreadyUsed)]
    [InlineData(DomainRules.PublisherCollectionNameAlreadyUsed)]
    [InlineData(DomainRules.UniverseNameAlreadyUsed)]
    public void EveryNameUniqueRegardlessOfCaseAndAccents_RemindsItInBothItsTexts(string ruleCode)
    {
        var rule = Services.GetRequiredService<IStringLocalizer<RuleMessages>>()[ruleCode];
        var field = Services.GetRequiredService<IStringLocalizer<FieldMessages>>()[ruleCode, "Nom"];

        Assert.EndsWith("sans tenir compte de la casse ni des accents.", rule.Value);
        Assert.False(field.ResourceNotFound, $"No text under the field for the rule {ruleCode}.");
        Assert.Contains("est déjà pris : deux ", field.Value);
        Assert.Contains(" qui ne diffèrent que par la casse ou les accents désignent ", field.Value);
    }

    [Theory]
    [MemberData(nameof(EntityKinds))]
    public void EveryEntityKind_HasItsSingularAndPluralName(EntityKind kind)
    {
        var names = Services.GetRequiredService<IStringLocalizer<EntityNames>>();

        Assert.False(names[$"{kind}.One", 1].ResourceNotFound);
        Assert.False(names[$"{kind}.Other", 2].ResourceNotFound);
    }

    public static TheoryData<EntityKind> EntityKinds() => [.. Enum.GetValues<EntityKind>()];
}
