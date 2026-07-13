using Microsoft.Extensions.Options;

namespace Bdtheque.Api.Security;

/// <summary>
/// Rejects any request missing the expected internal header. Does not replace user
/// authentication (owned by the `frontend` container's cookie): it only protects against
/// a direct call to `api` bypassing network isolation.
/// </summary>
public sealed class InternalApiKeyMiddleware(RequestDelegate next, IOptions<InternalApiKeyOptions> options)
{
    /// <summary>
    /// Paths exempted from the shared secret: `/health` is polled by orchestrators, while
    /// `/openapi` and `/scalar` are only ever mapped in Development (see Program.cs) and
    /// must stay reachable from a plain browser to be usable as interactive documentation.
    /// </summary>
    private static readonly string[] ExemptPathPrefixes = ["/health", "/openapi", "/scalar"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (ExemptPathPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            await next(context);
            return;
        }

        var providedKey = context.Request.Headers[InternalApiKeyOptions.HeaderName].ToString();

        if (string.IsNullOrEmpty(providedKey) || providedKey != options.Value.Key)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
