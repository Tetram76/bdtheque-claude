using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The draw of the edition put in the spotlight of the day (fonctionnel/page-accueil.md § Page d'accueil, À la une): the
/// repetitions it avoids, its preference for a cover, then its fairness. The roll stands for the random number, so
/// that each rule is checked on the edition it leads to.
/// </summary>
public sealed class SpotlightDrawTests
{
    private static readonly DateOnly Day = new(2026, 10, 27);

    [Fact]
    public void RepetitionWindow_SpansTheNineDaysBeforeAndAfter()
    {
        // Spotlighted on a day, an edition comes back ten days later at the earliest, whichever of the two days
        // was drawn first: a time zone ahead draws the next day before the others.
        Assert.Equal((new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 5)), SpotlightDraw.RepetitionWindow(Day));
    }

    [Fact]
    public void Pick_TheOnlyCandidate()
    {
        var only = Candidate(spotlightCount: 3, isRecent: true, hasCover: false);

        Assert.Equal(only.EditionId, SpotlightDraw.Pick([only], 0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.99)]
    public void Pick_LeavesOutTheRecentlySpotlighted(double roll)
    {
        var recent = Candidate(isRecent: true);
        var other = Candidate();

        Assert.Equal(other.EditionId, SpotlightDraw.Pick([recent, other], roll));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.99, 1)]
    public void Pick_AmongAll_WhenAllWereRecentlySpotlighted(double roll, int expected)
    {
        SpotlightCandidate[] candidates = [Candidate(isRecent: true), Candidate(isRecent: true)];

        Assert.Equal(candidates[expected].EditionId, SpotlightDraw.Pick(candidates, roll));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.99)]
    public void Pick_PrefersAnEditionWithACover(double roll)
    {
        var withoutCover = Candidate(hasCover: false);
        var withCover = Candidate();

        Assert.Equal(withCover.EditionId, SpotlightDraw.Pick([withoutCover, withCover], roll));
    }

    [Fact]
    public void Pick_PrefersACoverOnlyAmongTheEditionsTheRepetitionRuleKeeps()
    {
        var recentWithCover = Candidate(isRecent: true);
        var withoutCover = Candidate(hasCover: false);

        Assert.Equal(withoutCover.EditionId, SpotlightDraw.Pick([recentWithCover, withoutCover], 0.0));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.99, 1)]
    public void Pick_AmongTheEditionsWithoutCover_WhenNoneHasOne(double roll, int expected)
    {
        SpotlightCandidate[] candidates = [Candidate(hasCover: false), Candidate(hasCover: false)];

        Assert.Equal(candidates[expected].EditionId, SpotlightDraw.Pick(candidates, roll));
    }

    [Theory]
    // Weights 1, 1/4 and 1/9, in proportion to 1 / (1 + n)²: out of 49/36, the never spotlighted takes the first
    // 36/49 of the rolls (0.7347), the one spotlighted once the next 9/49 (up to 0.9184), the last the remaining 4/49.
    [InlineData(0.0, 0)]
    [InlineData(0.73, 0)]
    [InlineData(0.74, 1)]
    [InlineData(0.91, 1)]
    [InlineData(0.92, 2)]
    [InlineData(0.999, 2)]
    public void Pick_FavoursTheLeastSpotlighted_InProportionToTheInverseSquare(double roll, int expected)
    {
        SpotlightCandidate[] candidates = [Candidate(spotlightCount: 0), Candidate(spotlightCount: 1), Candidate(spotlightCount: 2)];

        Assert.Equal(candidates[expected].EditionId, SpotlightDraw.Pick(candidates, roll));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    public void Pick_ARollOutsideZeroToOne_IsRefused(double roll)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpotlightDraw.Pick([Candidate()], roll));
    }

    [Fact]
    public void Pick_WithoutCandidate_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => SpotlightDraw.Pick([], 0.5));
    }

    private static SpotlightCandidate Candidate(int spotlightCount = 0, bool isRecent = false, bool hasCover = true) =>
        new(Guid.CreateVersion7(), spotlightCount, isRecent, hasCover);
}
