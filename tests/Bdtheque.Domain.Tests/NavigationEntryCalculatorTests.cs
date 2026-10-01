using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

public sealed class NavigationEntryCalculatorTests
{
    // Examples of fonctionnel.md § Titres (séries et albums) and § Artistes, applied to their sort key.
    [Theory]
    [InlineData("Lotus bleu [Le]", "L")]
    [InlineData("Schtroumpfs [Les]", "S")]
    [InlineData("Épervier [L']", "E")]
    [InlineData("Tintin", "T")]
    [InlineData("13", "#")]
    [InlineData("...Et après", "@")]
    [InlineData("Van Hamme Jean", "V")]
    [InlineData("Moebius", "M")]
    [InlineData("Hergé", "H")]
    public void Compute_SpecificationExamples(string sortKey, string expectedEntry)
    {
        Assert.Equal(expectedEntry, NavigationEntryCalculator.Compute(sortKey));
    }

    // Letters ranked under their base letter by the French alphabetical order itself
    // (choix-implementation.md § Navigation par initiale : mise en œuvre).
    [Theory]
    [InlineData("é", "E")]
    [InlineData("È", "E")]
    [InlineData("Ê", "E")]
    [InlineData("à", "A")]
    [InlineData("Ç", "C")]
    [InlineData("Œ", "O")]
    [InlineData("œ", "O")]
    [InlineData("Æ", "A")]
    [InlineData("Ø", "O")]
    [InlineData("Ł", "L")]
    [InlineData("Đ", "D")]
    [InlineData("Ð", "D")]
    [InlineData("Ŧ", "T")]
    [InlineData("Ħ", "H")]
    [InlineData("ß", "S")]
    [InlineData("ı", "I")]
    [InlineData("Ŋ", "N")]
    [InlineData("a", "A")]
    [InlineData("z", "Z")]
    [InlineData("Ž", "Z")]
    public void Compute_LetterWithBaseLetter_ReturnsUppercaseBaseLetter(string initial, string expectedEntry)
    {
        Assert.Equal(expectedEntry, NavigationEntryCalculator.Compute(initial + "suite"));
    }

    [Fact]
    public void Compute_DecomposedAccentedInitial_ReturnsBaseLetter()
    {
        // "É" typed or migrated as "E" + combining acute accent: the whole first text element is
        // the initial, not its bare first code unit.
        Assert.Equal("E", NavigationEntryCalculator.Compute("Épervier"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("2001 Nights")]
    [InlineData("9")]
    public void Compute_DigitInitial_ReturnsHash(string sortKey)
    {
        Assert.Equal("#", NavigationEntryCalculator.Compute(sortKey));
    }

    [Theory]
    [InlineData("Ωmega")]
    [InlineData("Шрек")]
    [InlineData("東京")]
    [InlineData("Þorgal")]
    [InlineData("!Hola")]
    [InlineData("\"Guillemets\"")]
    [InlineData("«Titre»")]
    [InlineData("(Parenthèse)")]
    [InlineData("$")]
    [InlineData("😀 Emoji")]
    public void Compute_OtherInitial_ReturnsAt(string sortKey)
    {
        Assert.Equal("@", NavigationEntryCalculator.Compute(sortKey));
    }

    [Fact]
    public void Compute_EveryBaseLetter_ReturnsItself()
    {
        for (var letter = 'A'; letter <= 'Z'; letter++)
            Assert.Equal(letter.ToString(), NavigationEntryCalculator.Compute(letter + "x"));
    }

    [Fact]
    public void Entries_ListsTheTwentyEightNavigationEntriesInDisplayOrder()
    {
        Assert.Equal(
            ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "#", "@"],
            NavigationEntryCalculator.Entries);
    }

    [Theory]
    [InlineData("Épervier")]
    [InlineData("2001")]
    [InlineData("Ωmega")]
    [InlineData("...Et après")]
    public void Compute_AlwaysReturnsOneOfTheEntries(string sortKey)
    {
        Assert.Contains(NavigationEntryCalculator.Compute(sortKey), NavigationEntryCalculator.Entries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Compute_BlankSortKey_Throws(string sortKey)
    {
        // A stored sort key is never blank (domain and CHECK constraints): a blank one is a
        // programming error, not a user input to report.
        Assert.Throws<ArgumentException>(() => NavigationEntryCalculator.Compute(sortKey));
    }
}
