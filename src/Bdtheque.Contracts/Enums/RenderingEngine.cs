using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Rendering engine of the browser of a visit (Moteur de rendu, modele-metier.md § Visite de l'interface web),
/// rather than the browser itself: several browsers share an engine, and display the pages alike.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RenderingEngine>))]
public enum RenderingEngine
{
    Blink = 1,
    Gecko = 2,
    WebKit = 3,
    Other = 4,
}
