// Tells the server, by a cookie, that the browser has a touch screen (Audience/AudienceDevice.cs): Safari on an iPad asks
// for the website of a computer by default, and presents itself as a Mac, which has none; only the browser tells them
// apart. The audience measurement alone uses it: nothing is written while the visitor opposes it.
(() => {
    const cookieName = "Bdtheque.TouchScreen";

    if (document.documentElement.dataset.audience === "opposed" || navigator.maxTouchPoints <= 1
        || document.cookie.split("; ").some(c => c.startsWith(`${cookieName}=`))) {
        return;
    }
    // A year, below the 13 months a cookie may live (fonctionnel/mesure-audience.md).
    document.cookie = `${cookieName}=1; path=/; max-age=31536000; samesite=lax`;
})();
