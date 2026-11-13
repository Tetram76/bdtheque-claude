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
        DomainAssert.Violates<Album>(DomainRules.AlbumTitleRequiredWithoutSeries, nameof(Album.Title), () => new Album(title, null));
    }

    [Fact]
    public void Title_WhenNotManual_RecomputesSortKey()
    {
        var album = new Album("Tintin", null);

        SetTitle(album, "Les Schtroumpfs");

        Assert.Equal("Les Schtroumpfs", album.Title);
        Assert.Equal("Schtroumpfs [Les]", album.SortKey);
    }

    [Fact]
    public void Title_ClearedWithSeriesAttached_ClearsSortKeyAndManualFlag()
    {
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", series);
        album.SetSortKey("Custom Key");

        SetTitle(album, null);

        Assert.Null(album.Title);
        Assert.Null(album.SortKey);
        Assert.False(album.IsManualSortKey);
    }

    [Fact]
    public void Title_ClearedWithoutSeries_Throws()
    {
        var album = new Album("Le Lotus bleu", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumTitleRequiredWithoutSeries, nameof(Album.Title), () => SetTitle(album, null));
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

        DomainAssert.Violates<Album>(DomainRules.AlbumManualSortKeyRequiresTitle, nameof(Album.SortKey), () => album.SetSortKey("Custom Key"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSortKey_Blank_Throws(string sortKey)
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumSortKeyRequired, nameof(Album.SortKey), () => album.SetSortKey(sortKey));
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
    public void Series_ToNullWithTitle_Succeeds()
    {
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", series);

        SetSeries(album, null);

        Assert.Null(album.Series);
        Assert.Null(album.SeriesId);
    }

    [Fact]
    public void Series_ToNullWithoutTitle_Throws()
    {
        var series = new Series("Tintin");
        var album = new Album(null, series);

        DomainAssert.Violates<Album>(DomainRules.AlbumTitleRequiredWithoutSeries, nameof(Album.Title), () => SetSeries(album, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetVolumeNumber_ZeroOrNegative_Throws(int number)
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumVolumeNumberPositive, nameof(Album.VolumeNumber), () => album.SetVolumeNumber(number));
    }

    [Fact]
    public void SetVolumeNumber_PositiveValue_Succeeds()
    {
        var album = new Album("Tintin", null);

        album.SetVolumeNumber(5);

        Assert.Equal(5, album.VolumeNumber);
    }

    [Fact]
    public void SetTypeAndVolumeRange_OnlyStart_Throws()
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(
            DomainRules.AlbumVolumeRangeBothOrNeither, nameof(Album.EndVolumeNumber), () => album.SetTypeAndVolumeRange(AlbumType.Omnibus, 1, null));
    }

    [Fact]
    public void SetTypeAndVolumeRange_StartGreaterThanEnd_Throws()
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumVolumeRangeOrder, nameof(Album.EndVolumeNumber), () => album.SetTypeAndVolumeRange(AlbumType.Omnibus, 5, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetTypeAndVolumeRange_NonPositiveStart_Throws(int start)
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumVolumeRangeStartPositive, nameof(Album.StartVolumeNumber), () => album.SetTypeAndVolumeRange(AlbumType.Omnibus, start, 6));
    }

    [Fact]
    public void SetTypeAndVolumeRange_RangeOnARegularAlbum_Throws()
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumVolumeRangeOmnibusOnly, nameof(Album.StartVolumeNumber), () => album.SetTypeAndVolumeRange(AlbumType.Regular, 1, 6));
    }

    [Fact]
    public void SetTypeAndVolumeRange_OmnibusWithARange_Succeeds()
    {
        var album = new Album("Tintin", null);

        album.SetTypeAndVolumeRange(AlbumType.Omnibus, 1, 6);

        Assert.Equal((AlbumType.Omnibus, 1, 6), (album.Type, album.StartVolumeNumber, album.EndVolumeNumber));
    }

    [Fact]
    public void SetTypeAndVolumeRange_BackToRegularClearingTheRange_SucceedsInASingleCall()
    {
        // The form sends the type and the range together: whichever order separate setters would
        // apply them in, one of them would see an omnibus range on a regular album.
        var album = new Album("Tintin", null);
        album.SetTypeAndVolumeRange(AlbumType.Omnibus, 1, 6);

        album.SetTypeAndVolumeRange(AlbumType.Regular, null, null);

        Assert.Equal((AlbumType.Regular, (int?)null, (int?)null), (album.Type, album.StartVolumeNumber, album.EndVolumeNumber));
    }

    [Fact]
    public void SetTypeAndVolumeRange_Rejected_LeavesTheAlbumUntouched()
    {
        var album = new Album("Tintin", null);
        album.SetTypeAndVolumeRange(AlbumType.Omnibus, 1, 6);

        DomainAssert.Violates<Album>(DomainRules.AlbumVolumeRangeOmnibusOnly, nameof(Album.StartVolumeNumber), () => album.SetTypeAndVolumeRange(AlbumType.Regular, 2, 3));

        Assert.Equal((AlbumType.Omnibus, 1, 6), (album.Type, album.StartVolumeNumber, album.EndVolumeNumber));
    }

    [Fact]
    public void SetTypeAndVolumeRange_UndefinedType_Throws()
    {
        var album = new Album("Tintin", null);

        Assert.Throws<ArgumentOutOfRangeException>(() => album.SetTypeAndVolumeRange((AlbumType)42, null, null));
    }

    [Fact]
    public void SetSpecialIssue_IsIndependentOfType()
    {
        var album = new Album("Tintin", null);
        album.SetTypeAndVolumeRange(AlbumType.Omnibus, null, null);

        album.SetSpecialIssue(true);

        Assert.True(album.IsSpecialIssue);
        Assert.Equal(AlbumType.Omnibus, album.Type);
    }

    [Fact]
    public void SetFirstPublicationDate_MonthWithoutYear_Throws()
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumPublicationMonthRequiresYear, nameof(Album.FirstPublicationYear), () => album.SetFirstPublicationDate(null, 6));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void SetFirstPublicationDate_MonthOutOfRange_Throws(int month)
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumPublicationMonthRange, nameof(Album.FirstPublicationMonth), () => album.SetFirstPublicationDate(1978, month));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetFirstPublicationDate_NonPositiveYear_Throws(int year)
    {
        var album = new Album("Tintin", null);

        DomainAssert.Violates<Album>(DomainRules.AlbumPublicationYearPositive, nameof(Album.FirstPublicationYear), () => album.SetFirstPublicationDate(year, null));
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
    public void SetRating_UndefinedValue_Throws()
    {
        var album = new Album("Tintin", null);

        Assert.Throws<ArgumentOutOfRangeException>(() => album.SetRating((AlbumRating)99));
    }

    [Fact]
    public void AddGenre_SameGenreTwice_KeepsASingleEntry()
    {
        // A duplicate would violate the join table's primary key at the next save.
        var owner = new Album("Tintin", null);
        var genre = new Genre("Aventure");

        owner.AddGenre(genre);
        owner.AddGenre(genre);

        Assert.Single(owner.Genres);
    }

    [Fact]
    public void AddUniverse_SameUniverseTwice_KeepsASingleEntry()
    {
        var owner = new Album("Tintin", null);
        var universe = new Universe("Franco-belge");

        owner.AddUniverse(universe);
        owner.AddUniverse(universe);

        Assert.Single(owner.Universes);
    }

    [Fact]
    public void Genres_AddAndRemove_UpdatesCollection()
    {
        var album = new Album("Tintin", null);
        var genre = new Genre("Aventure");

        album.AddGenre(genre);
        Assert.Contains(genre, album.Genres);

        album.RemoveGenre(genre);
        Assert.DoesNotContain(genre, album.Genres);
    }

    [Fact]
    public void Universes_AddAndRemove_UpdatesCollection()
    {
        var album = new Album("Tintin", null);
        var universe = new Universe("Franco-belge");

        album.AddUniverse(universe);
        Assert.Contains(universe, album.Universes);

        album.RemoveUniverse(universe);
        Assert.DoesNotContain(universe, album.Universes);
    }

    [Fact]
    public void Constructor_WithTitle_ComputesNavigationEntryFromSortKey()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Equal("L", album.NavigationEntry);
    }

    [Fact]
    public void Constructor_WithoutTitle_LeavesNavigationEntryNull()
    {
        // The entry follows the album's own sort key: the series' entry is substituted at read
        // time, never copied onto the album
        // (choix-implementation/api/navigation-par-initiale-mise-en-oeuvre.md).
        var album = new Album(null, new Series("Tintin"));

        Assert.Null(album.NavigationEntry);
    }

    [Fact]
    public void Title_WhenNotManual_RecomputesNavigationEntry()
    {
        var album = new Album("Tintin", null);

        SetTitle(album, "2001 Nights");

        Assert.Equal("#", album.NavigationEntry);
    }

    [Fact]
    public void Title_Cleared_ClearsNavigationEntry()
    {
        var album = new Album("Le Lotus bleu", new Series("Tintin"));

        SetTitle(album, null);

        Assert.Null(album.NavigationEntry);
    }

    [Fact]
    public void SetSortKey_RecomputesNavigationEntry()
    {
        var album = new Album("Tintin", null);

        album.SetSortKey("Ωmega");

        Assert.Equal("@", album.NavigationEntry);
    }

    [Fact]
    public void Title_AfterManualSortKey_KeepsNavigationEntryOfManualSortKey()
    {
        var album = new Album("Tintin", null);
        album.SetSortKey("Astérix");

        SetTitle(album, "Les Schtroumpfs");

        Assert.Equal("A", album.NavigationEntry);
    }

    [Fact]
    public void ResetSortKey_RecomputesNavigationEntry()
    {
        var album = new Album("Les Schtroumpfs", null);
        album.SetSortKey("Astérix");

        album.ResetSortKey();

        Assert.Equal("S", album.NavigationEntry);
    }

    [Fact]
    public void Constructor_InASeriesWithTemplateContributions_CopiesNothing()
    {
        // The form takes the authors of the series as soon as the series is chosen
        // (fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md): the album
        // is credited with those it is given, and nothing more.
        var (series, _, _) = SeriesWithTemplate();

        var album = new Album(null, series);

        Assert.Empty(album.Contributions);
    }

    [Fact]
    public void Series_AttachingAnAlbumWithoutContributions_CopiesNothing()
    {
        var (series, _, _) = SeriesWithTemplate();
        var album = new Album("Le Lotus bleu", null);

        SetSeries(album, series);

        Assert.Empty(album.Contributions);
    }

    [Fact]
    public void SetTitleSeriesAndContributions_ClearingTheTitleWhileAttachingASeries_SucceedsInASingleCall()
    {
        // Separate setters would fail in one order or the other: the title is required until the
        // series is attached, and the series cannot be detached while the title is empty.
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", null);

        album.SetTitleSeriesAndContributions(null, series, []);

        Assert.Equal((null, series.Id), (album.Title, album.SeriesId));
    }

    [Fact]
    public void SetTitleSeriesAndContributions_SettingTheTitleWhileDetachingTheSeries_SucceedsInASingleCall()
    {
        var album = new Album(null, new Series("Tintin"));

        album.SetTitleSeriesAndContributions("Le Lotus bleu", null, []);

        Assert.Equal(("Le Lotus bleu", (Guid?)null, "Lotus bleu [Le]"), (album.Title, album.SeriesId, album.SortKey));
    }

    [Fact]
    public void SetTitleSeriesAndContributions_WithoutTitleNorSeries_ThrowsAndLeavesTheAlbumUntouched()
    {
        var series = new Series("Tintin");
        var album = new Album("Le Lotus bleu", series);
        var author = new Author(null, null, "Hergé");

        DomainAssert.Violates<Album>(
            DomainRules.AlbumTitleRequiredWithoutSeries, nameof(Album.Title),
            () => album.SetTitleSeriesAndContributions(" ", null, [(author, ContributionRole.Scenarist)]));

        Assert.Equal(("Le Lotus bleu", (Guid?)series.Id), (album.Title, album.SeriesId));
        Assert.Empty(album.Contributions);
    }

    [Fact]
    public void SetTitleSeriesAndContributions_SameCreditTwice_ThrowsAndLeavesTheAlbumUntouched()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        DomainAssert.Violates(
            DomainRules.ContributionAlreadyCredited,
            () => album.SetTitleSeriesAndContributions("Tintin", null, [(author, ContributionRole.Scenarist), (author, ContributionRole.Scenarist)]));

        Assert.Equal("Le Lotus bleu", album.Title);
        Assert.Empty(album.Contributions);
    }

    [Fact]
    public void SetTitleSeriesAndContributions_SameAuthorWithAnotherRole_IsAccepted()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        album.SetTitleSeriesAndContributions("Le Lotus bleu", null, [(author, ContributionRole.Scenarist), (author, ContributionRole.Illustrator)]);

        Assert.Equal(2, album.Contributions.Count);
    }

    [Fact]
    public void SetTitleSeriesAndContributions_ReplacesTheContributions_KeepingTheUnchangedOnes()
    {
        var kept = new Author(null, null, "Hergé");
        var dropped = new Author(null, null, "Jacobs");
        var added = new Author(null, null, "Studio");
        var album = new Album("Le Lotus bleu", null, [(kept, ContributionRole.Scenarist), (dropped, ContributionRole.Illustrator)]);
        var keptContribution = album.Contributions.Single(c => c.AuthorId == kept.Id);

        album.SetTitleSeriesAndContributions("Le Lotus bleu", null, [(kept, ContributionRole.Scenarist), (added, ContributionRole.Colorist)]);

        Assert.Equal([(kept.Id, ContributionRole.Scenarist), (added.Id, ContributionRole.Colorist)], Credits(album));
        Assert.Contains(keptContribution, album.Contributions);
    }

    [Fact]
    public void SetTitleSeriesAndContributions_AttachingWithNoContribution_LeavesNone()
    {
        // Authors removed from the form after they were taken from the series do not come back.
        var (series, _, _) = SeriesWithTemplate();
        var album = new Album("Le Lotus bleu", null, [(new Author(null, null, "Studio"), ContributionRole.Colorist)]);

        album.SetTitleSeriesAndContributions("Le Lotus bleu", series, []);

        Assert.Empty(album.Contributions);
    }

    [Fact]
    public void SetTitleSeriesAndContributions_KeepsAManualSortKeyWhenTheTitleChanges()
    {
        var album = new Album("Tintin", null);
        album.SetSortKey("Custom Key");

        album.SetTitleSeriesAndContributions("Les Schtroumpfs", null, []);

        Assert.Equal(("Custom Key", true), (album.SortKey, album.IsManualSortKey));
    }

    private static (Series Series, Author Scenarist, Author Illustrator) SeriesWithTemplate()
    {
        var series = new Series("Tintin");
        var scenarist = new Author(null, null, "Hergé");
        var illustrator = new Author(null, null, "Jacobs");
        series.SetTemplateContributions([(scenarist, ContributionRole.Scenarist), (illustrator, ContributionRole.Illustrator)]);
        return (series, scenarist, illustrator);
    }

    // Through the single operation the application uses, the album keeping its other two values.
    private static void SetTitle(Album album, string? title) =>
        album.SetTitleSeriesAndContributions(title, album.Series, CreditsOf(album));

    private static void SetSeries(Album album, Series? series) =>
        album.SetTitleSeriesAndContributions(album.Title, series, CreditsOf(album));

    private static List<(Author Author, ContributionRole Role)> CreditsOf(Album album) =>
        album.Contributions.Select(c => (c.Author, c.Role)).ToList();

    private static List<(Guid AuthorId, ContributionRole Role)> Credits(Album album) =>
        album.Contributions.Select(c => (c.AuthorId, c.Role)).OrderBy(c => c.Role).ToList();
}
