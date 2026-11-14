using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Bdtheque.Frontend.Theming;

/// <summary>
/// The colours of the day of the page a request renders, set on its root element (Components/App.razor), read before the
/// page is rendered: the root never waits for them, which Blazor does not do for a page re-executed for its status code
/// (the page of an address that leads nowhere), sent as soon as rendered (choix-implementation/socle/themes.md, couleurs du
/// jour).
/// </summary>
public sealed class PageDayColors(IDayColorsSource source, IThemeSelector themes, BrowserWelcome welcome)
{
    /// <summary>The custom properties of the colours of the day; <c>null</c> for a theme not painted by them, or the welcome screen.</summary>
    public string? Style { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!welcome.IsDue && themes.Current.PaintedByTheDay)
            Style = DayPalette.Of(await source.GetAsync(cancellationToken)).Style;
    }

    /// <summary>Reads the colours of the day of each request that renders a page, the re-executed ones included.</summary>
    public static void ReadBeforeRendering(IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>() is not null)
            await context.RequestServices.GetRequiredService<PageDayColors>().LoadAsync(context.RequestAborted);
        await next(context);
    });
}
