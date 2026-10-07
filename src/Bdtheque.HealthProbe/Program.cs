// Health check of the published images (choix-implementation.md § Images de base des conteneurs):
// they ship neither a shell nor curl, so the container's HEALTHCHECK runs this probe instead, on the
// .NET runtime the image already carries. Exit code 0 when the URL answers with a success status,
// 1 otherwise — whatever the failure, the container is then reported unhealthy.
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
try
{
    using var response = await client.GetAsync(args[0]);
    return response.IsSuccessStatusCode ? 0 : 1;
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
{
    return 1;
}
