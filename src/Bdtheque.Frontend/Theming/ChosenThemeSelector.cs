using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Theming;

/// <summary>
/// The theme chosen by the address of the page (<c>?theme=</c> and the key of an offered theme), in the functional
/// validation environment only (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md): the user
/// judges each theme without starting visits until it is drawn. The chosen theme becomes the theme of the visit, which
/// its cookie keeps for the following pages; without a theme offered in the address, the theme of the visit stays.
/// </summary>
public sealed class ChosenThemeSelector(IHttpContextAccessor httpContextAccessor, VisitCookie visitCookie, VisitThemeSelector visit)
    : IThemeSelector
{
    public const string QueryParameter = "theme";

    private Theme? _current;

    public Theme Current => _current ??= Select();

    private Theme Select()
    {
        var drawn = visit.Current;
        var key = httpContextAccessor.HttpContext?.Request.Query[QueryParameter].ToString();
        if (ThemeCatalog.Offered.SingleOrDefault(theme => theme.Key == key) is not { } chosen || visitCookie.Current is not { } current)
            return drawn;

        visitCookie.Set(current with { ThemeKey = chosen.Key });
        return chosen;
    }
}
