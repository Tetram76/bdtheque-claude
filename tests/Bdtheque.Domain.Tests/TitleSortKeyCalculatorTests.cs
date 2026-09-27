using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

public sealed class TitleSortKeyCalculatorTests
{
    [Theory]
    [InlineData("Le Lotus bleu", "Lotus bleu [Le]")]
    [InlineData("Un Lotus bleu", "Lotus bleu [Un]")]
    [InlineData("Les Schtroumpfs", "Schtroumpfs [Les]")]
    [InlineData("L'Épervier", "Épervier [L']")]
    [InlineData("L’Épervier", "Épervier [L’]")]
    [InlineData("Tintin", "Tintin")]
    [InlineData("Une aventure de Spirou", "aventure de Spirou [Une]")]
    [InlineData("Des nouvelles de nulle part", "nouvelles de nulle part [Des]")]
    [InlineData("La Marque jaune", "Marque jaune [La]")]
    public void Compute_KnownArticle_MovesArticleToSuffix(string title, string expectedSortKey)
    {
        Assert.Equal(expectedSortKey, TitleSortKeyCalculator.Compute(title));
    }

    [Fact]
    public void Compute_TitlesDifferingOnlyByArticle_SortDeterministically()
    {
        // The whole point of the suffix form: two titles sharing the same significant word but a
        // different leading article must never tie or sort arbitrarily relative to each other.
        var leKey = TitleSortKeyCalculator.Compute("Le Lotus bleu");
        var unKey = TitleSortKeyCalculator.Compute("Un Lotus bleu");

        Assert.NotEqual(leKey, unKey);
        Assert.True(string.CompareOrdinal(leKey, unKey) < 0);
    }

    [Theory]
    [InlineData("Lettre à un otage", "Lettre à un otage")]
    [InlineData("Larousse", "Larousse")]
    [InlineData("Description", "Description")]
    public void Compute_NoLeadingArticle_ReturnsTitleUnchanged(string title, string expectedSortKey)
    {
        Assert.Equal(expectedSortKey, TitleSortKeyCalculator.Compute(title));
    }

    [Fact]
    public void Compute_ArticleWordWithoutTrailingSpace_IsNotMoved()
    {
        // "Les" alone is the whole title: no word boundary after the article, so it must not move.
        Assert.Equal("Les", TitleSortKeyCalculator.Compute("Les"));
    }

    [Fact]
    public void Compute_TrimsSurroundingWhitespace()
    {
        Assert.Equal("Lotus bleu [Le]", TitleSortKeyCalculator.Compute("  Le Lotus bleu  "));
    }

    [Theory]
    [InlineData("Le  Lotus bleu", "Lotus bleu [Le]")]
    [InlineData("Le   Lotus bleu", "Lotus bleu [Le]")]
    [InlineData("Les  Schtroumpfs", "Schtroumpfs [Les]")]
    public void Compute_MultipleSeparatorSpacesAfterArticle_ConsumesAllOfThem(string title, string expectedSortKey)
    {
        // A leftover separator space would make the stored key start with whitespace instead of
        // the significant word, breaking the "first character = initial" invariant.
        Assert.Equal(expectedSortKey, TitleSortKeyCalculator.Compute(title));
    }

    [Fact]
    public void Compute_MultipleSpacesAfterElidedArticle_ConsumesAllOfThem()
    {
        Assert.Equal("Épervier [L']", TitleSortKeyCalculator.Compute("L'  Épervier"));
    }
}
