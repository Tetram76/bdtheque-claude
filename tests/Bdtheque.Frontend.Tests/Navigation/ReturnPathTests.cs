using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Navigation;

public sealed class ReturnPathTests
{
    private static readonly Uri CurrentPage = new("https://bdtheque.example/not-yet-implemented?ReturnUrl=%2Fadmin");

    [Theory]
    [InlineData("https://bdtheque.example/series?entry=A", "/series?entry=A")]
    [InlineData("https://BDTHEQUE.example/", "/")]
    [InlineData("https://bdtheque.example/admin/albums", "/admin/albums")]
    public void FromReferer_PageOfTheApplication_ReturnsItsPathAndQuery(string referer, string expected) =>
        Assert.Equal(expected, ReturnPath.FromReferer(referer, CurrentPage));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not an address")]
    [InlineData("/series")]
    [InlineData("https://other.example/series")]
    [InlineData("http://bdtheque.example/series")]
    [InlineData("https://bdtheque.example:8443/series")]
    [InlineData("https://bdtheque.example/not-yet-implemented")]
    public void FromReferer_NoPageOfTheApplicationToReturnTo_ReturnsHome(string? referer) =>
        Assert.Equal(AppPaths.Home, ReturnPath.FromReferer(referer, CurrentPage));

    [Theory]
    [InlineData("/admin", "/admin")]
    [InlineData("/admin/albums?entry=B", "/admin/albums?entry=B")]
    [InlineData("/", "/")]
    public void AfterSignIn_PathOfTheApplication_IsReturnedTo(string returnUrl, string expected) =>
        Assert.Equal(expected, ReturnPath.AfterSignIn(returnUrl));

    // Never a way out of the application: another site, written in full or by a path that the browser reads as one.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("https://other.example/admin")]
    [InlineData("//other.example/admin")]
    [InlineData(@"/\other.example/admin")]
    [InlineData("javascript:alert(1)")]
    public void AfterSignIn_NoPathOfTheApplication_ReturnsTheAdministration(string? returnUrl) =>
        Assert.Equal(AppPaths.AdminHome, ReturnPath.AfterSignIn(returnUrl));
}
