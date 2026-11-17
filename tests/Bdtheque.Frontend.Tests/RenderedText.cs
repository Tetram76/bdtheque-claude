using Microsoft.AspNetCore.Components;

namespace Bdtheque.Frontend.Tests;

/// <summary>What a rendered component presents to read, as opposed to its markup.</summary>
internal static class RenderedText
{
    /// <summary>
    /// The text of every node the component renders, without their attributes. A rule about what is presented is checked on
    /// it, never on the markup: among the attributes is the CSS isolation scope the Razor SDK generates (<c>b-</c> and ten
    /// letters or digits), whose value the build sets, not the component, and which may hold the very text the rule forbids
    /// (#500).
    /// </summary>
    public static string TextContent<TComponent>(this IRenderedComponent<TComponent> component) where TComponent : IComponent =>
        string.Concat(component.Nodes.Select(node => node.TextContent));
}
