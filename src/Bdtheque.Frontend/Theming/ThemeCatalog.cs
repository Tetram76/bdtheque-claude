namespace Bdtheque.Frontend.Theming;

/// <summary>The themes the application offers, among which the theme of a visit is drawn.</summary>
public static class ThemeCatalog
{
    /// <summary>The neutral base theme, whose contracts every theme follows (visuel/charte-graphique.md § Thème de base).</summary>
    public static readonly Theme Base = new("base", Stylesheet: null);

    /// <summary>
    /// The themes offered: the base theme alone until the first theme built on it (#195). The reference captures of
    /// the screens are taken in each of them.
    /// </summary>
    public static readonly IReadOnlyList<Theme> Offered = [Base];
}
