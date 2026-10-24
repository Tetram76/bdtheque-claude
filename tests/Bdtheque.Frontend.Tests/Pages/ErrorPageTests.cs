using Bdtheque.Frontend.Components.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

public sealed class ErrorPageTests : BunitContext
{
    public ErrorPageTests() => Services.AddLocalization();

    [Fact]
    public void UnhandledError_IsPresentedAsATechnicalError_WithoutDetail()
    {
        var page = Render<Error>();

        Assert.Equal("technical", page.Find("[role=alert]").GetAttribute("data-error-category"));
        Assert.DoesNotContain("ASPNETCORE_ENVIRONMENT", page.Markup);
        Assert.DoesNotContain("Request ID", page.Markup);
    }
}
