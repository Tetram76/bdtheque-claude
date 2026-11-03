using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The opposition of a visitor to the audience measurement (fonctionnel/mesure-audience.md, Information et opposition):
/// kept by their browser until they come back on it, within the 13 months a tracer may live.
/// </summary>
public sealed class AudienceOppositionTests
{
    private static readonly BrowserVisit Visit = new(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3), Guid.CreateVersion7());

    [Fact]
    public void Visitor_WithoutChoice_IsNotOpposed()
    {
        var request = new TestRequest();

        Assert.False(OppositionOf(request).IsOpposed);
    }

    [Fact]
    public void Visitor_WhoseBrowserKeepsTheOpposition_IsOpposed()
    {
        var request = new TestRequest(new Dictionary<string, string> { [AudienceOpposition.CookieName] = "1" });

        Assert.True(OppositionOf(request).IsOpposed);
    }

    [Fact]
    public async Task Opposition_IsKeptByTheBrowser_ForAYear()
    {
        var request = new TestRequest();
        var opposition = OppositionOf(request);

        opposition.Oppose();
        await request.StartResponseAsync();

        Assert.True(opposition.IsOpposed);
        var written = Assert.Single(request.SetCookies, c => c.StartsWith($"{AudienceOpposition.CookieName}=", StringComparison.Ordinal));
        Assert.Contains("max-age=31536000", written, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", written, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Opposition_ForgetsTheAudienceVisit_AndTheTouchScreen()
    {
        // Nothing of the measurement is left in the browser: its visit goes on for the theme only.
        var request = new TestRequest(new Dictionary<string, string>
        {
            [BrowserVisit.CookieName] = Visit.Format(),
            [AudienceDevice.TouchScreenCookieName] = "1",
        });

        OppositionOf(request).Oppose();
        await request.StartResponseAsync();

        Assert.Equal((Visit with { AudienceVisitId = null }).Format(), request.SetCookie(BrowserVisit.CookieName));
        var touchScreen = Assert.Single(request.SetCookies, c => c.StartsWith($"{AudienceDevice.TouchScreenCookieName}=", StringComparison.Ordinal));
        Assert.Contains("expires=Thu, 01 Jan 1970", touchScreen, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Withdrawal_ForgetsTheOpposition()
    {
        var request = new TestRequest(new Dictionary<string, string> { [AudienceOpposition.CookieName] = "1" });
        var opposition = OppositionOf(request);

        opposition.Withdraw();

        Assert.False(opposition.IsOpposed);
        var written = Assert.Single(request.SetCookies);
        Assert.StartsWith($"{AudienceOpposition.CookieName}=;", written);
        Assert.Contains("expires=Thu, 01 Jan 1970", written, StringComparison.OrdinalIgnoreCase);
    }

    private static AudienceOpposition OppositionOf(TestRequest request) => new(request.Accessor, new VisitCookie(request.Accessor));
}
