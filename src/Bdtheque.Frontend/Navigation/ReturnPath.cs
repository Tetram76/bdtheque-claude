namespace Bdtheque.Frontend.Navigation;

/// <summary>Page to return to from the current one: the page that led to it, read from the request.</summary>
public static class ReturnPath
{
    /// <summary>
    /// The referring page when it belongs to the application, home otherwise. Only a page of the same
    /// origin is followed, so that a link from another site never turns the return link into a way out
    /// of the application; the current page itself (reloaded) is no page to return to.
    /// </summary>
    public static string FromReferer(string? referer, Uri currentPage)
    {
        if (Uri.TryCreate(referer, UriKind.Absolute, out var source)
            && Uri.Compare(source, currentPage, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0
            && !string.Equals(source.AbsolutePath, currentPage.AbsolutePath, StringComparison.OrdinalIgnoreCase))
            return source.PathAndQuery;

        return AppPaths.Home;
    }
}
