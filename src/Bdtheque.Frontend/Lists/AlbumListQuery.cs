using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Navigation;
using Bdtheque.Frontend.Navigation;
using Microsoft.AspNetCore.WebUtilities;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the albums of the administration (visuel/liste-albums/contenu.md), which its address
/// carries, the page being rendered statically: every link and form of the list leads to the list with other criteria.
/// </summary>
/// <param name="Text">The search, on the title of the album and on that of its series.</param>
/// <param name="Entry">The entry of the navigation by initial; <c>null</c> for « Toutes ».</param>
/// <param name="Page">The page of the list, from 1.</param>
/// <param name="FiltersOpen">Whether the panel of the filters is open, folded by default.</param>
/// <param name="Delete">The album whose deletion dialog is open.</param>
/// <param name="Merge">The album whose merge dialog is open.</param>
/// <param name="MergeText">The search of the other album, in the merge dialog.</param>
public sealed record AlbumListQuery(
    string? Text = null,
    string? Entry = null,
    Guid? SeriesId = null,
    Guid? AuthorId = null,
    Guid? PublisherId = null,
    Guid? PublisherCollectionId = null,
    Guid? GenreId = null,
    Guid? UniverseId = null,
    AlbumCollectionState? State = null,
    int Page = 1,
    bool FiltersOpen = false,
    Guid? Delete = null,
    Guid? Merge = null,
    string? MergeText = null)
{
    /// <summary>The number of albums of a page: that of the lists of the API by default.</summary>
    public const int PageSize = 50;

    // The names of the criteria in the address, those of the API for the criteria it reads.
    internal const string TextName = "q";
    internal const string EntryName = "entry";
    internal const string SeriesName = "seriesId";
    internal const string AuthorName = "authorId";
    internal const string PublisherName = "publisherId";
    internal const string PublisherCollectionName = "publisherCollectionId";
    internal const string GenreName = "genreId";
    internal const string UniverseName = "universeId";
    internal const string StateName = "state";
    internal const string PageName = "page";
    internal const string FiltersName = "filters";
    internal const string FiltersOpenValue = "open";
    internal const string DeleteName = "delete";
    internal const string MergeName = "merge";
    internal const string MergeTextName = "mergeQ";

    /// <summary>The whole list, as the menu of the administration opens it.</summary>
    public static readonly AlbumListQuery All = new();

    /// <summary>The cross filters and the state chosen: what the button of the filters counts, neither the search nor the entry.</summary>
    public int ActiveFilterCount =>
        new object?[] { SeriesId, AuthorId, PublisherId, PublisherCollectionId, GenreId, UniverseId, State }.Count(filter => filter is not null);

    /// <summary>The list without its filters, back on its first page: « Tout effacer ».</summary>
    public AlbumListQuery WithoutFilters() => new(Text, Entry);

    /// <summary>The address of the list with these criteria.</summary>
    public string Href()
    {
        var parameters = Parameters();
        return parameters.Count == 0 ? AppPaths.AdminAlbums : $"{AppPaths.AdminAlbums}?{Join(parameters)}";
    }

    /// <summary>
    /// The parameters of the address of the list, in their fixed order: those of <see cref="Href"/>, which a form that leads
    /// to the list carries as hidden fields to keep them.
    /// </summary>
    public IReadOnlyList<(string Name, string Value)> Parameters()
    {
        var parameters = Criteria(includePage: true);
        if (FiltersOpen)
            parameters.Add((FiltersName, FiltersOpenValue));
        if (Delete is { } deleted)
            parameters.Add((DeleteName, deleted.ToString()));
        if (Merge is { } merged)
        {
            parameters.Add((MergeName, merged.ToString()));
            if (!string.IsNullOrWhiteSpace(MergeText))
                parameters.Add((MergeTextName, MergeText));
        }

        return parameters;
    }

    /// <summary>The page of the list of the API with these criteria (<c>GET /admin/albums</c>).</summary>
    public string ApiPath()
    {
        var parameters = Criteria(includePage: false);
        parameters.Add((PageName, Page.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        parameters.Add(("pageSize", PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return $"/admin/albums?{Join(parameters)}";
    }

    /// <summary>
    /// The action of the list the request of this address records for the audience measurement
    /// (fonctionnel/mesure-audience.md, Fonctions suivies), with the criteria of the list as its parameters: a dialog opened
    /// on an album, otherwise that of the last control used, as each control sets the criteria — a page keeps the others, an
    /// entry keeps the search and the filters and returns to the first page, the form of the search and of the filters
    /// leaves the entry and returns to the first page. None for the whole list.
    /// </summary>
    public (AudienceFunction Function, string Parameters)? Action
    {
        get
        {
            if (Delete is { } deleted)
                return (AudienceFunctions.AdminAlbumsDeletion, $"album={deleted}");
            if (Merge is { } merged)
                return (AudienceFunctions.AdminAlbumsMerge, string.IsNullOrWhiteSpace(MergeText)
                    ? $"album={merged}"
                    : $"album={merged}&{Join([(TextName, MergeText)])}");

            var criteria = Criteria(includePage: true);
            if (criteria.Count == 0)
                return null;
            var function = Page > 1 ? AudienceFunctions.AdminAlbumsPage
                : Entry is not null ? AudienceFunctions.AdminAlbumsInitial
                : ActiveFilterCount > 0 ? AudienceFunctions.AdminAlbumsFilter
                : AudienceFunctions.AdminAlbumsSearch;
            return (function, Join(criteria));
        }
    }

    /// <summary>
    /// Reads the criteria of the address of the list (its query, with or without its <c>?</c>); a criterion it cannot read,
    /// which only an address written by hand carries, is left out, as an unknown one.
    /// </summary>
    public static AlbumListQuery Parse(string query)
    {
        var values = QueryHelpers.ParseQuery(query);
        string? Text(string name) => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString().Trim() : null;
        Guid? Id(string name) => Guid.TryParse(Text(name), out var id) ? id : null;

        return new AlbumListQuery(
            Text(TextName),
            Text(EntryName) is { } entry && InitialNavigation.Entries.Contains(entry) ? entry : null,
            Id(SeriesName),
            Id(AuthorName),
            Id(PublisherName),
            Id(PublisherCollectionName),
            Id(GenreName),
            Id(UniverseName),
            Enum.TryParse<AlbumCollectionState>(Text(StateName), out var state) && Enum.IsDefined(state) && !int.TryParse(Text(StateName), out _)
                ? state
                : null,
            int.TryParse(Text(PageName), out var page) && page >= 1 ? page : 1,
            Text(FiltersName) == FiltersOpenValue,
            Id(DeleteName),
            Id(MergeName),
            Text(MergeTextName));
    }

    // In a fixed order, so that the same criteria always make the same address and the same parameters.
    private List<(string Name, string Value)> Criteria(bool includePage)
    {
        var criteria = new List<(string, string)>();
        void Add(string name, object? value)
        {
            if (value is not null && value.ToString() is { Length: > 0 } text)
                criteria.Add((name, text));
        }

        Add(TextName, string.IsNullOrWhiteSpace(Text) ? null : Text.Trim());
        Add(EntryName, Entry);
        Add(SeriesName, SeriesId);
        Add(AuthorName, AuthorId);
        Add(PublisherName, PublisherId);
        Add(PublisherCollectionName, PublisherCollectionId);
        Add(GenreName, GenreId);
        Add(UniverseName, UniverseId);
        Add(StateName, State);
        if (includePage && Page > 1)
            Add(PageName, Page);
        return criteria;
    }

    private static string Join(IEnumerable<(string Name, string Value)> parameters) =>
        string.Join('&', parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));
}
