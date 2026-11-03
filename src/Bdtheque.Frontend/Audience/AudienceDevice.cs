using System.Text.RegularExpressions;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Frontend.Audience;

/// <summary>
/// The only characteristics of the device a visit keeps (fonctionnel/mesure-audience.md, Type d'appareil, Moteur de
/// rendu): its category, and the rendering engine of its browser with its version — the major version of Blink and
/// Gecko, whose releases follow those of Chromium and Firefox, the version of Safari for WebKit, whose minor releases
/// bring features. Read from the user agent the request brings, the page being rendered by the server: nothing more
/// precise is kept, nor anything that identifies the device.
/// </summary>
public sealed partial record AudienceDevice(DeviceType DeviceType, RenderingEngine RenderingEngine, string? EngineVersion)
{
    /// <summary>
    /// Cookie a script writes on a browser with a touch screen (touch-screen.js): Safari on an iPad asks for the website
    /// of a computer by default, and presents itself as a Mac, which has none.
    /// </summary>
    public const string TouchScreenCookieName = "Bdtheque.TouchScreen";

    public static AudienceDevice Of(string? userAgent, bool touchScreen)
    {
        userAgent ??= "";
        var (engine, version) = EngineOf(userAgent);
        return new AudienceDevice(DeviceTypeOf(userAgent, touchScreen), engine, version);
    }

    private static DeviceType DeviceTypeOf(string userAgent, bool touchScreen)
    {
        // « Mobi » marks a smartphone, as MDN recommends (Browser detection using the user agent); an Android tablet leaves
        // it out, as an iPad does.
        if (userAgent.Contains("Mobi", StringComparison.Ordinal) && !userAgent.Contains("iPad", StringComparison.Ordinal))
            return DeviceType.Smartphone;
        if (userAgent.Contains("iPad", StringComparison.Ordinal) || userAgent.Contains("Android", StringComparison.Ordinal)
            || (touchScreen && userAgent.Contains("Macintosh", StringComparison.Ordinal)))
            return DeviceType.Tablet;
        return DeviceType.Computer;
    }

    private static (RenderingEngine Engine, string? Version) EngineOf(string userAgent)
    {
        // Every browser of iOS and iPadOS draws with the WebKit of the system: that of Safari, or of the system itself,
        // whose version Chrome and Firefox report, Safari freezing it (since Safari 26).
        if (IOs().Match(userAgent) is { Success: true } ios)
            return (RenderingEngine.WebKit, SafariVersion(userAgent) ?? MajorMinor(ios.Groups[Major].Value, ios.Groups[Minor].Value));
        if (Firefox().Match(userAgent) is { Success: true } firefox)
            return (RenderingEngine.Gecko, firefox.Groups[Major].Value);
        if (Chromium().Match(userAgent) is { Success: true } chromium)
            return (RenderingEngine.Blink, chromium.Groups[Major].Value);
        if (userAgent.Contains("AppleWebKit/", StringComparison.Ordinal) && SafariVersion(userAgent) is { } safari)
            return (RenderingEngine.WebKit, safari);
        return (RenderingEngine.Other, null);
    }

    private static string? SafariVersion(string userAgent) =>
        Safari().Match(userAgent) is { Success: true } safari ? MajorMinor(safari.Groups[Major].Value, safari.Groups[Minor].Value) : null;

    private static string MajorMinor(string major, string minor) => minor.Length == 0 ? major : $"{major}.{minor}";

    // The groups of the expressions below that capture a version.
    private const string Major = "major";
    private const string Minor = "minor";

    [GeneratedRegex(@"\((?:iPhone|iPad|iPod);.*? OS (?<major>\d+)(?:_(?<minor>\d+))?")]
    private static partial Regex IOs();

    // The Gecko token has a slash, unlike the « like Gecko » of the other engines.
    [GeneratedRegex(@"Gecko/\S+ Firefox/(?<major>\d+)")]
    private static partial Regex Firefox();

    // Chrome and every browser built on Chromium (Edge, Opera, Samsung Internet…) keep its token.
    [GeneratedRegex(@"(?:Chrome|Chromium)/(?<major>\d+)")]
    private static partial Regex Chromium();

    [GeneratedRegex(@"Version/(?<major>\d+)(?:\.(?<minor>\d+))?")]
    private static partial Regex Safari();
}
