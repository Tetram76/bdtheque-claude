using Bdtheque.Frontend.Security;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Api;

/// <summary>
/// Adds the secret shared with <c>api</c> to every request sent to it, by the API client as by the relay of the media
/// (choix-implementation/socle.md § Authentification : mise en œuvre). A relayed request is the browser's: whatever value
/// of this header it carries is replaced, never passed on.
/// </summary>
public sealed class InternalApiKeyHandler(IOptions<InternalApiKeyOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(InternalApiKeyOptions.HeaderName);
        request.Headers.Add(InternalApiKeyOptions.HeaderName, options.Value.Key);
        return base.SendAsync(request, cancellationToken);
    }
}
