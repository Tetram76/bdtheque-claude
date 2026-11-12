namespace Bdtheque.EndToEnd.Tests;

public static class RouteExtensions
{
    /// <summary>
    /// The response of the server to the request of the route, as the browser would have sent it. A request intercepted by
    /// a route does not carry yet the metadata of a navigation (<c>Sec-Fetch-Mode</c>, <c>Sec-Fetch-Dest</c>), which the
    /// network stack of Chromium adds after it (observed: without them, the server took the navigation of a browser for that
    /// of a client that is not one, and never answered it with its welcome screen, Visits/BrowserWelcome.cs): the navigation
    /// of the main frame to a document gets them back here.
    /// </summary>
    public static async Task<IAPIResponse> FetchAsSentAsync(this IRoute route)
    {
        var headers = new Dictionary<string, string>(await route.Request.AllHeadersAsync());
        if (route.Request.IsNavigationRequest && route.Request.ResourceType == "document" && route.Request.Frame.ParentFrame is null)
        {
            headers["sec-fetch-mode"] = "navigate";
            headers["sec-fetch-dest"] = "document";
        }
        return await route.FetchAsync(new RouteFetchOptions { Headers = headers });
    }
}
