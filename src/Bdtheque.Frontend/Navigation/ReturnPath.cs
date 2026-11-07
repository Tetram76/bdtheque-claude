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

    /// <summary>
    /// The page to return to once signed in, which the address of the login carries: a path of the application only, the
    /// home of the administration otherwise, so that the login never becomes a way out of the application. A path that
    /// starts with two slashes, or a slash and a backslash, is read by the browser as another site, and is refused. The
    /// path is then resolved against the address of the application, as the redirection would resolve it, and the address
    /// followed is that one, once checked to be of the application: never the text received, which the browser could still
    /// read otherwise.
    /// </summary>
    public static string AfterSignIn(string? returnUrl, Uri application) =>
        returnUrl is { Length: > 0 } path && path[0] == '/' && (path.Length == 1 || path[1] is not ('/' or '\\'))
        && Uri.TryCreate(application, path, out var target) && application.IsBaseOf(target)
            ? target.AbsoluteUri
            : AppPaths.AdminHome;
}
