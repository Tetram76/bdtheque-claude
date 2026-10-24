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
}
