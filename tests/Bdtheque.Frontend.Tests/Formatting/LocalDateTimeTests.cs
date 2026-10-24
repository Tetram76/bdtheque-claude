using System.Globalization;
using Bdtheque.Frontend.Components.Formatting;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

public sealed class LocalDateTimeTests : BunitContext
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    public LocalDateTimeTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Services.AddLocalization();
        Services.AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" });
        Services.AddScoped<DisplayFormatter>();
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _previous;
        base.Dispose(disposing);
    }

    [Fact]
    public void DateAndTime_IsPresentedInTheTimeZoneOfTheBrowser_AndMarkedAsSuch()
    {
        var instant = new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);

        var time = Render<LocalDateTime>(parameters => parameters.Add(p => p.Value, instant)).Find("time");

        Assert.Equal("01/01/2026 00:30", time.TextContent);
        Assert.Equal("2025-12-31T23:30:00.0000000+00:00", time.GetAttribute("datetime"));
        // What the script of the time zone looks for, to render the page again once it knows the browser's.
        Assert.True(time.HasAttribute("data-local"));
    }
}
