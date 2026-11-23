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

    // The filter by screen presents a screen with its actions, by the group their names give
    // (AudienceFunctionNames.ScreenOf): the group of every function is that of a screen followed, a variant of a screen
    // included (« Saisie d'un album · nouvel album »), so that no action is left out of the screen it is done on. The only
    // exception is the logout, done from any page of the administration.
    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryFunction_BelongsToAScreen_ByItsName(string key)
    {
        var screens = AudienceFunctions.Followed
            .Where(function => function.Kind == AudienceFunctionKind.Screen)
            .Select(function => AudienceFunctionNames.ScreenOf(Names.Of(function.Key)))
            .ToHashSet(StringComparer.Ordinal);
        string[] withoutScreen = [Names.Of(AudienceFunctions.AdminSignedOut.Key)];

        var name = Names.Of(key);

        Assert.True(screens.Contains(AudienceFunctionNames.ScreenOf(name)) || withoutScreen.Contains(name), name);
    }

    // The entry form of a new record is a variant of that of a record: the creation done on it is one of its actions.
    public static TheoryData<string, string> EntryForms => new()
    {
        { AudienceFunctions.AdminAlbum.Key, AudienceFunctions.AdminNewAlbum.Key },
        { AudienceFunctions.AdminSeriesForm.Key, AudienceFunctions.AdminNewSeriesForm.Key },
        { AudienceFunctions.AdminPublisherForm.Key, AudienceFunctions.AdminNewPublisherForm.Key },
        { AudienceFunctions.AdminEditionForm.Key, AudienceFunctions.AdminNewEditionForm.Key },
        { AudienceFunctions.AdminPurchaseIntentForm.Key, AudienceFunctions.AdminNewPurchaseIntentForm.Key },
    };

    [Theory]
    [MemberData(nameof(EntryForms))]
    public void NewRecordForm_BelongsToTheScreenOfItsEntryForm(string form, string newForm) =>
        Assert.Equal(AudienceFunctionNames.ScreenOf(Names.Of(form)), AudienceFunctionNames.ScreenOf(Names.Of(newForm)));

    [Fact]
    public void FunctionNoLongerFollowed_KeepsItsKey() =>
        Assert.Equal("admin.former", Names.Of("admin.former"));

    private static AudienceFunctionNames Names { get; } =
        new ServiceCollection().AddLogging().AddLocalization().AddSingleton<AudienceFunctionNames>()
            .BuildServiceProvider().GetRequiredService<AudienceFunctionNames>();
}
