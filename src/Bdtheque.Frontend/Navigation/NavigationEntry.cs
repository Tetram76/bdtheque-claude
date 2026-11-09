namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// An entry of a menu: the key of its label in the texts of the menu, and the page it leads to. It is the current entry on
/// its page and on the pages under it (an album under the list of the albums), or on its page only when it is
/// <paramref name="Exact"/> (the home, under which every page is). An entry whose page is not implemented yet is never
/// current on its own path: every such entry leads to the same page; its <paramref name="Section"/>, the path its page will
/// have, makes it current on the pages under it already implemented (the record of an album, under the list of the albums).
/// </summary>
public sealed record NavigationEntry(string Name, string Path, bool Exact = false, string? Section = null)
{
    public bool IsCurrent(string currentPath)
    {
        var section = Section ?? Path;
        if (section == AppPaths.NotYetImplemented)
            return false;
        if (string.Equals(currentPath, section, StringComparison.OrdinalIgnoreCase))
            return Path != AppPaths.NotYetImplemented;
        return !Exact && currentPath.StartsWith(section.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
    }
}
