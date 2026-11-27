namespace Bdtheque.Domain.Tests;

public sealed class ResemblanceTests
{
    [Theory]
    [InlineData("Dargaud", "DARGAUD.")]
    [InlineData("Mœbius", "Moebius")]
    [InlineData("Les Tuniques bleues", "Tuniques bleues (Les)")]
    [InlineData("L'Épervier", "Épervier [L’]")]
    [InlineData("Astérix 2", "Astérix II")]
    [InlineData("Astérix II", "Astérix deux")]
    [InlineData("Tome 21", "Tome vingt et un")]
    [InlineData("Les 80 jours", "Les quatre-vingts jours")]
    [InlineData("Tome 92", "Tome quatre-vingt-douze")]
    [InlineData("1001 nuits", "Mille et une nuits")]
    [InlineData("Tome 007", "Tome 7")]
    [InlineData("Aujourd’hui", "Aujourd'hui")]
    [InlineData("Jean Dupont", "Jean   Dupont")]
    [InlineData("Blake et Mortimer", "Blake – et – Mortimer")]
    public void Compare_TextsAlikeOnceBroughtToACommonForm_AreIdentical(string first, string second)
    {
        Assert.Equal(Resemblance.Identical, Resemblance.Compare([first], [second], inclusion: false));
    }

    [Theory]
    [InlineData("Franquin", "Franqin", 1)] // 8 characters: up to 2
    [InlineData("Uderzo", "Uderso", 1)] // 6 characters: up to 1
    [InlineData("Le Lotus bleu", "Le lotus bleue", 1)]
    [InlineData("Les aventures de Tintin", "Les aventure de Tintim", 2)] // beyond 12 characters: up to 3
    public void Compare_TextsAFewCharactersApart_AreAsFarAsTheirDistance(string first, string second, int distance)
    {
        Assert.Equal(distance, Resemblance.Compare([first], [second], inclusion: false));
    }

    [Theory]
    [InlineData("Uderzo", "Udersso")] // 6 characters: 2 is too far
    [InlineData("Franquin", "Frnqen")] // 3 apart
    [InlineData("Humour", "Aventure")]
    [InlineData("B", "C")] // nothing in common is no resemblance, however short
    public void Compare_TextsTooFarApart_DoNotResemble(string first, string second)
    {
        Assert.Null(Resemblance.Compare([first], [second], inclusion: false));
    }

    [Fact]
    public void Compare_ATextWithinTheOtherInWholeWords_IsIncludedWhereInclusionApplies()
    {
        Assert.Equal(Resemblance.Included, Resemblance.Compare(["Gotlib"], ["Marcel Gotlib"], inclusion: true));
        Assert.Equal(Resemblance.Included, Resemblance.Compare(["Éditions Dupuis"], ["Dupuis"], inclusion: true));
        Assert.Equal(Resemblance.Included, Resemblance.Compare(["Le Lotus bleu"], ["Tintin – Le Lotus bleu"], inclusion: true));
    }

    [Fact]
    public void Compare_ATextWithinTheOther_DoesNotResembleWhereInclusionDoesNotApply()
    {
        Assert.Null(Resemblance.Compare(["Humour"], ["Humour noir"], inclusion: false));
    }

    [Fact]
    public void Compare_ATextWithinAWordOfTheOther_IsNotIncluded()
    {
        Assert.Null(Resemblance.Compare(["Gotlib"], ["Gotlibien Marcel"], inclusion: true));
    }

    [Fact]
    public void Compare_SeveralTexts_GivesTheClosestOfTheirPairs()
    {
        // An author is compared by each of their identifiers: the pseudonym of one may be the name of the other.
        Assert.Equal(Resemblance.Identical, Resemblance.Compare(["Hergé", "Georges Remi", "Remi Georges"], ["Herge"], inclusion: true));
        Assert.Equal(1, Resemblance.Compare(["Jean Giraud", "Giraud Jean"], ["Moebius", "Jean Girau"], inclusion: true));
    }

    [Fact]
    public void Compare_AbsentOrBlankTexts_DoNotResemble()
    {
        Assert.Null(Resemblance.Compare([null, "  ", "..."], ["Tintin"], inclusion: true));
        Assert.Null(Resemblance.Compare([], [], inclusion: true));
    }

    [Fact]
    public void Compare_AnArticleAlone_IsKeptAsTheText()
    {
        // A title reduced to an article keeps it: without it, nothing would remain to compare.
        Assert.Equal(Resemblance.Identical, Resemblance.Compare(["Les"], ["LES"], inclusion: false));
        Assert.Null(Resemblance.Compare(["Les"], ["Tintin"], inclusion: false));
    }

    [Fact]
    public void Compare_ANumberWordAmongOtherWords_ComparesAsItsNumber()
    {
        Assert.Equal(Resemblance.Identical, Resemblance.Compare(["Il était une fois"], ["Il était 1 fois"], inclusion: false));
        // « et » between two words that are not numbers stays a word.
        Assert.Equal(Resemblance.Identical, Resemblance.Compare(["Blake et Mortimer"], ["Blake et Mortimer"], inclusion: false));
    }

    [Theory]
    [InlineData("2-205-04735-0", "978-2-205-04735-4")]
    [InlineData("9782205047354", "978 2 205 04735 4")]
    [InlineData("2-205-04735-x", "2205047350")] // check characters are not compared
    public void SameIsbn_TheTenAndThirteenDigitsOfABook_AreTheSame(string first, string second)
    {
        Assert.True(Resemblance.SameIsbn(first, second));
    }

    [Theory]
    [InlineData("2-205-04735-0", "2-205-04736-0")]
    [InlineData("979-10-205-0473-4", "2-205-04735-0")] // a 979 prefix has no ten-digit form
    [InlineData(null, "2-205-04735-0")]
    [InlineData("", "")]
    [InlineData("12", "12")] // not an ISBN
    public void SameIsbn_OtherBooksOrNoIsbn_AreNotTheSame(string? first, string? second)
    {
        Assert.False(Resemblance.SameIsbn(first, second));
    }
}
