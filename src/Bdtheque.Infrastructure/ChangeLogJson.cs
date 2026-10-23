using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Bdtheque.Domain.Entities;

namespace Bdtheque.Infrastructure;

/// <summary>
/// JSON form of the labels and changes of the change log, stored in <c>jsonb</c> columns
/// (choix-implementation/domaine.md § Journal des modifications : mise en œuvre).
/// </summary>
/// <remarks>
/// The change log is never purged, so this form must read back forever: the kinds below are never
/// renamed, and the members of the domain records, which name their properties, are pinned by
/// <c>ChangeLogJsonTests</c>. Declared here rather than by attributes on the domain records, which
/// know nothing of their persistence.
/// </remarks>
internal static class ChangeLogJson
{
    private const string KindProperty = "kind";

    private static readonly (Type Type, string Kind)[] ValueKinds =
    [
        (typeof(TextValue), "text"),
        (typeof(NumberValue), "number"),
        (typeof(FlagValue), "flag"),
        (typeof(DateValue), "date"),
        (typeof(PartialDateValue), "partialDate"),
        (typeof(AmountValue), "amount"),
        (typeof(EnumValue), "enum"),
        (typeof(ReferenceValue), "reference"),
        (typeof(ContributionValue), "contribution"),
        (typeof(VisualValue), "visual"),
        (typeof(SearchCriteriaValue), "searchCriteria"),
        (typeof(SearchVariablesValue), "searchVariables"),
    ];

    private static readonly (Type Type, string Kind)[] LabelKinds =
    [
        (typeof(AlbumLabel), "album"),
        (typeof(EditionLabel), "edition"),
        (typeof(PurchaseIntentLabel), "purchaseIntent"),
        (typeof(AuthorLabel), "author"),
        (typeof(PublisherCollectionLabel), "publisherCollection"),
        (typeof(NameLabel), "name"),
    ];

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Stored in the database, never embedded in a page: the texts keep their own characters.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // jsonb stores the keys of an object in its own order: the kind is not read back first.
        AllowOutOfOrderMetadataProperties = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { DeclareKinds } },
    };

    public static string SerializeLabel(RecordLabel label) => JsonSerializer.Serialize(label, Options);

    public static RecordLabel DeserializeLabel(string json) =>
        JsonSerializer.Deserialize<RecordLabel>(json, Options) ?? throw new JsonException("A change log entry has a label.");

    public static string SerializeChanges(IReadOnlyList<ChangeLogChange> changes) => JsonSerializer.Serialize(changes, Options);

    public static IReadOnlyList<ChangeLogChange> DeserializeChanges(string json) =>
        (JsonSerializer.Deserialize<List<ChangeLogChange>>(json, Options) ?? []).AsReadOnly();

    private static void DeclareKinds(JsonTypeInfo typeInfo)
    {
        var kinds = typeInfo.Type == typeof(ChangeLogValue) ? ValueKinds
            : typeInfo.Type == typeof(RecordLabel) ? LabelKinds
            : null;
        if (kinds is null)
            return;

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = KindProperty };
        foreach (var (type, kind) in kinds)
            typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(type, kind));
    }
}
