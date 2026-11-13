using System.Net;
using System.Text.Json;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;

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

    /// <summary>Asserts that <paramref name="response"/> is a business error reporting <paramref name="ruleCode"/>.</summary>
    public static async Task<JsonElement> IsBusinessProblemAsync(HttpResponseMessage response, string ruleCode)
    {
        var problem = await IsProblemAsync(response, HttpStatusCode.UnprocessableContent, ProblemTypes.Business);
        Assert.Equal(ruleCode, problem.GetProperty(ProblemTypes.RuleCodeExtension).GetString());
        return problem;
    }

    /// <summary>
    /// Asserts that <paramref name="response"/> is a business error reporting <paramref name="ruleCode"/> that designates
    /// exactly <paramref name="fields"/>, in the content the request sent
    /// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
    /// </summary>
    /// <remarks>
    /// <paramref name="related"/>: the fields holding the values those to correct clash with, entered in the same content.
    /// </remarks>
    public static async Task<JsonElement> IsBusinessProblemAsync(
        HttpResponseMessage response, string ruleCode, string[] fields, string[]? related = null)
    {
        var problem = await IsBusinessProblemAsync(response, ruleCode);
        Assert.Equal(fields, FieldsOf(problem));
        Assert.Equal(related ?? [], FieldsOf(problem, ProblemTypes.RelatedFieldsExtension));
        return problem;
    }

    /// <summary>The fields a business problem designates (or the related ones); none when it carries none.</summary>
    public static string[] FieldsOf(JsonElement problem, string extension = ProblemTypes.FieldsExtension) =>
        problem.TryGetProperty(extension, out var fields)
            ? [.. fields.EnumerateArray().Select(field => field.GetString()!)]
            : [];

    /// <summary>The deletion impact a problem carries.</summary>
    public static DeletionImpact ImpactOf(JsonElement problem) =>
        problem.GetProperty(ProblemTypes.ImpactExtension).Deserialize<DeletionImpact>(JsonSerializerOptions.Web)!;

    /// <summary>Compares two impacts by value (a record compares its lists by reference).</summary>
    public static void SameImpact(DeletionImpact expected, DeletionImpact actual)
    {
        Assert.Equal(expected.BlockedBy, actual.BlockedBy);
        Assert.Equal(expected.AssociationsRemoved, actual.AssociationsRemoved);
        Assert.Equal(expected.DeletedWith, actual.DeletedWith);
        Assert.Equal(expected.LeavingCollection, actual.LeavingCollection);
        Assert.Equal(expected.Fingerprint, actual.Fingerprint);
    }
}
