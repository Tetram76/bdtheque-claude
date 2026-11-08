using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the series of the administration (visuel/listes/liste-par-liste.md), which its address carries
/// (<see cref="AdminListQuery"/>): its search, on the title of the series, and its cross filters, those of the list of the
/// consultation.
/// </summary>
public sealed record SeriesListQuery(
    string? Text = null,
    string? Entry = null,
    Guid? AuthorId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    Guid? GenreId = null,
    Guid? UniverseId = null,
    int Page = 1,
    bool FiltersOpen = false,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null) : AdminListQuery(Text, Entry, Page, FiltersOpen, Delete, Merge, MergeText)
{
    internal const string AuthorName = "authorId";
    internal const string PublisherName = "publisherId";
    internal const string PublisherCollectionName = "publisherCollectionId";
    internal const string GenreName = "genreId";
    internal const string UniverseName = "universeId";

    private static readonly ListFunctions SeriesFunctions = new(
        "series", AudienceFunctions.AdminSeriesSearch, AudienceFunctions.AdminSeriesFilter, AudienceFunctions.AdminSeriesInitial,
        AudienceFunctions.AdminSeriesPage, AudienceFunctions.AdminSeriesDeletion, AudienceFunctions.AdminSeriesMerge);

    protected override string Path => AppPaths.AdminSeries;

    protected override string ApiRoute => "/admin/series";

    protected override ListFunctions Functions => SeriesFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (AuthorName, AuthorId), (PublisherName, PublisherId), (PublisherCollectionName, PublisherCollectionId), (GenreName, GenreId),
        (UniverseName, UniverseId),
    ];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public SeriesListQuery WithoutFilters() => new(Text, Entry);

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static SeriesListQuery Parse(string query)
    {
        var address = new Address(query);
        return new SeriesListQuery(
            address.Search,
            address.Entry,
            address.Id(AuthorName),
            address.Id(PublisherName),
            address.Id(PublisherCollectionName),
            address.Id(GenreName),
            address.Id(UniverseName),
            address.Page,
            address.FiltersOpen,
            address.Delete,
            address.Merge,
            address.MergeText);
    }
}
