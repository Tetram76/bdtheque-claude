using System.Globalization;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Navigation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of the administration (visuel/listes/listes-de-l-administration.md, after the model of the list of
/// the albums), which its address carries, the page being rendered statically: every link and form of the list leads to the
/// list with other criteria. Each list adds its own cross filters, its path and the functions the audience measurement
/// follows for it.
/// </summary>
/// <param name="Text">The search.</param>
/// <param name="Entry">The entry of the navigation by initial; <c>null</c> for « Toutes », or for a list without navigation.</param>
/// <param name="Page">The page of the list, from 1.</param>
/// <param name="FiltersOpen">Whether the panel of the filters is open, folded by default.</param>
/// <param name="Delete">The record whose deletion dialog is open.</param>
/// <param name="Merge">The record whose merge dialog is open.</param>
/// <param name="MergeText">The search of the other record, in the merge dialog.</param>
public abstract record AdminListQuery(string? Text, string? Entry, int Page, bool FiltersOpen, Guid? Delete, Guid? Merge, string? MergeText)
{
    /// <summary>The number of records of a page: that of the lists of the API by default.</summary>
    public const int PageSize = 50;

    // The names of the criteria in the address, those of the API for the criteria it reads.
    internal const string TextName = "q";
    internal const string EntryName = "entry";
    internal const string PageName = "page";
    internal const string FiltersName = "filters";
    internal const string FiltersOpenValue = "open";
    internal const string DeleteName = "delete";
    internal const string MergeName = "merge";
    internal const string MergeTextName = "mergeQ";

    /// <summary>The path of the list in the frontend.</summary>
    protected abstract string Path { get; }

    /// <summary>The path of the list in the API.</summary>
    protected abstract string ApiRoute { get; }

    /// <summary>The functions the audience measurement follows for the actions of the list.</summary>
    protected abstract ListFunctions Functions { get; }

    /// <summary>
    /// The cross filters of the list, in their fixed order, each with its name in the address (that of the API); a filter not
    /// chosen has no value.
    /// </summary>
    protected abstract IEnumerable<(string Name, object? Value)> Filters { get; }

    /// <summary>The cross filters chosen: what the button of the filters counts, neither the search nor the entry.</summary>
    public int ActiveFilterCount => Filters.Count(filter => filter.Value is not null);

    /// <summary>The address of the list with these criteria.</summary>
    public string Href()
    {
        var parameters = Parameters();
        return parameters.Count == 0 ? Path : $"{Path}?{Join(parameters)}";
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

    /// <summary>The page of the list of the API with these criteria.</summary>
    public string ApiPath()
    {
        var parameters = Criteria(includePage: false);
        parameters.Add((PageName, Page.ToString(CultureInfo.InvariantCulture)));
        parameters.Add(("pageSize", PageSize.ToString(CultureInfo.InvariantCulture)));
        return $"{ApiRoute}?{Join(parameters)}";
    }

    /// <summary>
    /// The records of the list of the API found by the search of the other record of a merge, the record itself being one of
    /// them: one more than <paramref name="count"/>, so that <paramref name="count"/> others remain.
    /// </summary>
    public string MergeCandidatesApiPath(string text, int count) =>
        $"{ApiRoute}?{Join([(TextName, text), ("pageSize", (count + 1).ToString(CultureInfo.InvariantCulture))])}";

    /// <summary>
    /// The action of the list the request of this address records for the audience measurement
    /// (fonctionnel/mesure-audience.md, Fonctions suivies), with the criteria of the list as its parameters: a dialog opened
    /// on a record, otherwise that of the last control used, as each control sets the criteria — a page keeps the others, an
    /// entry keeps the search and the filters and returns to the first page, the form of the search and of the filters
    /// leaves the entry and returns to the first page. None for the whole list.
    /// </summary>
    public (AudienceFunction Function, string Parameters)? Action
    {
        get
        {
            if (Delete is { } deleted)
                return (Functions.Deletion, $"{Functions.Subject}={deleted}");
            if (Merge is { } merged)
                return (Functions.Merge, string.IsNullOrWhiteSpace(MergeText)
                    ? $"{Functions.Subject}={merged}"
                    : $"{Functions.Subject}={merged}&{Join([(TextName, MergeText)])}");

            var criteria = Criteria(includePage: true);
            if (criteria.Count == 0)
                return null;
            var function = this switch
            {
                { Page: > 1 } => Functions.Page,
                { Entry: not null } => Functions.Initial,
                { ActiveFilterCount: > 0 } => Functions.Filter,
                _ => Functions.Search,
            };
            return (function, Join(criteria));
        }
    }

    /// <summary>
    /// The criteria of the address of a list (its query, with or without its <c>?</c>), as each list reads them; a criterion it
    /// cannot read, which only an address written by hand carries, is left out, as an unknown one.
    /// </summary>
    protected sealed class Address(string query)
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

        public Guid? Delete => Id(DeleteName);

        public Guid? Merge => Id(MergeName);

        public string? MergeText => Text(MergeTextName);
    }

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

    private static string Join(IEnumerable<(string Name, string Value)> parameters) =>
        string.Join('&', parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));
}

/// <summary>The functions the audience measurement follows for the actions of a list of the administration.</summary>
/// <param name="Subject">The name of the record a dialog is opened on, in the parameters of its action (e.g. <c>album</c>).</param>
public sealed record ListFunctions(
    string Subject,
    AudienceFunction Search,
    AudienceFunction Filter,
    AudienceFunction Initial,
    AudienceFunction Page,
    AudienceFunction Deletion,
    AudienceFunction Merge);
