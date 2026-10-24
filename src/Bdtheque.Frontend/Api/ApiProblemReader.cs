using System.Text.Json;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Frontend.Api;

/// <summary>
/// Reads the error of a failed response. Only a ProblemDetails of one of the three categories of
/// <see cref="ProblemTypes"/> is taken for what it says: anything else (another type, a body that is not
/// a ProblemDetails, an error page of a proxy) is a technical error, which no input of the user causes.
/// </summary>
internal static class ApiProblemReader
{
    private const string ProblemMediaType = "application/problem+json";

    public static async Task<ApiProblem> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        if (response.Content.Headers.ContentType?.MediaType != ProblemMediaType)
            return ApiProblem.Technical(status);

        try
        {
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var category = StringOf(root, "type") switch
            {
                ProblemTypes.Business => ProblemCategory.Business,
                ProblemTypes.Functional => ProblemCategory.Functional,
                _ => ProblemCategory.Technical,
            };
            if (category == ProblemCategory.Technical)
                return ApiProblem.Technical(status);

            var ruleCode = category == ProblemCategory.Business ? StringOf(root, ProblemTypes.RuleCodeExtension) : null;
            var impact = root.TryGetProperty(ProblemTypes.ImpactExtension, out var element) && element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<DeletionImpact>(JsonSerializerOptions.Web)
                : null;
            return new ApiProblem(category, status, ruleCode, impact);
        }
        catch (JsonException)
        {
            return ApiProblem.Technical(status);
        }
    }

    private static string? StringOf(JsonElement root, string property) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
