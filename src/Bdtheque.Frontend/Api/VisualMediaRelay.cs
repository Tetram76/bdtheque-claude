using System.Net;
using Bdtheque.Contracts.Catalog;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;

namespace Bdtheque.Frontend.Api;

/// <summary>
/// Relays the media of the visuals that <c>api</c> serves (<see cref="VisualMediaRoutes"/>), under the same paths: the
/// browser only talks to <c>frontend</c>, and the volume is private to <c>api</c> (choix-implementation/domaine.md § Visuels :
/// stockage et traitement). Streamed, never held in memory, and with the cache headers of <c>api</c>, which alone knows
/// the rule that justifies them.
/// </summary>
public static class VisualMediaRelay
{
    private static readonly ForwarderRequestConfig RequestConfig = new() { ActivityTimeout = TimeSpan.FromSeconds(100) };
    private static readonly HttpTransformer Transformer = new MediaRequestTransformer();

    /// <summary>
    /// The handler of the connection to <c>api</c>, set as YARP recommends for a forwarder: neither proxy, redirection,
    /// decompression nor cookies, which would alter what is relayed. The API client uses it too.
    /// </summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(15),
    };

    public static void MapVisualMediaRelay(this IEndpointRouteBuilder app)
    {
        // Only these two routes are relayed, those of VisualMediaRoutes: frontend is no open proxy to api.
        var visual = app.MapGroup("/catalog/visuals/{id:guid}");
        visual.MapGet("/display", RelayAsync);
        visual.MapGet("/original", RelayAsync);
    }

    private static async Task RelayAsync(
        HttpContext context, IHttpForwarder forwarder, IHttpMessageHandlerFactory handlers, IOptions<ApiOptions> api,
        ILoggerFactory loggers)
    {
        // The handler of the API client, with the internal key: created by the factory, which owns and recycles it.
        using var invoker = new HttpMessageInvoker(handlers.CreateHandler(ApiClient.HttpClientName), disposeHandler: false);
        var error = await forwarder.SendAsync(context, api.Value.BaseUrl, invoker, RequestConfig, Transformer, context.RequestAborted);
        if (error != ForwarderError.None)
        {
            loggers.CreateLogger(typeof(VisualMediaRelay)).LogError(context.GetForwarderErrorFeature()?.Exception,
                "The media {Path} could not be relayed from the API: {Error}.", context.Request.Path, error);
        }
    }

    private sealed class MediaRequestTransformer : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(
            HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);
            // The cookies of the visitor (authentication, time zone…) are frontend's: api has no knowledge of the user.
            proxyRequest.Headers.Remove(HeaderNames.Cookie);
            proxyRequest.Headers.Host = null;
        }
    }
}
