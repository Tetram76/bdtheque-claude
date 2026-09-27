using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

public sealed class TitleSortKeyCalculatorTests
{
    [Theory]
    [InlineData("Le Lotus bleu", "Lotus bleu")]
    [InlineData("Les Schtroumpfs", "Schtroumpfs")]
    [InlineData("L'Épervier", "Épervier")]
    [InlineData("L’Épervier", "Épervier")]
    [InlineData("Tintin", "Tintin")]
    [InlineData("Un homme est mort", "homme est mort")]
    [InlineData("Une aventure de Spirou", "aventure de Spirou")]
    [InlineData("Des nouvelles de nulle part", "nouvelles de nulle part")]
    [InlineData("La Marque jaune", "Marque jaune")]
    public void Compute_KnownArticle_StripsArticle(string title, string expectedSortKey)
    {
        Assert.Equal(expectedSortKey, TitleSortKeyCalculator.Compute(title));
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
    public void Compute_ArticleWordWithoutTrailingSpace_IsNotStripped()
    {
        // "Les" alone is the whole title: no word boundary after the article, so it must not be stripped.
        Assert.Equal("Les", TitleSortKeyCalculator.Compute("Les"));
    }

    [Fact]
    public void Compute_TrimsSurroundingWhitespace()
    {
        Assert.Equal("Lotus bleu", TitleSortKeyCalculator.Compute("  Le Lotus bleu  "));
    }
}
