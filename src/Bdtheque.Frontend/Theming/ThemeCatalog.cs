namespace Bdtheque.Frontend.Theming;

/// <summary>The themes the application offers, among which the theme of a visit is drawn.</summary>
public static class ThemeCatalog
{
    /// <summary>The neutral base theme, whose contracts every theme follows (visuel/charte-graphique/theme-de-base.md).</summary>
    public static readonly Theme Base = new("base", Stylesheet: null, Script: null, "fonts/inter/inter-latin-wght-normal.woff2");

    /// <summary>« La planche encrée », a page of comic (visuel/dashboard/la-planche-encree.md).</summary>
    public static readonly Theme Planche = new("planche", "themes/planche.css", "themes/planche.js", "fonts/delius/delius-latin-400-normal.woff2");

    /// <summary>« La gazette », the front page of a newspaper (visuel/dashboard/la-gazette.md).</summary>
    public static readonly Theme Gazette = new("gazette", "themes/gazette.css", Script: null, "fonts/dm-sans/dm-sans-latin-wght-normal.woff2");

    /// <summary>
    /// « La couverture du jour », whose colours are those of the cover of the edition in the spotlight
    /// (visuel/dashboard/la-couverture-du-jour.md).
    /// </summary>
    public static readonly Theme Couverture = new(
        "couverture", "themes/couverture.css", Script: null, "fonts/inter-tight/inter-tight-latin-wght-normal.woff2", PaintedByTheDay: true);

    /// <summary>The themes offered. The reference captures of the screens are taken in each of them.</summary>
    public static readonly IReadOnlyList<Theme> Offered = [Base, Planche, Gazette, Couverture];
}
