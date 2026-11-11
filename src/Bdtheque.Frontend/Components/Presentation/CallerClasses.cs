namespace Bdtheque.Frontend.Components.Presentation;

/// <summary>
/// The class a caller gives a component that has classes of its own, the anchors of the styles of the theme: added to them,
/// never replacing them, as an attribute splatted after them would (documentation of ASP.NET Core, « Attribute splatting »).
/// </summary>
internal static class CallerClasses
{
    private const string Class = "class";

    /// <summary>The classes of the component, then that of the caller, if any.</summary>
    public static string Merge(string own, IReadOnlyDictionary<string, object>? attributes) =>
        attributes?.GetValueOrDefault(Class) is string { Length: > 0 } caller ? $"{own} {caller}" : own;

    /// <summary>The other attributes of the caller, which the component splats.</summary>
    public static IEnumerable<KeyValuePair<string, object>>? Others(IReadOnlyDictionary<string, object>? attributes) =>
        attributes?.Where(attribute => attribute.Key != Class);
}
