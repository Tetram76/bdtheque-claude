using System.Text.Json.Serialization;
using Bdtheque.Contracts.Catalog;
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

/// <summary>
/// An element of a list proper to the merged records (a genre, a universe, a contribution), held by one of them only, and the
/// record whose value its row retains (fonctionnel/regles-metier/fusion-des-fiches.md): the record holding it keeps it, the other
/// one, whose value is missing, discards it. An element both records hold is kept, with nothing to choose.
/// </summary>
public sealed record MergeElement(Guid Id, MergeSide Side);

/// <summary>A template contribution of a series, as its merge presents it: its author, credited in its role.</summary>
public sealed record SeriesMergeContribution(Guid Id, AuthorListItem Author, ContributionRole Role);

/// <summary>A series as its merge presents it: what its form edits, and the records it references, named.</summary>
/// <param name="ManualSortKey">The sort key entered by hand, <c>null</c> when it follows the title.</param>
/// <param name="PublisherCollection">With its publisher, the one it belongs to.</param>
/// <param name="Universes">Each with its ancestors, a universe being presented with them.</param>
public sealed record SeriesMergeRecord(
    Guid Id, string Title, string? ManualSortKey, SeriesStatus? Status, int? TheoreticalVolumeCount, bool IsComplete,
    bool ExcludeFromMissingVolumes, bool ExcludeFromReleaseEstimates, string? Summary, string? PersonalNotes,
    SeriesEditionTemplate EditionTemplate, PublisherListItem? Publisher, PublisherCollectionListItem? PublisherCollection,
    IReadOnlyList<GenreListItem> Genres, IReadOnlyList<UniverseInHierarchy> Universes, IReadOnlyList<SeriesMergeContribution> Contributions,
    DateTimeOffset ModifiedAt, uint Version);

/// <summary>Two series to merge, and their albums, in the order of the entity types.</summary>
public sealed record SeriesMergePreview(SeriesMergeRecord First, SeriesMergeRecord Second, IReadOnlyList<MergeLinkCount> Links);

/// <summary>
/// The merge of two series: each data kept, of the one or the other, the publisher and its collection together, of the same
/// series (modele-metier/relations-et-cardinalites.md); and each genre, universe and contribution held by one series only,
/// retained or discarded.
/// </summary>
/// <param name="SortKey">The sort key, entered by hand or following the title kept.</param>
/// <param name="Publisher">The publisher and the collection of the template, together.</param>
public sealed record SeriesMergeRequest(
    MergedRecord First, MergedRecord Second, MergeSide Title, MergeSide SortKey, MergeSide Status, MergeSide TheoreticalVolumeCount,
    MergeSide IsComplete, MergeSide ExcludeFromMissingVolumes, MergeSide ExcludeFromReleaseEstimates, MergeSide Summary,
    MergeSide PersonalNotes, MergeSide Publisher, MergeSide Category, MergeSide Condition, MergeSide Binding, MergeSide Format,
    MergeSide Orientation, MergeSide ReadingDirection, MergeSide IsColor, IReadOnlyList<MergeElement> Genres,
    IReadOnlyList<MergeElement> Universes, IReadOnlyList<MergeElement> Contributions);
