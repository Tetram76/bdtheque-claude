using Bdtheque.Frontend.Components.Pages;
using Bdtheque.Frontend.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

public sealed class NotYetImplementedTests : BunitContext
{
    public NotYetImplementedTests()
    {
        Services.AddLocalization();
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppPaths.NotYetImplemented);
    }

    [Fact]
    public void ReturnLink_LeadsBackToTheReferringPage()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Referer = "http://localhost/series?entry=B";

        var page = Render<NotYetImplemented>(parameters => parameters.AddCascadingValue(httpContext));

        Assert.Equal("/series?entry=B", page.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void ReturnLink_WithoutReferringPage_LeadsHome()
    {
        var page = Render<NotYetImplemented>(parameters => parameters.AddCascadingValue(new DefaultHttpContext()));

        Assert.Equal(AppPaths.Home, page.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Page_SaysThePageIsComing()
    {
        var page = Render<NotYetImplemented>(parameters => parameters.AddCascadingValue(new DefaultHttpContext()));

        Assert.False(string.IsNullOrWhiteSpace(page.Find("h1").TextContent));
    }
}
