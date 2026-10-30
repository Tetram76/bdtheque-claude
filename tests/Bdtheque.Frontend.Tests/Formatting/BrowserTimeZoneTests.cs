using System.Globalization;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>The time zone of the browser, in which the dates and times are presented (fonctionnel/presentation.md).</summary>
public sealed class BrowserTimeZoneTests : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    public BrowserTimeZoneTests() => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

    public void Dispose() => CultureInfo.CurrentCulture = _previous;

    [Fact]
    public void Zone_IsTheOneTheBrowserGave() => Assert.Equal("Europe/Paris", new BrowserTimeZone { Name = "Europe/Paris" }.Zone.Id);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Nowhere/Atlantis")]
    [InlineData("Romance Standard Time")]
    public void Zone_WithoutAKnownIanaName_IsUniversalTime(string? name)
    {
        var timeZone = new BrowserTimeZone { Name = name };

        Assert.Same(TimeZoneInfo.Utc, timeZone.Zone);
        Assert.False(timeZone.IsKnown);
    }

    [Theory]
    [InlineData("Europe/Paris")]
    [InlineData("Etc/UTC")]
    public void Zone_WithAKnownIanaName_IsKnown(string name) => Assert.True(new BrowserTimeZone { Name = name }.IsKnown);

    [Fact]
    public void Today_IsTheDateOfTheBrowser_NotThatOfTheServer()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 1, 1), new BrowserTimeZone { Name = "Europe/Paris" }.Today(clock));
        Assert.Equal(new DateOnly(2025, 12, 31), new BrowserTimeZone { Name = "America/Montreal" }.Today(clock));
    }

    [Fact]
    public void DateTime_IsPresentedInTheTimeZoneOfTheBrowser()
    {
        var formatter = FormatterIn("Europe/Paris");

        Assert.Equal("01/01/2026 00:30", formatter.DateTime(new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DateTime_InSummer_FollowsSummerTime() =>
        Assert.Equal("01/07/2026 14:00", FormatterIn("Europe/Paris").DateTime(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero)));

    private static DisplayFormatter FormatterIn(string zone)
    {
        var services = new ServiceCollection().AddLogging().AddLocalization()
            .AddScoped(_ => new BrowserTimeZone { Name = zone })
            .AddScoped<DisplayFormatter>()
            .BuildServiceProvider();
        return services.CreateScope().ServiceProvider.GetRequiredService<DisplayFormatter>();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
