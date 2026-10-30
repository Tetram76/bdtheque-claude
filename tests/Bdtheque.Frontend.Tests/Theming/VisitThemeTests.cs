using Bdtheque.Frontend.Theming;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Tests.Theming;

/// <summary>
/// The theme of the visit (fonctionnel/index.md § Design et charte graphique, Thème actif): drawn at random among the
/// offered themes at the start of a visit, kept for the whole visit, drawn again for each new date of the browser.
/// </summary>
public sealed class VisitThemeTests
{
    private static readonly Theme First = new("first", "themes/first.css");
    private static readonly Theme Second = new("second", "themes/second.css");
    private static readonly Theme Third = new("third", "themes/third.css");
    private static readonly IReadOnlyList<Theme> Offered = [First, Second, Third];

    private static readonly DateOnly Today = new(2026, 10, 30);
    private static readonly DateOnly Tomorrow = new(2026, 10, 31);

    [Fact]
    public void NewVisit_StartsToday_WithTheThemeDrawnAmongTheOfferedOnes()
    {
        var draws = new List<int>();

        var visit = VisitTheme.Continue(null, Today, Today, Offered, Draw(1, draws));

        Assert.Equal(new BrowserVisit(Today, Second.Key, Today), visit);
        Assert.Equal([Offered.Count], draws);
    }

    [Fact]
    public void Visit_KeepsItsTheme_ForTheWholeDate()
    {
        var visit = new BrowserVisit(Today, Second.Key, Today);

        Assert.Same(visit, VisitTheme.Continue(visit, Today, Today, Offered, NoDraw));
    }

    [Fact]
    public void Visit_ThatGoesOnAtANewDate_DrawsAThemeAgain_AndGoesOn()
    {
        var visit = new BrowserVisit(Today, Second.Key, Today);

        Assert.Equal(new BrowserVisit(Today, Third.Key, Tomorrow), VisitTheme.Continue(visit, Tomorrow, Tomorrow, Offered, Draw(2)));
    }

    [Fact]
    public void NewDate_IsThatOfTheBrowser_NotThatOfTheServer()
    {
        // Past midnight in the browser, not yet in universal time: a new date all the same.
        var visit = new BrowserVisit(Today, Second.Key, Today);

        Assert.Equal(new BrowserVisit(Today, First.Key, Tomorrow), VisitTheme.Continue(visit, Today, Tomorrow, Offered, Draw(0)));
    }

    [Fact]
    public void FirstPage_BeforeTheBrowserGaveItsTimeZone_LeavesTheDateOfTheThemeUnknown()
    {
        // No date of the browser to draw for: universal time could be another date than the browser's, whose next page
        // would then draw a theme again in the middle of the visit's first date.
        var visit = VisitTheme.Continue(null, Today, browserToday: null, Offered, Draw(1));

        Assert.Equal(new BrowserVisit(Today, Second.Key, ThemeDate: null), visit);
    }

    [Fact]
    public void ThemeDrawnBeforeTheTimeZoneWasKnown_IsTheThemeOfTheFirstDateOfTheBrowser()
    {
        var visit = new BrowserVisit(Today, Second.Key, ThemeDate: null);

        Assert.Equal(new BrowserVisit(Today, Second.Key, Tomorrow), VisitTheme.Continue(visit, Today, Tomorrow, Offered, NoDraw));
    }

    [Fact]
    public void Visit_WhileTheTimeZoneOfTheBrowserIsUnknown_KeepsItsTheme()
    {
        // A browser that stopped giving its time zone (cookie refused or altered): no date to compare, the theme stays.
        var visit = new BrowserVisit(Today, Second.Key, Today);

        Assert.Same(visit, VisitTheme.Continue(visit, Tomorrow, browserToday: null, Offered, NoDraw));
    }

    [Fact]
    public void Visit_WhoseThemeIsNoLongerOffered_DrawsAThemeAgain_AndGoesOn()
    {
        // A theme withdrawn from the catalog by a new version, or a cookie altered by hand.
        var visit = new BrowserVisit(Today, "withdrawn", Today);

        Assert.Equal(new BrowserVisit(Today, First.Key, Today), VisitTheme.Continue(visit, Today, Today, Offered, Draw(0)));
    }

    [Fact]
    public void VisitThatIsOver_GivesWayToANewVisit()
    {
        var visit = new BrowserVisit(new DateOnly(2025, 9, 30), Second.Key, Today);

        Assert.Equal(new BrowserVisit(Today, Third.Key, Today), VisitTheme.Continue(visit, Today, Today, Offered, Draw(2)));
    }

    [Fact]
    public void SingleOfferedTheme_IsRetainedOutright() =>
        Assert.Equal(new BrowserVisit(Today, First.Key, Today), VisitTheme.Continue(null, Today, Today, [First], Random.Shared.Next));

    [Fact]
    public void Theme_OfAVisit_IsItsOfferedTheme()
    {
        Assert.Same(Second, VisitTheme.Of(new BrowserVisit(Today, Second.Key, Today), Offered));
    }

    private static Func<int, int> Draw(int index, List<int>? draws = null) => count =>
    {
        draws?.Add(count);
        return index;
    };

    private static int NoDraw(int count) => throw new InvalidOperationException("No theme is to be drawn.");
}
