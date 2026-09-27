using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class AlbumTests
{
    [Fact]
    public void Constructor_TitleOnly_SetsAutoSortKeyAndDefaults()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Equal("Le Lotus bleu", album.Title);
        Assert.Equal("Lotus bleu [Le]", album.SortKey);
        Assert.False(album.IsManualSortKey);
        Assert.Null(album.Series);
        Assert.Null(album.SeriesId);
        Assert.Equal(AlbumType.Regular, album.Type);
        Assert.False(album.IsSpecialIssue);
        Assert.NotEqual(Guid.Empty, album.Id);
    }

    [Fact]
    public void Constructor_SeriesOnlyNoTitle_LeavesTitleAndSortKeyNull()
    {
        var series = new Series("Tintin");

        var album = new Album(null, series);

        Assert.Null(album.Title);
        Assert.Null(album.SortKey);
        Assert.Same(series, album.Series);
        Assert.Equal(series.Id, album.SeriesId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankTitleAndNoSeries_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => new Album(title, null));
    }

    [Fact]
    public void SetTitle_WhenNotManual_RecomputesSortKey()
    {
        var album = new Album("Tintin", null);

        album.SetTitle("Les Schtroumpfs");

        Assert.Equal("Les Schtroumpfs", album.Title);
        Assert.Equal("Schtroumpfs [Les]", album.SortKey);
    }

    [Fact]
    public void SetTitle_ClearedWithSeriesAttached_ClearsSortKeyAndManualFlag()
    {
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", series);
        album.SetSortKey("Custom Key");

        album.SetTitle(null);

        Assert.Null(album.Title);
        Assert.Null(album.SortKey);
        Assert.False(album.IsManualSortKey);
    }

    [Fact]
    public void SetTitle_ClearedWithoutSeries_Throws()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Throws<ArgumentException>(() => album.SetTitle(null));
    }

    [Fact]
    public void SetSortKey_SetsManualFlagAndValue()
    {
        var album = new Album("Tintin", null);

        album.SetSortKey("Custom Key");

        Assert.Equal("Custom Key", album.SortKey);
        Assert.True(album.IsManualSortKey);
    }

    [Fact]
    public void SetSortKey_WithoutTitle_Throws()
    {
        var series = new Series("Tintin");
        var album = new Album(null, series);

        Assert.Throws<InvalidOperationException>(() => album.SetSortKey("Custom Key"));
    }

    [Fact]
    public void ResetSortKey_RecomputesFromTitleAndClearsManualFlag()
    {
        var album = new Album("Les Schtroumpfs", null);
        album.SetSortKey("Custom Key");

        album.ResetSortKey();

        Assert.Equal("Schtroumpfs [Les]", album.SortKey);
        Assert.False(album.IsManualSortKey);
    }

    [Fact]
    public void SetSeries_ToNullWithTitle_Succeeds()
    {
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", series);

        album.SetSeries(null);

        Assert.Null(album.Series);
        Assert.Null(album.SeriesId);
    }

    [Fact]
    public void SetSeries_ToNullWithoutTitle_Throws()
    {
        var series = new Series("Tintin");
        var album = new Album(null, series);

        Assert.Throws<ArgumentException>(() => album.SetSeries(null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetVolumeNumber_ZeroOrNegative_Throws(int number)
    {
        var album = new Album("Tintin", null);

        Assert.Throws<ArgumentOutOfRangeException>(() => album.SetVolumeNumber(number));
    }

    [Fact]
    public void SetVolumeNumber_PositiveValue_Succeeds()
    {
        var album = new Album("Tintin", null);

        album.SetVolumeNumber(5);

        Assert.Equal(5, album.VolumeNumber);
    }

    [Fact]
    public void SetVolumeRange_OnlyStart_Throws()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);

        Assert.Throws<ArgumentException>(() => album.SetVolumeRange(1, null));
    }

    [Fact]
    public void SetVolumeRange_StartGreaterThanEnd_Throws()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);

        Assert.Throws<ArgumentException>(() => album.SetVolumeRange(5, 1));
    }

    [Fact]
    public void SetVolumeRange_OnRegularAlbum_Throws()
    {
        var album = new Album("Tintin", null);

        Assert.Throws<InvalidOperationException>(() => album.SetVolumeRange(1, 6));
    }

    [Fact]
    public void SetVolumeRange_OnOmnibusAlbum_Succeeds()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);

        album.SetVolumeRange(1, 6);

        Assert.Equal(1, album.StartVolumeNumber);
        Assert.Equal(6, album.EndVolumeNumber);
    }

    [Fact]
    public void SetVolumeRange_ClearBoth_Succeeds()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);
        album.SetVolumeRange(1, 6);

        album.SetVolumeRange(null, null);

        Assert.Null(album.StartVolumeNumber);
        Assert.Null(album.EndVolumeNumber);
    }

    [Fact]
    public void SetType_AwayFromOmnibusWithRangeSet_Throws()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);
        album.SetVolumeRange(1, 6);

        Assert.Throws<InvalidOperationException>(() => album.SetType(AlbumType.Regular));
    }

    [Fact]
    public void SetType_AwayFromOmnibusAfterClearingRange_Succeeds()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);
        album.SetVolumeRange(1, 6);
        album.SetVolumeRange(null, null);

        album.SetType(AlbumType.Regular);

        Assert.Equal(AlbumType.Regular, album.Type);
    }

    [Fact]
    public void SetSpecialIssue_IsIndependentOfType()
    {
        var album = new Album("Tintin", null);
        album.SetType(AlbumType.Omnibus);

        album.SetSpecialIssue(true);

        Assert.True(album.IsSpecialIssue);
        Assert.Equal(AlbumType.Omnibus, album.Type);
    }

    [Fact]
    public void SetFirstPublicationDate_MonthWithoutYear_Throws()
    {
        var album = new Album("Tintin", null);

        Assert.Throws<ArgumentException>(() => album.SetFirstPublicationDate(null, 6));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void SetFirstPublicationDate_MonthOutOfRange_Throws(int month)
    {
        var album = new Album("Tintin", null);

        Assert.Throws<ArgumentOutOfRangeException>(() => album.SetFirstPublicationDate(1978, month));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetFirstPublicationDate_NonPositiveYear_Throws(int year)
    {
        var album = new Album("Tintin", null);

        Assert.Throws<ArgumentOutOfRangeException>(() => album.SetFirstPublicationDate(year, null));
    }

    [Fact]
    public void SetFirstPublicationDate_YearOnly_Succeeds()
    {
        var album = new Album("Tintin", null);

        album.SetFirstPublicationDate(1978, null);

        Assert.Equal(1978, album.FirstPublicationYear);
        Assert.Null(album.FirstPublicationMonth);
    }

    [Fact]
    public void SetFirstPublicationDate_YearAndMonth_Succeeds()
    {
        var album = new Album("Tintin", null);

        album.SetFirstPublicationDate(1978, 6);

        Assert.Equal(1978, album.FirstPublicationYear);
        Assert.Equal(6, album.FirstPublicationMonth);
    }

    [Fact]
    public void SetFirstPublicationDate_ClearBoth_Succeeds()
    {
        var album = new Album("Tintin", null);
        album.SetFirstPublicationDate(1978, 6);

        album.SetFirstPublicationDate(null, null);

        Assert.Null(album.FirstPublicationYear);
        Assert.Null(album.FirstPublicationMonth);
    }

    [Fact]
    public void SetRating_SetsAndClearsValue()
    {
        var album = new Album("Tintin", null);

        album.SetRating(AlbumRating.VeryGood);
        Assert.Equal(AlbumRating.VeryGood, album.Rating);

        album.SetRating(null);
        Assert.Null(album.Rating);
    }

    [Fact]
    public void Genres_AddAndRemove_UpdatesCollection()
    {
        var album = new Album("Tintin", null);
        var genre = new Genre("Aventure");

        album.Genres.Add(genre);
        Assert.Contains(genre, album.Genres);

        album.Genres.Remove(genre);
        Assert.DoesNotContain(genre, album.Genres);
    }

    [Fact]
    public void Universes_AddAndRemove_UpdatesCollection()
    {
        var album = new Album("Tintin", null);
        var universe = new Universe("Franco-belge");

        album.Universes.Add(universe);
        Assert.Contains(universe, album.Universes);

        album.Universes.Remove(universe);
        Assert.DoesNotContain(universe, album.Universes);
    }
}
