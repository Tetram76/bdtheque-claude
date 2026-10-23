using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Errors;

/// <summary>
/// Business rule enforced by each unique index of the schema, by index name. A uniqueness rule
/// spans several rows, which the domain cannot see: the database enforces it, and its violation is
/// a business error the user can fix (e.g. a genre label already used) — never a technical one.
/// </summary>
/// <remarks>
/// Every unique index of the model must appear here (pinned by <c>UniqueIndexRulesTests</c>): one
/// missing would turn a user mistake into a technical error. Only those of the tables the application alone
/// writes, without any input, are declared apart, in <see cref="OutsideUserInput"/>.
/// </remarks>
internal static class UniqueIndexRules
{
    public static readonly IReadOnlyDictionary<string, string> ByIndexName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["IX_Genres_Label"] = DomainRules.GenreLabelAlreadyUsed,
        ["IX_Publishers_Name"] = DomainRules.PublisherNameAlreadyUsed,
        ["IX_PublisherCollections_PublisherId_Name"] = DomainRules.PublisherCollectionNameAlreadyUsed,
        ["IX_Contributions_AlbumId_Role_AuthorId"] = DomainRules.ContributionAlreadyCredited,
        ["IX_Contributions_SeriesId_Role_AuthorId"] = DomainRules.ContributionAlreadyCredited,
        ["IX_PurchaseIntents_AlbumId_WholeAlbum"] = DomainRules.PurchaseIntentAlbumAlreadyTargeted,
        ["IX_PurchaseIntents_EditionId"] = DomainRules.PurchaseIntentEditionAlreadyTargeted,
        ["IX_SavedSearches_Name"] = DomainRules.SavedSearchNameAlreadyUsed,
    };

    /// <summary>
    /// Unique indexes no input of the user can break, their table being written by the application alone: a
    /// violation reveals a programming error, and stays a technical error.
    /// </summary>
    public static readonly IReadOnlySet<string> OutsideUserInput = new HashSet<string>(StringComparer.Ordinal)
    {
        // The audience counters, established once per month by the retention of the audience measurement.
        "IX_AudienceCounters_FunctionMonthCompartment",
    };
}
