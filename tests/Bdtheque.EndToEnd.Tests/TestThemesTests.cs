extern alias frontend;

using System.Reflection;
using System.Text.RegularExpressions;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The share of the end-to-end tests between the jobs of the CI, one per offered theme (<see cref="TestThemes"/>): a test
/// of each theme left without its trait would run in the job of the first theme only, for that theme alone, and a theme
/// missing from the jobs would never be tested, both without any failure to tell.
/// </summary>
public sealed partial class TestThemesTests
{
    [Fact]
    public void EveryTestRunInEachTheme_CarriesTheTraitThatSharesItOutBetweenTheJobs()
    {
        var untraited = typeof(TestThemesTests).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(TheoryAttribute))
                && method.GetParameters() is [{ Name: "theme" }, ..]
                && !method.IsDefined(typeof(InEachThemeAttribute))
                && !method.IsDefined(typeof(EachThemeAttribute)))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}");

        Assert.Empty(untraited);
    }

    [Fact]
    public void JobsOfTheEndToEndTests_AreThoseOfTheOfferedThemes_TheFirstOneRunningTheOtherTests()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml"));
        var offered = ThemeCatalog.Offered.Select(theme => theme.Key).ToList();

        Assert.Equal(offered, MatrixOfThemes().Match(workflow).Groups["themes"].Value.Split(',', StringSplitOptions.TrimEntries));
        Assert.Contains($"matrix.theme != '{offered[0]}' && '--filter \"{TestThemes.Trait}={TestThemes.EachTheme}\"'", workflow, StringComparison.Ordinal);
        Assert.Contains($"{TestThemes.VariableName}: ${{{{ matrix.theme }}}}", workflow, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s+theme: \[(?<themes>[^\]]*)\]\s*$", RegexOptions.Multiline)]
    private static partial Regex MatrixOfThemes();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bdtheque.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root (Bdtheque.slnx) not found.");
    }
}
