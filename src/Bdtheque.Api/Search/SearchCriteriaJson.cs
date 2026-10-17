using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bdtheque.Contracts.Search;

namespace Bdtheque.Api.Search;

/// <summary>
/// The JSON form in which a saved search keeps its criteria: that of the advanced search contract
/// (choix-implementation.md § Recherche avancée : mise en œuvre), into which its variables point.
/// </summary>
/// <remarks>
/// Stored by the saved searches and by the change log, never purged: the form must read back forever, so
/// the members of the criteria contract and their discriminators are pinned by <c>SearchCriteriaJsonTests</c>.
/// </remarks>
internal static class SearchCriteriaJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Stored in the database, never embedded in a page: the texts keep their own characters.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowOutOfOrderMetadataProperties = true,
    };

    public static string Serialize(SearchCondition criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        return JsonSerializer.Serialize(criteria, Options);
    }

    /// <exception cref="ArgumentException">Not criteria: a technical error, the criteria being written by the API.</exception>
    public static SearchCondition Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SearchCondition>(json, Options)
                   ?? throw new ArgumentException("Criteria are a condition.", nameof(json));
        }
        // A condition without its kind cannot be built, its type being abstract.
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new ArgumentException("Not the JSON form of criteria.", nameof(json), exception);
        }
    }
}
