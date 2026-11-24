using System.Text.Json.Serialization;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

// The merge of two records of the same type (fonctionnel/regles-metier/fusion-des-fiches.md, visuel/fusion/fusion-de-deux-fiches.md):
// what the screen presents of both, and what it sends back, data by data, of the one or the other.

/// <summary>Which of the two merged records a data of the merged record comes from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MergeSide>))]
public enum MergeSide
{
    First,
    Second,
}

/// <summary>A merged record, at the version its data were read, as the merge sends it back.</summary>
public sealed record MergedRecord(Guid Id, uint Version);

/// <summary>
/// What a type of records linked to the merged records counts, as the screen says it passes to the record
/// (« Crédité sur 26 albums — les 24 de Gotlib et les 3 de Marcel Gotlib, dont 1 commun aux deux fiches »).
/// </summary>
/// <param name="Kind">The type of the linked records.</param>
/// <param name="Total">The records linked to the merged record, each once.</param>
/// <param name="First">Those linked to the first record.</param>
/// <param name="Second">Those linked to the second record.</param>
/// <param name="Common">Those linked to both, counted once in <paramref name="Total"/>.</param>
public sealed record MergeLinkCount(EntityKind Kind, int Total, int First, int Second, int Common);

/// <summary>The record the merge leaves, the other one being deleted.</summary>
public sealed record MergeResult(Guid Id);

/// <summary>A genre as its merge presents it.</summary>
public sealed record GenreMergeRecord(Guid Id, string Label, DateTimeOffset ModifiedAt, uint Version);

/// <summary>Two genres to merge, and the series and albums classed under them, in the order of the entity types.</summary>
public sealed record GenreMergePreview(GenreMergeRecord First, GenreMergeRecord Second, IReadOnlyList<MergeLinkCount> Links);

/// <summary>The merge of two genres: the label kept, of the one or the other.</summary>
public sealed record GenreMergeRequest(MergedRecord First, MergedRecord Second, MergeSide Label);

/// <summary>An author as its merge presents it.</summary>
public sealed record AuthorMergeRecord(
    Guid Id, string? LastName, string? FirstName, string? Pseudonym, string? Nationality, string? Biography, DateTimeOffset ModifiedAt,
    uint Version);

/// <summary>Two authors to merge, and the series and albums they are credited on, in the order of the entity types.</summary>
public sealed record AuthorMergePreview(AuthorMergeRecord First, AuthorMergeRecord Second, IReadOnlyList<MergeLinkCount> Links);

/// <summary>The merge of two authors: each data kept, of the one or the other.</summary>
public sealed record AuthorMergeRequest(
    MergedRecord First, MergedRecord Second, MergeSide LastName, MergeSide FirstName, MergeSide Pseudonym, MergeSide Nationality,
    MergeSide Biography);

/// <summary>
/// The identity of an author as the domain derives it from the names (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes),
/// which the merge of two authors presents for the names chosen.
/// </summary>
public sealed record AuthorIdentity(string DisplayName, string SortKey, string NavigationEntry);
