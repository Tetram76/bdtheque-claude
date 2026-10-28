namespace Bdtheque.Frontend.Theming;

/// <summary>Chooses the theme in which a request is presented.</summary>
public interface IThemeSelector
{
    Theme Current { get; }
}

/// <summary>
/// The first offered theme, the only one until the draw of the theme of the visit (#258) replaces this selector.
/// </summary>
public sealed class FirstOfferedThemeSelector : IThemeSelector
{
    public Theme Current => ThemeCatalog.Offered[0];
}
