using Microsoft.Extensions.Options;

namespace Bdtheque.Api.Security;

/// <summary>
/// Rejects any request missing the expected internal header. Does not replace user
/// authentication (owned by the `frontend` container's cookie): it only protects against
/// a direct call to `api` bypassing network isolation.
/// </summary>
public sealed class InternalApiKeyMiddleware(RequestDelegate next, IOptions<InternalApiKeyOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
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
