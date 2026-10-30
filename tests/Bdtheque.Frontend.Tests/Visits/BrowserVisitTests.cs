using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Tests.Visits;

/// <summary>
/// The visit of the browser, kept in its session cookie (fonctionnel/mesure-audience.md, Visites): what the cookie
/// carries, and when the visit is over.
/// </summary>
public sealed class BrowserVisitTests
{
    [Fact]
    public void Visit_ReadFromItsCookie_IsTheVisitWritten()
    {
        var visit = new BrowserVisit(new DateOnly(2026, 10, 30), "base", new DateOnly(2026, 10, 31));

        Assert.Equal(visit, BrowserVisit.Parse(visit.Format()));
    }

    [Fact]
    public void Visit_WhoseDateOfTheThemeIsNotKnownYet_IsReadFromItsCookie()
    {
        var visit = new BrowserVisit(new DateOnly(2026, 10, 30), "base", ThemeDate: null);

        Assert.Equal(visit, BrowserVisit.Parse(visit.Format()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("base")]
    [InlineData("2026-10-30|base")]
    [InlineData("30/10/2026||base")]
    [InlineData("2026-10-30|31/10/2026|base")]
    [InlineData("2026-10-30|2026-10-31|")]
    public void Cookie_ThatIsNotAVisit_IsNoVisit(string? value) => Assert.Null(BrowserVisit.Parse(value));

    [Theory]
    [InlineData("2026-11-29", false)]
    [InlineData("2026-11-30", true)]
    public void Visit_IsOver_ThirteenMonthsAfterItsStart(string today, bool over)
    {
        // The most a tracer may live (fonctionnel/mesure-audience.md, Durées de conservation): a browser left open, which
        // keeps its session, does not keep its visit beyond.
        var visit = new BrowserVisit(new DateOnly(2025, 10, 30), "base", new DateOnly(2026, 11, 1));

        Assert.Equal(over, visit.IsOver(DateOnly.Parse(today, System.Globalization.CultureInfo.InvariantCulture)));
    }
}
