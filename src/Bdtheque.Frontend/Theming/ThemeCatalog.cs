namespace Bdtheque.Frontend.Theming;

/// <summary>The themes the application offers, among which the theme of a visit is drawn.</summary>
public static class ThemeCatalog
{
    /// <summary>The neutral base theme, whose contracts every theme follows (visuel/charte-graphique.md § Thème de base).</summary>
    public static readonly Theme Base = new("base", Stylesheet: null, Script: null, "fonts/inter/inter-latin-wght-normal.woff2");

    /// <summary>« La planche encrée », a page of comic (visuel/dashboard.md § La planche encrée).</summary>
    public static readonly Theme Planche = new("planche", "themes/planche.css", "themes/planche.js", "fonts/delius/delius-latin-400-normal.woff2");

    /// <summary>The themes offered. The reference captures of the screens are taken in each of them.</summary>
    public static readonly IReadOnlyList<Theme> Offered = [Base, Planche];
}
