using System.Net.Http.Json;
using System.Text.Json;

namespace Bdtheque.Frontend.Api;

/// <summary>
/// Client of <c>api</c>, over the internal Docker network with the shared secret (choix-implementation/socle.md
/// § Authentification : mise en œuvre), exchanging the contracts of <c>Bdtheque.Contracts</c>. Every call
/// that does not succeed throws an <see cref="ApiProblemException"/>, whatever the cause: an error the API
/// reported, or an API out of reach.
/// </summary>
public sealed class ApiClient(HttpClient http)
{
    /// <summary>Name of the HTTP client of the API, which the configuration of the application gives its address and secret.</summary>
    public const string HttpClientName = "Api";

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        ReadAsync<T>(new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest content, CancellationToken cancellationToken = default) =>
        ReadAsync<TResponse>(new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(content) }, cancellationToken);

    public Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest content, CancellationToken cancellationToken = default) =>
        ReadAsync<TResponse>(new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(content) }, cancellationToken);

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        (await SendAsync(new HttpRequestMessage(HttpMethod.Delete, path), cancellationToken)).Dispose();

    private async Task<T> ReadAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(request, cancellationToken);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new ApiProblemException(ApiProblem.Technical((int)response.StatusCode));
        }
        catch (JsonException exception)
        {
            throw new ApiProblemException(ApiProblem.Technical((int)response.StatusCode), exception);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        using (request)
        {
            try
            {
                response = await http.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new ApiProblemException(ApiProblem.Technical(), exception);
            }
            // The timeout of the client, not a cancellation by the caller, which keeps its own meaning.
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ApiProblemException(ApiProblem.Technical(), exception);
            }
        }

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
            throw new ApiProblemException(await ApiProblemReader.ReadAsync(response, cancellationToken));
    }
}
