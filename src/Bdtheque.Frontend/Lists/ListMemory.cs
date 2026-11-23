using Bdtheque.Frontend.Security;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.WebUtilities;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// What a list of the consultation keeps for the visit (fonctionnel/structure-de-l-application.md, Retour à une liste): its
/// address as the user left it, criteria included. Found again from a record by its fil d'Ariane, the list is presented as
/// left; opened by the entry of the menu, it keeps its filters only, from the start.
/// </summary>
/// <remarks>
/// Each list has its session cookie, which carries the start of the visit with the address: the browser forgets it with its
/// session, as a visit ends, and a cookie of another visit (one restarted within the same session) is ignored. Like the cookie
/// of the visit, it is written once, when the response starts, and only for a request rendered statically, the lists being so.
/// </remarks>
public sealed class ListMemory(IHttpContextAccessor httpContextAccessor, VisitCookie visitCookie)
{
    private const string CookiePrefix = "Bdtheque.List.";
    private const char Separator = '|';

    // The criteria of an address that are no filters: what the entry of the menu forgets, with the panel of the filters.
    private static readonly HashSet<string> NotFilters = [ListQuery.TextName, ListQuery.EntryName, ListQuery.PageName, ListQuery.FiltersName];

    private readonly Dictionary<string, string> _kept = [];

    /// <summary>Keeps <paramref name="query"/>, as the list presents it, for the requests that follow in the visit.</summary>
    public void Keep(ListQuery query)
    {
        if (httpContextAccessor.HttpContext is not { Response.HasStarted: false } context || visitCookie.Current is not { } visit)
            return;

        var path = query.ListPath;
        var left = query.LeftHref();
        if (_kept.TryAdd(path, left))
            context.Response.OnStarting(() =>
            {
                context.Response.Cookies.Append(CookieName(path), $"{visit.Start:yyyy-MM-dd}{Separator}{_kept[path]}", FrontendCookies.For(context.Request));
                return Task.CompletedTask;
            });
        else
            _kept[path] = left;
    }

    /// <summary>The address of the list at <paramref name="path"/> as the user left it in the visit: that of the fil d'Ariane of a record.</summary>
    public string BackHref(string path) => Left(path) ?? path;

    /// <summary>
    /// The address of the list at <paramref name="path"/> with its filters only, from the start: that of the entry of the menu. On
    /// the list itself, its filters are those it presents, from the request. Any other page of the menu is left as it is.
    /// </summary>
    public string MenuHref(string path)
    {
        var query = PresentedQuery(path);
        if (string.IsNullOrEmpty(query))
            return path;

        var filters = QueryHelpers.ParseQuery(query)
            .Where(parameter => !NotFilters.Contains(parameter.Key))
            .SelectMany(parameter => parameter.Value.Select(value => (parameter.Key, Value: value ?? "")))
            .ToList();
        return filters.Count == 0 ? path : $"{path}?{string.Join('&', filters.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"))}";
    }

    // The query of the list at path as last presented: that of the request on the list itself, whose cookie is only written
    // once its response starts; otherwise that of the address the visit left it at, if any.
    private string? PresentedQuery(string path)
    {
        if (httpContextAccessor.HttpContext?.Request is { } request && request.Path.Value == path)
            return request.QueryString.Value;
        return Left(path)?[path.Length..];
    }

    // The address the visit left the list at, if any: only one of the list itself, kept in this very visit.
    private string? Left(string path)
    {
        if (_kept.TryGetValue(path, out var kept))
            return kept;
        if (httpContextAccessor.HttpContext?.Request.Cookies[CookieName(path)]?.Split(Separator, 2) is not [var start, var address]
            || visitCookie.Current is not { } visit || start != $"{visit.Start:yyyy-MM-dd}")
            return null;
        return address == path || address.StartsWith($"{path}?", StringComparison.Ordinal) ? address : null;
    }

    private static string CookieName(string path) => CookiePrefix + path.Trim('/');
}
