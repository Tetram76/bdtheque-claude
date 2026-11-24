using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the ISBN to check (visuel/listes/liste-par-liste.md), which its address carries: those of the list
/// of the editions, its search on the ISBN, the navigation by initial of its albums and its cross filters, and the state of the
/// errors it presents, those to correct by default. No dialog opens over it: its actions are written at once from their row.
/// </summary>
/// <param name="State">The errors presented: <c>null</c> for those to correct, the default, which is no filter.</param>
public sealed record InvalidIsbnListQuery(
    string? Text = null,
    string? Entry = null,
    InvalidIsbnState? State = null,
    Guid? SeriesId = null,
    Guid? AlbumId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    int Page = 1,
    bool FiltersOpen = false) : ListQuery(Text, Entry, Page, FiltersOpen)
{
    internal const string StateName = "state";

    private static readonly ListCriteriaFunctions Functions = new(
        AudienceFunctions.AdminInvalidIsbnsSearch, AudienceFunctions.AdminInvalidIsbnsFilter, AudienceFunctions.AdminInvalidIsbnsInitial,
        AudienceFunctions.AdminInvalidIsbnsPage);

    protected override string Path => AppPaths.AdminInvalidIsbns;

    protected override string ApiRoute => "/admin/editions/invalid-isbns";

    protected override ListCriteriaFunctions CriteriaFunctions => Functions;

    protected override IEnumerable<(string Name, object? Value)> Filters =>
    [
        (StateName, State is null or InvalidIsbnState.ToCorrect ? null : State.ToString()),
        (EditionListQuery.SeriesName, SeriesId), (EditionListQuery.AlbumName, AlbumId), (EditionListQuery.PublisherName, PublisherId),
        (EditionListQuery.PublisherCollectionName, PublisherCollectionId),
    ];

    /// <summary>The state the list presents: the errors to correct unless another is chosen.</summary>
    public InvalidIsbnState ShownState => State ?? InvalidIsbnState.ToCorrect;

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public InvalidIsbnListQuery WithoutFilters() => new(Text, Entry);

    /// <summary>The criteria of the list read from its address.</summary>
    public static InvalidIsbnListQuery Parse(string query)
    {
        var address = new Address(query);
        return new InvalidIsbnListQuery(
            address.Search,
            address.Entry,
            address.Enum<InvalidIsbnState>(StateName) is { } state and not InvalidIsbnState.ToCorrect ? state : null,
            address.Id(EditionListQuery.SeriesName),
            address.Id(EditionListQuery.AlbumName),
            address.Id(EditionListQuery.PublisherName),
            address.Id(EditionListQuery.PublisherCollectionName),
            address.Page,
            address.FiltersOpen);
    }
}
