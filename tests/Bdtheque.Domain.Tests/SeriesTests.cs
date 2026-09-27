using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class SeriesTests
{
    [Fact]
    public void Constructor_ValidTitle_SetsAutoSortKeyAndDefaults()
    {
        var series = new Series("Le Lotus bleu");

        Assert.Equal("Le Lotus bleu", series.Title);
        Assert.Equal("Lotus bleu [Le]", series.SortKey);
        Assert.False(series.IsManualSortKey);
        Assert.False(series.IsComplete);
        Assert.False(series.ExcludeFromMissingVolumes);
        Assert.Null(series.Status);
        Assert.NotEqual(Guid.Empty, series.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankTitle_Throws(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Series(title!));
    }

    [Fact]
    public void SetTitle_WhenNotManual_RecomputesSortKey()
    {
        var series = new Series("Tintin");

        series.SetTitle("Les Schtroumpfs");

        Assert.Equal("Les Schtroumpfs", series.Title);
        Assert.Equal("Schtroumpfs [Les]", series.SortKey);
    }

    [Fact]
    public void SetSortKey_SetsManualFlagAndValue()
    {
        var series = new Series("Tintin");

        series.SetSortKey("Custom Key");

        Assert.Equal("Custom Key", series.SortKey);
        Assert.True(series.IsManualSortKey);
    }

    [Fact]
    public void SetTitle_AfterManualSortKey_DoesNotOverwriteSortKey()
    {
        var series = new Series("Tintin");
        series.SetSortKey("Custom Key");

        series.SetTitle("Les Schtroumpfs");

        Assert.Equal("Custom Key", series.SortKey);
        Assert.True(series.IsManualSortKey);
    }

    [Fact]
    public void ResetSortKey_RecomputesFromTitleAndClearsManualFlag()
    {
        var series = new Series("Les Schtroumpfs");
        series.SetSortKey("Custom Key");

        series.ResetSortKey();

        Assert.Equal("Schtroumpfs [Les]", series.SortKey);
        Assert.False(series.IsManualSortKey);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetTheoreticalVolumeCount_ZeroOrNegative_Throws(int count)
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTheoreticalVolumeCount(count));
    }

    [Fact]
    public void SetTheoreticalVolumeCount_PositiveValue_Succeeds()
    {
        var series = new Series("Tintin");

        series.SetTheoreticalVolumeCount(24);

        Assert.Equal(24, series.TheoreticalVolumeCount);
    }

    [Fact]
    public void SetTheoreticalVolumeCount_Null_ClearsValue()
    {
        var series = new Series("Tintin");
        series.SetTheoreticalVolumeCount(24);

        series.SetTheoreticalVolumeCount(null);

        Assert.Null(series.TheoreticalVolumeCount);
    }

    [Fact]
    public void SetTemplate_CollectionNotBelongingToPublisher_Throws()
    {
        var series = new Series("Tintin");
        var publisher = new Publisher("Casterman");
        var otherPublisher = new Publisher("Dupuis");
        var collection = new PublisherCollection("Tintin", otherPublisher);

        Assert.Throws<ArgumentException>(() => series.SetTemplate(publisher, collection));
    }

    [Fact]
    public void SetTemplate_CollectionWithoutPublisher_Throws()
    {
        var series = new Series("Tintin");
        var publisher = new Publisher("Casterman");
        var collection = new PublisherCollection("Tintin", publisher);

        Assert.Throws<ArgumentException>(() => series.SetTemplate(null, collection));
    }

    [Fact]
    public void SetTemplate_MatchingPublisherAndCollection_Succeeds()
    {
        var series = new Series("Tintin");
        var publisher = new Publisher("Casterman");
        var collection = new PublisherCollection("Tintin", publisher);

        series.SetTemplate(publisher, collection);

        Assert.Same(publisher, series.TemplatePublisher);
        Assert.Same(collection, series.TemplatePublisherCollection);
        Assert.Equal(publisher.Id, series.TemplatePublisherId);
        Assert.Equal(collection.Id, series.TemplatePublisherCollectionId);
    }

    [Fact]
    public void SetTemplate_PublisherWithoutCollection_Succeeds()
    {
        var series = new Series("Tintin");
        var publisher = new Publisher("Casterman");

        series.SetTemplate(publisher, null);

        Assert.Same(publisher, series.TemplatePublisher);
        Assert.Null(series.TemplatePublisherCollection);
    }

    [Fact]
    public void SetTemplate_ClearBoth_Succeeds()
    {
        var series = new Series("Tintin");
        var publisher = new Publisher("Casterman");
        var collection = new PublisherCollection("Tintin", publisher);
        series.SetTemplate(publisher, collection);

        series.SetTemplate(null, null);

        Assert.Null(series.TemplatePublisher);
        Assert.Null(series.TemplatePublisherId);
        Assert.Null(series.TemplatePublisherCollection);
        Assert.Null(series.TemplatePublisherCollectionId);
    }

    [Fact]
    public void SetStatus_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetStatus((SeriesStatus)42));
    }

    [Fact]
    public void SetTemplateBinding_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTemplateBinding((BindingType)42));
    }

    [Fact]
    public void SetTemplateOrientation_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTemplateOrientation((BookOrientation)42));
    }

    [Fact]
    public void SetTemplateReadingDirection_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTemplateReadingDirection((ReadingDirection)42));
    }

    [Fact]
    public void SetTemplateFormat_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTemplateFormat((EditionFormat)42));
    }

    [Fact]
    public void SetTemplateEditionCategory_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTemplateEditionCategory((EditionCategory)42));
    }

    [Fact]
    public void SetTemplateCondition_UndefinedValue_Throws()
    {
        var series = new Series("Tintin");

        Assert.Throws<ArgumentOutOfRangeException>(() => series.SetTemplateCondition((EditionCondition)42));
    }

    [Fact]
    public void Genres_AddAndRemove_UpdatesCollection()
    {
        var series = new Series("Tintin");
        var genre = new Genre("Aventure");

        series.Genres.Add(genre);
        Assert.Contains(genre, series.Genres);

        series.Genres.Remove(genre);
        Assert.DoesNotContain(genre, series.Genres);
    }

    [Fact]
    public void Universes_AddAndRemove_UpdatesCollection()
    {
        var series = new Series("Tintin");
        var universe = new Universe("Franco-belge");

        series.Universes.Add(universe);
        Assert.Contains(universe, series.Universes);

        series.Universes.Remove(universe);
        Assert.DoesNotContain(universe, series.Universes);
    }
}
