using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Tests.Visits;

/// <summary>
/// The cookie of the visit, which the theme of the visit and the audience measurement both continue in a request: written
/// once, with the visit as the request leaves it, and only when it changed.
/// </summary>
public sealed class VisitCookieTests
{
    private static readonly BrowserVisit Visit = new(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3));

    [Fact]
    public void Visit_IsTheOneTheRequestBrings()
    {
        var request = RequestWith(Visit);

        Assert.Equal(Visit, new VisitCookie(request.Accessor).Current);
    }

    [Fact]
    public void Request_WithoutVisit_HasNone()
    {
        Assert.Null(new VisitCookie(new TestRequest().Accessor).Current);
    }

    [Fact]
    public async Task Visit_ContinuedTwice_IsWrittenOnce_AsTheRequestLeavesIt()
    {
        var request = new TestRequest();
        var cookie = new VisitCookie(request.Accessor);
        var continued = Visit with { AudienceVisitId = Guid.CreateVersion7() };

        cookie.Set(Visit);
        cookie.Set(continued);
        await request.StartResponseAsync();

        Assert.Single(request.SetCookies);
        Assert.Equal(continued.Format(), request.SetCookie(BrowserVisit.CookieName));
        Assert.Equal(continued, cookie.Current);
    }

    [Fact]
    public async Task SessionCookie_ReadByNoScript_SecureOnHttpsOnly()
    {
        var request = new TestRequest();

        new VisitCookie(request.Accessor).Set(Visit);
        await request.StartResponseAsync();

        // Without an expiry: a session cookie, which the browser forgets with its session, as a visit ends.
        var written = Assert.Single(request.SetCookies);
        Assert.DoesNotContain("expires", written, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age", written, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", written, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", written, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Visit_LeftAsTheRequestBroughtIt_IsNotWrittenAgain()
    {
        var request = RequestWith(Visit);

        new VisitCookie(request.Accessor).Set(Visit with { });
        await request.StartResponseAsync();

        Assert.Empty(request.SetCookies);
    }

    [Fact]
    public async Task Visit_ChangedThenChangedBack_IsNotWritten()
    {
        var request = RequestWith(Visit);
        var cookie = new VisitCookie(request.Accessor);

        cookie.Set(Visit with { AudienceVisitId = Guid.CreateVersion7() });
        cookie.Set(Visit);
        await request.StartResponseAsync();

        Assert.Empty(request.SetCookies);
    }

    [Fact]
    public void Visit_ContinuedOnceTheResponseStarted_IsAnError()
    {
        // An interactive circuit keeps the request that opened it, whose response is long gone: nothing could be written.
        var request = new TestRequest();
        request.MarkStarted();

        Assert.Throws<InvalidOperationException>(() => new VisitCookie(request.Accessor).Set(Visit));
    }

    private static TestRequest RequestWith(BrowserVisit visit) =>
        new(new Dictionary<string, string> { [BrowserVisit.CookieName] = visit.Format() });
}
