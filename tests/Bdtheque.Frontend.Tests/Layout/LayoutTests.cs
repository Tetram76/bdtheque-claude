using Bdtheque.Frontend.Components.Layout;
using Bdtheque.Frontend.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Layout;

public sealed class LayoutTests : BunitContext
{
    private static readonly RenderFragment Body = builder => builder.AddMarkupContent(0, "<p id=\"body\">content</p>");

    public LayoutTests() => Services.AddLocalization();

    [Fact]
    public void PublicLayout_FramesThePageWithTheLinksOfTheConsultation()
    {
        var layout = Render<PublicLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.NotNull(layout.Find("main #body"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.Home}']"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.AdminHome}']"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.Help}']"));
    }

    [Fact]
    public void AdminLayout_FramesThePageWithTheLinksOfTheAdministration()
    {
        var layout = Render<AdminLayout>(parameters => parameters.Add(p => p.Body, Body));

        Assert.NotNull(layout.Find("main #body"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.AdminHome}']"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.Home}']"));
        Assert.NotNull(layout.Find($"header a[href='{AppPaths.Help}']"));
    }
}
