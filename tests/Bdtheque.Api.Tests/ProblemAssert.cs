using System.Net;
using System.Text.Json;

namespace Bdtheque.Api.Tests;

/// <summary>Assertions on the error responses of the API (ProblemDetails, RFC 9457).</summary>
internal static class ProblemAssert
{
    /// <summary>
    /// Asserts that <paramref name="response"/> is a ProblemDetails of the given category and HTTP
    /// status, and returns its body for further assertions (rule code, etc.).
    /// </summary>
    public static async Task<JsonElement> IsProblemAsync(HttpResponseMessage response, HttpStatusCode status, string category)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        Assert.Equal(category, problem.GetProperty("type").GetString());
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        return problem;
    }
}
