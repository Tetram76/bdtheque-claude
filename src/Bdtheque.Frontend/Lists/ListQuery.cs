using System.Diagnostics;
using System.Globalization;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Navigation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list, of the consultation as of the administration (visuel/liste-albums/contenu.md, model of the lists),
/// which its address carries, the page being rendered statically: every link and form of the list leads to the list with other
/// criteria. Each list adds its own cross filters, its path and the functions the audience measurement follows for it; a list
/// of the administration, its dialogs (<see cref="AdminListQuery"/>).
/// </summary>
/// <param name="Text">The search.</param>
/// <param name="Entry">The entry of the navigation by initial; <c>null</c> for « Toutes », or for a list without navigation.</param>
/// <param name="Page">The page of the list, from 1.</param>
/// <param name="FiltersOpen">Whether the panel of the filters is open, folded by default.</param>
public abstract record ListQuery(string? Text, string? Entry, int Page, bool FiltersOpen)
{
    /// <summary>The number of records of a page: that of the lists of the API by default.</summary>
    public const int PageSize = 50;

    // The names of the criteria in the address, those of the API for the criteria it reads.
    internal const string TextName = "q";
    internal const string EntryName = "entry";
    internal const string PageName = "page";
    internal const string FiltersName = "filters";
    internal const string FiltersOpenValue = "open";

    /// <summary>The path of the list in the frontend.</summary>
    protected abstract string Path { get; }

    /// <summary>The path of the list in the API.</summary>
    protected abstract string ApiRoute { get; }

    /// <summary>The functions the audience measurement follows for the criteria of the list.</summary>
    protected abstract ListCriteriaFunctions CriteriaFunctions { get; }

    /// <summary>
    /// The cross filters of the list, in their fixed order, each with its name in the address (that of the API); a filter not
    /// chosen has no value.
    /// </summary>
    protected abstract IEnumerable<(string Name, object? Value)> Filters { get; }

    /// <summary>The cross filters chosen: what the button of the filters counts, neither the search nor the entry.</summary>
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

    /// <summary>Whether the search or a filter narrows the list, the entry of the navigation being no criterion.</summary>
    public bool IsNarrowed => Text is not null || ActiveFilterCount > 0;

    /// <summary>The address of the list with these criteria.</summary>
    public string Href() => HrefOf(Path, Parameters());

    /// <summary>
    /// The address of the list without its search, at its first page, its entry and its filters kept: that of « Effacer la
    /// recherche » of an empty list and of the cross of its search field (visuel/liste-albums/controles.md).
    /// </summary>
    public string WithoutSearchHref() => (this with { Text = null, Page = 1 }).Href();

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
        if (FiltersOpen)
            parameters.Add((FiltersName, FiltersOpenValue));
        parameters.AddRange(DialogParameters);
        return parameters;
    }

    /// <summary>What the address opens over the list (a dialog, an entry form), after its criteria: nothing by default.</summary>
    protected virtual IEnumerable<(string Name, string Value)> DialogParameters => [];

    /// <summary>The action of what the address opens over the list, if any, which <see cref="Action"/> records before its criteria.</summary>
    protected virtual (AudienceFunction Function, string? Parameters)? DialogAction => null;

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
    /// The action of the list the request of this address records for the audience measurement
    /// (fonctionnel/mesure-audience.md, Fonctions suivies), with the criteria of the list as its parameters: what the address
    /// opens over the list, otherwise that of the last control used, as each control sets the criteria — a page keeps the
    /// others, an entry keeps the search and the filters and returns to the first page, the form of the search and of the
    /// filters leaves the entry and returns to the first page. None for the whole list.
    /// </summary>
    public (AudienceFunction Function, string? Parameters)? Action
    {
        get
        {
            if (DialogAction is { } dialog)
                return dialog;

            var criteria = Criteria(includePage: true);
            if (criteria.Count == 0)
                return null;
            // A list without navigation or filters never reads an entry or a filter from its address.
            var functions = CriteriaFunctions;
            var function = this switch
            {
                { Page: > 1 } => functions.Page,
                { Entry: not null } => functions.Initial ?? throw new UnreachableException("An entry in a list without navigation."),
                { ActiveFilterCount: > 0 } => functions.Filter ?? throw new UnreachableException("A filter in a list without filters."),
                _ => functions.Search,
            };
            return (function, Join(criteria));
        }
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

        public bool FiltersOpen => Text(FiltersName) == FiltersOpenValue;
    }

    /// <summary>The parameters of an address, joined and escaped.</summary>
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
    /// <summary>Nothing answers the search: the empty list recalls what it looks into, offers to clear it, or to create the record.</summary>
    Search,

    /// <summary>Nothing answers the filters, without search: the empty list offers to clear them.</summary>
    Filters,

    /// <summary>No record under the entry of the navigation, without criterion: the empty list offers to go back to « Toutes ».</summary>
    Entry,

    /// <summary>No record at all.</summary>
    Nothing,
}

/// <summary>The functions the audience measurement follows for the criteria of a list.</summary>
/// <param name="Filter">None for a list without filters, so that no function the list cannot use is followed.</param>
/// <param name="Initial">None for a list without navigation by initial.</param>
public sealed record ListCriteriaFunctions(AudienceFunction Search, AudienceFunction? Filter, AudienceFunction? Initial, AudienceFunction Page);
