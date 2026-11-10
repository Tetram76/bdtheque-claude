using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>A series as its administration form edits it.</summary>
/// <param name="SortKey">
/// The sort key in force, computed from the title or entered by hand (see
/// <see cref="SeriesContent.ManualSortKey"/>): always shown by the form.
/// </param>
/// <param name="Version">Version of the series, sent back with any modification or deletion.</param>
/// <param name="References">The records the content references by their identifier, named for the form to present them.</param>
public sealed record SeriesForm(
    Guid Id, string SortKey, SeriesContent Content, uint Version, SeriesFormReferences References, DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

/// <summary>The records a <see cref="SeriesContent"/> references, named as the lists name them.</summary>
/// <param name="PublisherCollection">With its publisher, the one it belongs to.</param>
/// <param name="Universes">Each with its ancestors, a universe being presented with them (visuel/saisie-serie/contenu.md).</param>
/// <param name="Authors">The authors of its contributions, each once.</param>
public sealed record SeriesFormReferences(
    PublisherListItem? Publisher,
    PublisherCollectionListItem? PublisherCollection,
    IReadOnlyList<GenreListItem> Genres,
    IReadOnlyList<UniverseInHierarchy> Universes,
    IReadOnlyList<AuthorListItem> Authors);

/// <summary>
/// Everything the user edits on a series, sent to create it, and sent whole to modify it (the
/// form always sends the entire record).
/// </summary>
/// <param name="ManualSortKey">
/// The sort key entered by hand, which is then kept whatever the title becomes; <c>null</c> for the
/// automatic mode, where the sort key follows the title
/// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#calcul-et-stockage-de-la-clé-de-tri).
/// </param>
/// <param name="GenreIds">Genres of the series.</param>
/// <param name="UniverseIds">Universes of the series.</param>
/// <param name="Contributions">Template contributions, copied onto an album attached to the series.</param>
/// <param name="NewRecords">
/// The authors, genres and universes the form added on the spot, created with the series; none when the series is read.
/// </param>
public sealed record SeriesContent(
    string Title,
    string? ManualSortKey,
    SeriesStatus? Status,
    int? TheoreticalVolumeCount,
    bool IsComplete,
    bool ExcludeFromMissingVolumes,
    bool ExcludeFromReleaseEstimates,
    string? Summary,
    string? PersonalNotes,
    SeriesEditionTemplate EditionTemplate,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<Guid> UniverseIds,
    IReadOnlyList<ContributionContent> Contributions,
    NewRecords? NewRecords = null);

/// <summary>
/// Default values of a new edition of an album of the series
/// (fonctionnel/regles-metier/initialisation-d-une-nouvelle-edition-depuis-la-serie.md).
/// </summary>
/// <param name="PublisherCollectionId">Requires <paramref name="PublisherId"/>, and must belong to that publisher.</param>
public sealed record SeriesEditionTemplate(
    Guid? PublisherId,
    Guid? PublisherCollectionId,
    EditionCategory? Category,
    EditionCondition? Condition,
    BindingType? Binding,
    BookOrientation? Orientation,
    ReadingDirection? ReadingDirection,
    EditionFormat? Format,
    bool? IsColor);

/// <param name="Version">Version of the series the form was read at.</param>
public sealed record UpdateSeriesRequest(SeriesContent Content, uint Version);
