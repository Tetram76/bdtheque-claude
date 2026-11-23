using System.Reflection;
using Bdtheque.Frontend.Audience;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The list of the functions the audience measurement follows, so that a function never used appears with null
/// indicators (fonctionnel/mesure-audience.md, Indicateurs): every function declared is in it, under its own key.
/// </summary>
public sealed class AudienceFunctionsTests
{
    [Fact]
    public void EveryFunctionDeclared_IsFollowed()
    {
        var declared = typeof(AudienceFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AudienceFunction))
            .Select(field => (AudienceFunction)field.GetValue(null)!);

        Assert.Equal(declared.OrderBy(f => f.Key), AudienceFunctions.Followed.OrderBy(f => f.Key));
    }

    [Fact]
    public void EveryFunction_HasItsOwnKey()
    {
        Assert.Equal(AudienceFunctions.Followed.Count, AudienceFunctions.Followed.Select(f => f.Key).Distinct(StringComparer.Ordinal).Count());
    }

    public static TheoryData<string> Keys => [.. AudienceFunctions.Followed.Select(function => function.Key)];

    // The administration presents the indicators of each function under its name: a function added without one would be
    // presented under its key.
    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryFunction_HasAName(string key) =>
        Assert.False(Names.ResourceNotFound(AudienceFunctions.Followed.Single(function => function.Key == key)), key);

    [Fact]
    public void EveryName_IsThatOfAFunction_AndNamesNoOtherOne()
    {
        var names = AudienceFunctions.Followed.Select(function => Names.Of(function.Key)).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("Liste des albums · recherche", Names.Of(AudienceFunctions.AdminAlbumsSearch.Key));
    }

    [Fact]
    public void FunctionNoLongerFollowed_KeepsItsKey() =>
        Assert.Equal("admin.former", Names.Of("admin.former"));

    private static AudienceFunctionNames Names { get; } =
        new ServiceCollection().AddLogging().AddLocalization().AddSingleton<AudienceFunctionNames>()
            .BuildServiceProvider().GetRequiredService<AudienceFunctionNames>();
}
