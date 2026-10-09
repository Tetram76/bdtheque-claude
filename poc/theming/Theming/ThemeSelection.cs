namespace ThemeProto.Theming;

/// <summary>The theme of the current request, set once by <see cref="ThemeSelectionMiddleware"/>.</summary>
public sealed class CurrentTheme
{
    public Theme Theme { get; set; } = default!;
}

/// <summary>
/// Draws a theme for a visitor without one and keeps it in a session cookie, so the whole visit
/// shares it. The <c>theme</c> query parameter only exists to demonstrate the prototype.
/// </summary>
public sealed class ThemeSelectionMiddleware(RequestDelegate next, ThemeCatalog catalog)
{
    public const string CookieName = "bdt-theme";

    public Task InvokeAsync(HttpContext context, CurrentTheme current)
    {
        var requested = context.Request.Query["theme"].ToString();
        var theme = requested == "new" ? catalog.PickRandom()
            : catalog.Find(requested) ?? catalog.Find(context.Request.Cookies[CookieName]);

        if (theme is null || theme.Id != context.Request.Cookies[CookieName])
        {
            theme ??= catalog.PickRandom();
            // No expiry: a browser session cookie, dropped when the browser closes.
            context.Response.Cookies.Append(CookieName, theme.Id, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
            });
        }

        current.Theme = theme;
        return next(context);
    }
}
