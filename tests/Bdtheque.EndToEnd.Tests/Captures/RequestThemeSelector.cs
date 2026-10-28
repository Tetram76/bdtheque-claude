extern alias frontend;

using Microsoft.AspNetCore.Http;
using frontend::Bdtheque.Frontend.Theming;

namespace Bdtheque.EndToEnd.Tests.Captures;

/// <summary>
/// The theme a test fixes for its requests, by a header of its browser context, rather than the one the application
/// would choose: each screen is captured in each offered theme.
/// </summary>
public sealed class RequestThemeSelector(IHttpContextAccessor httpContextAccessor, IThemeSelector application) : IThemeSelector
{
    public const string HeaderName = "X-Test-Theme";

    public Theme Current =>
        httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString() is { Length: > 0 } key
            ? ThemeCatalog.Offered.Single(theme => theme.Key == key)
            : application.Current;
}
