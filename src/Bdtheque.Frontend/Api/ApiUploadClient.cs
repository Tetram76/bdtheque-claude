namespace Bdtheque.Frontend.Api;

/// <summary>
/// Client of <c>api</c> for the uploads of visuals, streamed from the browser to <c>api</c> through the circuit
/// (choix-implementation/domaine/visuels-stockage-et-traitement.md): the same connection and secret as <see cref="ApiClient"/>,
/// without its fixed timeout, which would cut the upload of a heavy scan over a slow connection. The upload is cancelled with the
/// form that sends it.
/// </summary>
public sealed class ApiUploadClient(HttpClient http)
{
    /// <summary>Name of the HTTP client of the uploads.</summary>
    public const string HttpClientName = "ApiUploads";

    private readonly ApiClient _api = new(http);

    /// <summary>Posts <paramref name="content"/> to <paramref name="path"/>, and reads the contract answered.</summary>
    public Task<TResponse> PostAsync<TResponse>(string path, HttpContent content, CancellationToken cancellationToken) =>
        _api.PostAsync<TResponse>(path, content, cancellationToken);
}
