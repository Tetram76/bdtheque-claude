namespace Bdtheque.Contracts.Catalog;

/// <summary>The dashboard of the home page (fonctionnel/page-accueil.md § Page d'accueil), public.</summary>
/// <param name="Content">
/// <c>null</c> when the collection is empty, without any draw nor statistic: the home page then presents its
/// welcome screen instead of the dashboard.
/// </param>
public sealed record Dashboard(DashboardContent? Content);

/// <summary>The blocks of the dashboard, every statistic bearing on the collection only.</summary>
/// <param name="LatestEntries">The latest editions entered in the collection, from the most recent.</param>
/// <param name="AreValueIndicatorsComputable">
/// <c>false</c> until the amounts are converted into euros (Phase 4): the block of the value indicators is then
/// presented without any data, as the theme sees fit.
/// </param>
/// <param name="AreRandomStatisticsComputable">
/// <c>false</c> until the statistics drawn at random are computed (Phase 4): their blocks are then presented without
/// any data.
/// </param>
public sealed record DashboardContent(
    DashboardCounters Counters,
    SpotlightEdition Spotlight,
    IReadOnlyList<LatestEntry> LatestEntries,
    bool AreValueIndicatorsComputable,
    bool AreRandomStatisticsComputable);

/// <param name="AlbumCount">The albums of the collection, those with an owned edition.</param>
/// <param name="SeriesCount">The series of these albums.</param>
/// <param name="EditionCount">The owned editions.</param>
public sealed record DashboardCounters(int AlbumCount, int SeriesCount, int EditionCount);

/// <summary>The owned edition in the spotlight of the day, the same for every display of the day.</summary>
/// <param name="Cover">The first cover of the edition; <c>null</c> without any, the generic visual standing in for it.</param>
/// <param name="Summary">The summary of the album, which the theme may shorten.</param>
public sealed record SpotlightEdition(AlbumSummary Album, EditionSummary Edition, AlbumCover? Cover, string? Summary);

/// <summary>An edition entered in the collection.</summary>
/// <param name="Cover">The first cover of the edition; <c>null</c> without any.</param>
/// <param name="AcquisitionDate">When the user got the edition, if known.</param>
/// <param name="EnteredCollectionAt">When the edition became owned, always known.</param>
public sealed record LatestEntry(
    AlbumSummary Album, EditionSummary Edition, AlbumCover? Cover, DateOnly? AcquisitionDate, DateTimeOffset EnteredCollectionAt);
