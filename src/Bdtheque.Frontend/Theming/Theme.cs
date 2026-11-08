namespace Bdtheque.Frontend.Theming;

/// <summary>
/// A theme of the application (choix-implementation/socle/themes.md): its key, carried by the
/// <c>data-theme</c> attribute of the page, and the stylesheet that gives its own values to the contracts of the
/// base theme. The base theme has no stylesheet of its own: <c>themes/base.css</c> is loaded for every theme.
/// </summary>
/// <param name="Script">
/// The script that draws the touches a stylesheet cannot (e.g. edges drawn by hand, tinted at random), hooked on the
/// same anchors; none for most themes. Loaded with every page, it acts only on a page of its theme.
/// </param>
/// <param name="PreloadedFont">The font of its text, which the page asks for before its stylesheets reveal it.</param>
/// <param name="PaintedByTheDay">
/// Whether its colours are those of the cover of the edition in the spotlight of the day (<see cref="DayPalette"/>), which
/// the page sets on its root element: only known at each request.
/// </param>
public sealed record Theme(string Key, string? Stylesheet, string? Script, string PreloadedFont, bool PaintedByTheDay = false);
