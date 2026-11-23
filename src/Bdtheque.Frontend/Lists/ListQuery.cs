using System.Diagnostics;
using System.Globalization;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Navigation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list, which its address carries, the page being rendered statically: every link and form of the list
/// leads to the list with other criteria, and a list is shared or reloaded as it is. What the lists of the administration
/// (<see cref="AdminListQuery"/>) and those of the consultation (<see cref="CatalogListQuery"/>) share is written here once;
/// each list adds its filters, its paths and the functions the audience measurement follows for it.
/// </summary>
/// <param name="Text">The search; none in a list without search.</param>
/// <param name="Entry">The entry of the navigation by initial; <c>null</c> for « Toutes », or for a list without navigation.</param>
/// <param name="Page">The page of the list, from 1.</param>
public abstract record ListQuery(string? Text, string? Entry, int Page)
{
    /// <summary>The number of records of a page: that of the lists of the API by default.</summary>
    public const int PageSize = 50;

    // The names of the criteria in the address, those of the API for the criteria it reads.
    internal const string TextName = "q";
    internal const string EntryName = "entry";
    internal const string PageName = "page";

    /// <summary>The path of the list in the frontend.</summary>
    protected abstract string Path { get; }

    /// <summary>The path of the list in the API.</summary>
    protected abstract string ApiRoute { get; }

    /// <summary>
    /// The filters of the list, in their fixed order, each with its name in the address (that of the API); a filter not
    /// chosen has no value.
    /// </summary>
    protected abstract IEnumerable<(string Name, object? Value)> Filters { get; }

    /// <summary>The filters chosen: neither the search nor the entry.</summary>
    public int ActiveFilterCount => Filters.Count(filter => filter.Value is not null);

    /// <summary>
    /// Why the list is empty, when it is (visuel/liste-albums/contenu.md, Liste vide): the most precise of its criteria, the
    /// search then the filters, otherwise the entry of the navigation by initial, which is no criterion. What the empty list
    /// offers follows from it, written once for every list, so that none offers to clear a criterion that is not active.
    /// </summary>
    public EmptyListReason EmptyReason => this switch
    {
        { Text: not null } => EmptyListReason.Search,
        { ActiveFilterCount: > 0 } => EmptyListReason.Filters,
        { Entry: not null } => EmptyListReason.Entry,
        _ => EmptyListReason.Nothing,
    };

    /// <summary>The address of the list with these criteria.</summary>
    public string Href() => HrefOf(Path, Parameters());

    /// <summary>The address of a list from its path and the parameters of its address (<see cref="Parameters"/>).</summary>
    public static string HrefOf(string path, IReadOnlyList<(string Name, string Value)> parameters) =>
        parameters.Count == 0 ? path : $"{path}?{Join(parameters)}";

    /// <summary>
    /// The parameters of the address of the list, in their fixed order: those of <see cref="Href"/>, which a form that leads
    /// to the list carries as hidden fields to keep them.
    /// </summary>
    public IReadOnlyList<(string Name, string Value)> Parameters()
    {
        var parameters = Criteria(includePage: true);
        parameters.AddRange(StateParameters);
        return parameters;
    }

    /// <summary>What the address carries besides the criteria of the list (e.g. an open dialog): nothing by default.</summary>
    protected virtual IEnumerable<(string Name, string Value)> StateParameters => [];

    /// <summary>The page of the list of the API with these criteria.</summary>
    public string ApiPath() => ApiPath(PageSize);

    /// <summary>
    /// The page of the list of the API with these criteria, of <paramref name="pageSize"/> records: the first records a search
    /// of the administration presents of the list (Components/Pages/Admin/AdminSearch.razor), found as the list finds them.
    /// </summary>
    public string ApiPath(int pageSize)
    {
        var parameters = Criteria(includePage: false);
        parameters.Add((PageName, Page.ToString(CultureInfo.InvariantCulture)));
        parameters.Add(("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)));
        return $"{ApiRoute}?{Join(parameters)}";
    }

    /// <summary>
    /// The first record of the list of the API without any criterion, whose total is the number of all its records: what a list
    /// reads to give that number when its criteria narrow it.
    /// </summary>
    public string WholeApiPath() => $"{ApiRoute}?{PageName}=1&pageSize=1";

    /// <summary>
    /// The action of the criteria of this address, which its request records for the audience measurement
    /// (fonctionnel/mesure-audience.md, Fonctions suivies), with the criteria as its parameters: that of the last control
    /// used, as each control sets the criteria — a page keeps the others, an entry keeps the search and the filters and
    /// returns to the first page, the form of the search and of the filters leaves the entry and returns to the first page.
    /// None for the whole list.
    /// </summary>
    /// <param name="search">None for a list without search.</param>
    /// <param name="filter">None for a list without filters.</param>
    /// <param name="initial">None for a list without navigation by initial.</param>
    protected (AudienceFunction Function, string? Parameters)? CriteriaAction(
        AudienceFunction? search, AudienceFunction? filter, AudienceFunction? initial, AudienceFunction page)
    {
        var criteria = Criteria(includePage: true);
        if (criteria.Count == 0)
            return null;
        // A list without search, navigation or filters never reads a text, an entry or a filter from its address.
        var function = this switch
        {
            { Page: > 1 } => page,
            { Entry: not null } => initial ?? throw new UnreachableException("An entry in a list without navigation."),
            { ActiveFilterCount: > 0 } => filter ?? throw new UnreachableException("A filter in a list without filters."),
            _ => search ?? throw new UnreachableException("A search in a list without search."),
        };
        return (function, Join(criteria));
    }

    /// <summary>
    /// The criteria of the address of a list (its query, with or without its <c>?</c>), as each list reads them; a criterion it
    /// cannot read, which only an address written by hand carries, is left out, as an unknown one.
    /// </summary>
    protected class Address(string query)
    {
        private readonly Dictionary<string, StringValues> _values = QueryHelpers.ParseQuery(query);

        public string? Text(string name) =>
            _values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString().Trim() : null;

        public Guid? Id(string name) => Guid.TryParse(Text(name), out var id) ? id : null;

        /// <summary>A value of an enumeration by its name, never by its number.</summary>
        public TEnum? Enum<TEnum>(string name) where TEnum : struct, Enum =>
            System.Enum.TryParse<TEnum>(Text(name), out var value) && System.Enum.IsDefined(value) && !int.TryParse(Text(name), out _)
                ? value
                : null;

        public string? Search => Text(TextName);

        public string? Entry => Text(EntryName) is { } entry && InitialNavigation.Entries.Contains(entry) ? entry : null;

        public int Page => int.TryParse(Text(PageName), out var page) && page >= 1 ? page : 1;
    }

    /// <summary>Joins parameters into the query of an address, each value escaped.</summary>
    protected static string Join(IEnumerable<(string Name, string Value)> parameters) =>
        string.Join('&', parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));

    // In a fixed order, so that the same criteria always make the same address and the same parameters.
    private List<(string Name, string Value)> Criteria(bool includePage)
    {
        var criteria = new List<(string, string)>();
        void Add(string name, object? value)
        {
            if (value is not null && Convert.ToString(value, CultureInfo.InvariantCulture) is { Length: > 0 } text)
                criteria.Add((name, text));
        }

        Add(TextName, string.IsNullOrWhiteSpace(Text) ? null : Text.Trim());
        Add(EntryName, Entry);
        foreach (var (name, value) in Filters)
            Add(name, value);
        if (includePage && Page > 1)
            Add(PageName, Page);
        return criteria;
    }
}

/// <summary>Why a list is empty (<see cref="ListQuery.EmptyReason"/>).</summary>
public enum EmptyListReason
{
    /// <summary>Nothing answers the search: the empty list recalls what it looks into, offers to clear it or to create the record.</summary>
    Search,

    /// <summary>Nothing answers the filters, without search: the empty list offers to clear them.</summary>
    Filters,

    /// <summary>No record under the entry of the navigation, without criterion: the empty list offers to go back to « Toutes ».</summary>
    Entry,

    /// <summary>No record at all: the empty list says so.</summary>
    Nothing,
}
