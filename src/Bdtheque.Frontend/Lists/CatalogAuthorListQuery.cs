using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the authors of the consultation, which its address carries (<see cref="ListQuery"/>): its search,
/// on the last name, the first name and the pseudonym, and the possession, its only filter, by which it limits itself to the
/// collection, an author taking the state of the albums it is credited on
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
public sealed record CatalogAuthorListQuery(
    string? Text = null,
    string? Entry = null,
    AlbumCollectionState? State = null,
    int Page = 1,
    bool FiltersOpen = false) : ListQuery(Text, Entry, Page, FiltersOpen)
{
    // The name of the filter in the address, that of the API, as in the lists of the albums.
    internal const string StateName = AlbumListQuery.StateName;

    private static readonly ListCriteriaFunctions AuthorFunctions = new(
        AudienceFunctions.AuthorsSearch, AudienceFunctions.AuthorsFilter, AudienceFunctions.AuthorsInitial, AudienceFunctions.AuthorsPage);

    /// <summary>The whole list, as the menu of the consultation opens it.</summary>
    public static readonly CatalogAuthorListQuery All = new();

    protected override string Path => AppPaths.Authors;

    protected override string ApiRoute => "/catalog/authors";

    protected override ListCriteriaFunctions CriteriaFunctions => AuthorFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [(StateName, State)];

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public CatalogAuthorListQuery WithoutFilters() => new(Text, Entry);

    /// <inheritdoc cref="ListQuery.Address"/>
    public static CatalogAuthorListQuery Parse(string query)
    {
        var address = new Address(query);
        return new CatalogAuthorListQuery(address.Search, address.Entry, address.Enum<AlbumCollectionState>(StateName), address.Page, address.FiltersOpen);
    }
}
