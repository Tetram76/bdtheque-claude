using Microsoft.Extensions.Options;

namespace Bdtheque.Api.Security;

/// <summary>
/// Rejette toute requête ne portant pas l'en-tête interne attendu. Ne remplace pas
/// l'authentification de l'utilisateur (portée par le cookie du conteneur `frontend`) :
/// protège uniquement contre un appel direct à `api` qui contournerait l'isolation réseau.
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
