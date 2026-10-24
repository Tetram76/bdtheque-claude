// Keeps the time zone of the browser in the cookie each request brings to the server, which presents the dates and
// times in it (Formatting/BrowserTimeZone.cs). Checked again when the user comes back to the tab: a traveller's
// browser that changed time zone is followed.
(() => {
    const cookieName = "Bdtheque.TimeZone";

    const stored = () => {
        const cookie = document.cookie.split("; ").find(c => c.startsWith(`${cookieName}=`));
        return cookie ? decodeURIComponent(cookie.substring(cookieName.length + 1)) : null;
    };

    const check = () => {
        const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        if (!zone || stored() === zone) {
            return;
        }
        // A year, below the 13 months a cookie may live (fonctionnel/mesure-audience.md).
        document.cookie = `${cookieName}=${encodeURIComponent(zone)}; path=/; max-age=31536000; samesite=lax`;
        // A page that presents dates and times in another time zone is rendered again, once only: never while the
        // cookie is refused, which would render it again and again.
        if (stored() === zone && document.documentElement.dataset.timeZone !== zone && document.querySelector("time[data-local]")) {
            location.reload();
        }
    };

    check();
    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "visible") {
            check();
        }
    });
})();
