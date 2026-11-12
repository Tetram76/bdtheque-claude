extern alias frontend;

using Microsoft.AspNetCore.Http;
using frontend::Bdtheque.Frontend.Theming;
using frontend::Bdtheque.Frontend.Visits;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The theme a test fixes for its requests, by a header of its browser context, rather than the one the application
/// would draw: a test runs in each offered theme, never in a drawn one (<see cref="TestThemes"/>). It wraps the selector
/// of the application in the host of the tests only (<see cref="ApplicationHost"/>), never in production.
/// <para>
/// Only the theme changes: the selector of the application still continues the visit, as at every page (a page records
/// its usages in it, choix-implementation/socle/themes.md), and the visit takes the fixed theme, which its cookie keeps,
/// as the choice of the validation environment does (<c>ChosenThemeSelector</c>).
/// </para>
/// </summary>
public sealed class RequestThemeSelector(IHttpContextAccessor httpContextAccessor, VisitCookie visitCookie, IThemeSelector application)
    : IThemeSelector
{
    public const string HeaderName = "X-Test-Theme";

    private Theme? _current;

    public Theme Current => _current ??= Select();

    private Theme Select()
    {
        var drawn = application.Current;
        if (httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString() is not { Length: > 0 } key)
            return drawn;

        var fixedTheme = TestThemes.Of(key);
        if (visitCookie.Current is { } visit)
            visitCookie.Set(visit with { ThemeKey = fixedTheme.Key });
        return fixedTheme;
    }
}
