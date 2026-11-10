using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Theming;

/// <summary>Chooses the theme in which a request is presented.</summary>
public interface IThemeSelector
{
    Theme Current { get; }
}

/// <summary>
/// The theme of the visit of the browser (<see cref="VisitTheme"/>), read from its cookie, which carries the visit on
/// when the request continues it otherwise (new visit, new date, theme drawn again). Chosen while the page is rendered
/// statically, by its root component, before the response starts (choix-implementation/socle/themes.md): an interactive
/// circuit has no request, and never asks. The page, rendered after its root component, finds
/// the visit continued (the audience measurement records its usages in it).
/// </summary>
public sealed class VisitThemeSelector(VisitCookie visitCookie, BrowserTimeZone timeZone, TimeProvider clock) : IThemeSelector
{
    private Theme? _current;

    public Theme Current => _current ??= Select();

    private Theme Select()
    {
        var continued = VisitTheme.Continue(
            visitCookie.Current, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), timeZone.Today(clock), ThemeCatalog.Offered, Random.Shared.Next);
        visitCookie.Set(continued);
        return VisitTheme.Of(continued, ThemeCatalog.Offered);
    }
}
