using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bdtheque.EndToEnd.Tests.Errors;

/// <summary>
/// A page of the tests only, whose interactive component fails on demand: what the application presents then, the banner
/// of an unhandled error and the dialog of the reconnection, can only be reached through such a failure, which no screen
/// offers. Routed by the application through <c>PageAssemblies</c>, as the page of the controls.
/// </summary>
[Route(Path)]
public sealed class FailurePage : ComponentBase
{
    public const string Path = "/test/failure";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h1");
        builder.AddContent(1, "Échec");
        builder.CloseElement();

        builder.OpenComponent<FailingButton>(2);
        builder.AddComponentRenderMode(RenderMode.InteractiveServer);
        builder.CloseComponent();
    }
}

/// <summary>A button that fails as it is clicked, marked once its circuit renders it, from which it answers clicks.</summary>
public sealed class FailingButton : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "type", "button");
        builder.AddAttribute(2, "data-interactive", RendererInfo.IsInteractive ? "true" : "false");
        builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, Fail));
        builder.AddContent(4, "Échouer");
        builder.CloseElement();
    }

    private static void Fail() => throw new InvalidOperationException("A failure the tests ask for.");
}
