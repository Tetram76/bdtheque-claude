namespace Bdtheque.Frontend.Theming;

/// <summary>
/// A theme of the application (choix-implementation/socle.md § Thèmes : mise en œuvre): its key, carried by the
/// <c>data-theme</c> attribute of the page, and the stylesheet that gives its own values to the contracts of the
/// base theme. The base theme has no stylesheet of its own: <c>themes/base.css</c> is loaded for every theme.
/// </summary>
public sealed record Theme(string Key, string? Stylesheet);
