using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>A series as its administration form edits it.</summary>
/// <param name="SortKey">
/// The sort key in force, computed from the title or entered by hand (see
/// <see cref="SeriesContent.ManualSortKey"/>): always shown by the form.
/// </param>
/// <param name="Version">Version of the series, sent back with any modification or deletion.</param>
public sealed record SeriesForm(Guid Id, string SortKey, SeriesContent Content, uint Version);

/// <summary>
/// Everything the user edits on a series, sent to create it, and sent whole to modify it (the
/// form always sends the entire record).
/// </summary>
/// <param name="ManualSortKey">
/// The sort key entered by hand, which is then kept whatever the title becomes; <c>null</c> for the
/// automatic mode, where the sort key follows the title (fonctionnel.md § Calcul et stockage de la
/// clé de tri).
/// </param>
/// <param name="GenreIds">Genres of the series.</param>
/// <param name="UniverseIds">Universes of the series.</param>
/// <param name="Contributions">Template contributions, copied onto an album attached to the series.</param>
public sealed record SeriesContent(
    string Title,
    string? ManualSortKey,
    SeriesStatus? Status,
    int? TheoreticalVolumeCount,
    bool IsComplete,
    bool ExcludeFromMissingVolumes,
    string? Summary,
    string? PersonalNotes,
    SeriesEditionTemplate EditionTemplate,
    IReadOnlyList<Guid> GenreIds,
    IReadOnlyList<Guid> UniverseIds,
    IReadOnlyList<SeriesContribution> Contributions);

/// <summary>
/// Default values of a new edition of an album of the series (fonctionnel.md § Initialisation d'une
/// nouvelle édition depuis la série).
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

/// <summary>An author credited with a role on the template of a series.</summary>
public sealed record SeriesContribution(Guid AuthorId, ContributionRole Role);

/// <param name="Version">Version of the series the form was read at.</param>
public sealed record UpdateSeriesRequest(SeriesContent Content, uint Version);
