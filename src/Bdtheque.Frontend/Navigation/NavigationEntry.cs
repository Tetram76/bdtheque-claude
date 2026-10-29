namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// An entry of a menu: the key of its label in the texts of the menu, and the page it leads to. It is the current entry on
/// its page and on the pages under it (an album under the list of the albums), or on its page only when it is
/// <paramref name="Exact"/> (the home, under which every page is). An entry whose page is not implemented yet is never
/// current: every such entry leads to the same page.
/// </summary>
public sealed record NavigationEntry(string Name, string Path, bool Exact = false)
{
    public bool IsCurrent(string currentPath)
    {
        if (Path == AppPaths.NotYetImplemented)
            return false;
        if (string.Equals(currentPath, Path, StringComparison.OrdinalIgnoreCase))
            return true;
        return !Exact && currentPath.StartsWith(Path.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
    }
}
