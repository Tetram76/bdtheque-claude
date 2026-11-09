namespace Bdtheque.Frontend.Components.Presentation;

/// <summary>
/// The looks of a <see cref="Tag"/>. Filled and dashed say at first glance whether what it marks is there or only expected
/// (e.g. « Dans la collection » filled, « Hors collection » or « Intention d'achat » dashed, visuel/liste-albums/contenu.md);
/// accent and outlined tell apart what a purchase intent targets, the whole album or one edition
/// (visuel/listes/liste-par-liste.md).
/// </summary>
public enum TagVariant
{
    Filled,
    Dashed,

    /// <summary>Filled with the accent of the theme.</summary>
    Accent,

    /// <summary>Framed by a solid border of the color of the text, without fill.</summary>
    Outlined,
}
