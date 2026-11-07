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

    // Whether the visit of the measurement is that of the administrator is kept with it.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Visit_RecordedByTheAudienceMeasurement_KeepsItsAudienceVisit(bool ofAdministrator)
    {
        var visit = new BrowserVisit(new DateOnly(2026, 10, 30), "base", ThemeDate: null, new MeasuredVisit(Guid.CreateVersion7(), ofAdministrator));

        Assert.Equal(visit, BrowserVisit.Parse(visit.Format()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("base")]
    [InlineData("2026-10-30|base")]
    [InlineData("30/10/2026||base||")]
    [InlineData("2026-10-30|31/10/2026|base||")]
    [InlineData("2026-10-30|2026-10-31|||")]
    // The forms of the cookie before the audience measurement, then before the kind of its visit: a new visit.
    [InlineData("2026-10-30|2026-10-31|base")]
    [InlineData("2026-10-30|2026-10-31|base|0196f1d9a1b27c3e8d4f5a6b7c8d9e0f")]
    [InlineData("2026-10-30|2026-10-31|base|not-a-visit|")]
    // A kind without a visit, or that is no kind.
    [InlineData("2026-10-30|2026-10-31|base||admin")]
    [InlineData("2026-10-30|2026-10-31|base|0196f1d9a1b27c3e8d4f5a6b7c8d9e0f|visitor")]
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
