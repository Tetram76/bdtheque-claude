extern alias frontend;

using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The themes a run of the end-to-end tests covers. A test whose result may depend on the theme fixes the theme of its
/// pages and runs in each offered theme, never in the one the application would draw (gestion-projet/gouvernance/
/// qualite-du-code.md): its rows come from <see cref="Covered"/>, which a theme added to the catalog joins on its own.
/// </summary>
public static class TestThemes
{
    /// <summary>
    /// The theme a job of the CI covers, the tests being shared out by theme between its jobs
    /// (choix-implementation/socle/themes.md, captures de référence); unset, a run covers every offered theme.
    /// </summary>
    public const string VariableName = "BDTHEQUE_TEST_THEME";

    /// <summary>
    /// The trait of the tests run in each covered theme: the jobs of the CI but one run only them, the remaining one
    /// runs as well the tests that do not depend on the theme, or that check a theme of their own, once per push.
    /// </summary>
    public const string Trait = "Themes";

    public const string EachTheme = "Each";

    public static IReadOnlyList<Theme> Covered { get; } = Environment.GetEnvironmentVariable(VariableName) is { Length: > 0 } key
        ? [ThemeCatalog.Offered.SingleOrDefault(theme => theme.Key == key)
            ?? throw new InvalidOperationException($"{VariableName} names no offered theme: {key}.")]
        : ThemeCatalog.Offered;

    /// <summary>The offered theme of the given key: what a test expects of the theme it runs in.</summary>
    public static Theme Of(string key) => ThemeCatalog.Offered.Single(theme => theme.Key == key);
}

/// <summary>
/// A theory run in each covered theme (<see cref="TestThemes.Covered"/>), the key of the theme as its first argument,
/// followed by the given ones: <c>[Theory, InEachTheme]</c> for a test of its own, <c>[InEachTheme(320)]</c> in place of
/// <c>[InlineData(320)]</c>. It carries the trait by which the CI shares the tests out between its jobs.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[TraitDiscoverer("Bdtheque.EndToEnd.Tests.EachThemeTraitDiscoverer", "Bdtheque.EndToEnd.Tests")]
public sealed class InEachThemeAttribute(params object[] data) : DataAttribute, ITraitAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod) =>
        TestThemes.Covered.Select(theme => (object[])[theme.Key, .. data]);
}

/// <summary>
/// The trait of a theory run in each covered theme whose rows are not those of <see cref="InEachThemeAttribute"/>
/// (e.g. the captures, theme by screen by width).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[TraitDiscoverer("Bdtheque.EndToEnd.Tests.EachThemeTraitDiscoverer", "Bdtheque.EndToEnd.Tests")]
public sealed class EachThemeAttribute : Attribute, ITraitAttribute;

public sealed class EachThemeTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) =>
        [new(TestThemes.Trait, TestThemes.EachTheme)];
}
