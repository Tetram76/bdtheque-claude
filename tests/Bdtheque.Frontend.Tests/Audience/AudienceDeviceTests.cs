using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The only characteristics of the device a visit keeps (fonctionnel/mesure-audience.md, Type d'appareil, Moteur de
/// rendu): its category, and the rendering engine of its browser with its version, read from the user agents browsers
/// actually send.
/// </summary>
public sealed class AudienceDeviceTests
{
    [Theory]
    // Chrome and Edge on Windows: Blink, whose version is that of Chromium.
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36",
        DeviceType.Computer, RenderingEngine.Blink, "147")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 Edg/147.0.0.0",
        DeviceType.Computer, RenderingEngine.Blink, "147")]
    // Chrome on an Android smartphone, then on an Android tablet, which leaves out the « Mobile » token.
    [InlineData("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Mobile Safari/537.36",
        DeviceType.Smartphone, RenderingEngine.Blink, "147")]
    [InlineData("Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36",
        DeviceType.Tablet, RenderingEngine.Blink, "147")]
    // Firefox: Gecko, whose version is that of Firefox.
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:147.0) Gecko/20100101 Firefox/147.0",
        DeviceType.Computer, RenderingEngine.Gecko, "147")]
    [InlineData("Mozilla/5.0 (Android 14; Mobile; rv:147.0) Gecko/147.0 Firefox/147.0",
        DeviceType.Smartphone, RenderingEngine.Gecko, "147")]
    // Safari on a Mac: WebKit, whose version is that of Safari, which ships its releases.
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.1 Safari/605.1.15",
        DeviceType.Computer, RenderingEngine.WebKit, "26.1")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36",
        DeviceType.Computer, RenderingEngine.Blink, "147")]
    // Safari on an iPhone, whose user agent freezes the version of iOS (18_6) but not that of Safari.
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0.1 Mobile/15E148 Safari/604.1",
        DeviceType.Smartphone, RenderingEngine.WebKit, "26.0")]
    // Every browser of iOS draws with WebKit, that of its iOS, whose version Chrome and Firefox do not freeze.
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_5_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/147.0.7727.47 Mobile/15E148 Safari/604.1",
        DeviceType.Smartphone, RenderingEngine.WebKit, "17.5")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) FxiOS/130.0 Mobile/15E148 Safari/605.1.15",
        DeviceType.Smartphone, RenderingEngine.WebKit, "17.5")]
    // An iPad that asks for the website of a tablet.
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1",
        DeviceType.Tablet, RenderingEngine.WebKit, "17.5")]
    // Anything else: neither a browser this application knows the engine of, nor its version.
    [InlineData("curl/8.10.1", DeviceType.Computer, RenderingEngine.Other, null)]
    [InlineData("", DeviceType.Computer, RenderingEngine.Other, null)]
    public void Device_IsReadFromTheUserAgent(string userAgent, DeviceType deviceType, RenderingEngine engine, string? version)
    {
        Assert.Equal(new AudienceDevice(deviceType, engine, version), AudienceDevice.Of(userAgent, touchScreen: false));
    }

    [Fact]
    public void Device_WithoutUserAgent_IsAComputerOfAnotherEngine()
    {
        Assert.Equal(new AudienceDevice(DeviceType.Computer, RenderingEngine.Other, null), AudienceDevice.Of(null, touchScreen: false));
    }

    [Fact]
    public void Mac_WithATouchScreen_IsAnIPad_ThatAskedForTheWebsiteOfAComputer()
    {
        // Safari on an iPad asks for the website of a computer by default, presenting itself as a Mac, which has no touch
        // screen: only the browser tells them apart (touch-screen.js).
        const string ipadAsAComputer =
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.1 Safari/605.1.15";

        Assert.Equal(new AudienceDevice(DeviceType.Tablet, RenderingEngine.WebKit, "26.1"), AudienceDevice.Of(ipadAsAComputer, touchScreen: true));
    }

    [Fact]
    public void Computer_WithATouchScreen_RemainsAComputer()
    {
        const string touchLaptop =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36";

        Assert.Equal(DeviceType.Computer, AudienceDevice.Of(touchLaptop, touchScreen: true).DeviceType);
    }
}
