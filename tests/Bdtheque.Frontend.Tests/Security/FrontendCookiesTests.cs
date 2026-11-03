using Bdtheque.Frontend.Security;
using Microsoft.AspNetCore.Http;

namespace Bdtheque.Frontend.Tests.Security;

/// <summary>The cookies frontend writes itself: Secure on an HTTPS request only, so that the internal deployment in HTTP keeps them.</summary>
public sealed class FrontendCookiesTests
{
    [Theory]
    [InlineData("https", true)]
    [InlineData("http", false)]
    public void Cookie_IsSecure_OnAnHttpsRequestOnly(string scheme, bool secure)
    {
        var request = new DefaultHttpContext().Request;
        request.Scheme = scheme;

        var options = FrontendCookies.For(request);

        Assert.Equal((secure, true, SameSiteMode.Lax, "/"), (options.Secure, options.HttpOnly, options.SameSite, options.Path));
    }

    [Fact]
    public void Cookie_WithoutLifetime_IsASessionCookie()
    {
        var request = new DefaultHttpContext().Request;

        Assert.Null(FrontendCookies.For(request).MaxAge);
        Assert.Equal(TimeSpan.FromDays(365), FrontendCookies.For(request, TimeSpan.FromDays(365)).MaxAge);
    }
}
