namespace Bdtheque.Domain.Enums;

/// <summary>
/// Rendering engine of the browser of a visit (Moteur de rendu, modele-metier/visite-de-l-interface-web.md),
/// rather than the browser itself: several browsers share an engine, and display the pages alike.
/// </summary>
public enum RenderingEngine
{
    Blink = 1,
    Gecko = 2,
    WebKit = 3,
    Other = 4,
}
