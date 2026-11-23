using System.Diagnostics;
using System.Globalization;
using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of the administration (visuel/listes/listes-de-l-administration.md, after the model of the list of
/// the albums), which its address carries (<see cref="ListQuery"/>), with what the address opens over it: the panel of the
/// filters, the dialog of a row. Each list adds its own cross filters, its path and the functions the audience measurement
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
    : ListQuery(Text, Entry, Page)
{
    // The names of what the address opens over the list.
    internal const string FiltersName = "filters";
    internal const string FiltersOpenValue = "open";
    internal const string DeleteName = "delete";
    internal const string MergeName = "merge";
    internal const string MergeTextName = "mergeQ";

    /// <summary>The functions the audience measurement follows for the actions of the list.</summary>
    protected abstract ListFunctions Functions { get; }

    /// <summary>
    /// The address of the list without its search, at its first page, its entry and its filters kept: that of « Effacer la
    /// recherche » of an empty list and of the cross of its search field (visuel/liste-albums/controles.md).
    /// </summary>
    public string WithoutSearchHref() => (this with { Text = null, Page = 1 }).Href();

    /// <inheritdoc/>
    protected sealed override IEnumerable<(string Name, string Value)> StateParameters
    {
        get
        {
            if (FiltersOpen)
                yield return (FiltersName, FiltersOpenValue);
            if (Delete is { } deleted)
                yield return (DeleteName, deleted.ToString());
            if (Merge is { } merged)
            {
                yield return (MergeName, merged.ToString());
                if (!string.IsNullOrWhiteSpace(MergeText))
                    yield return (MergeTextName, MergeText);
            }

            foreach (var parameter in EntryParameters)
                yield return parameter;
        }
    }

    /// <summary>
    /// The entry of a record the address opens over the list, for a list whose records are entered in it
    /// (visuel/listes/saisie-selon-l-entite.md): none by default, the records being entered on a page of their own.
    /// </summary>
    protected virtual IEnumerable<(string Name, string Value)> EntryParameters => [];

    /// <summary>The action of the entry opened by the address, if any, which <see cref="Action"/> records.</summary>
    protected virtual (AudienceFunction Function, string? Parameters)? EntryAction => null;

    /// <summary>
    /// The records of the list of the API found by the search of the other record of a merge, the record itself being one of
    /// them: one more than <paramref name="count"/>, so that <paramref name="count"/> others remain.
    /// </summary>
    public string MergeCandidatesApiPath(string text, int count) =>
        $"{ApiRoute}?{Join([(TextName, text), ("pageSize", (count + 1).ToString(CultureInfo.InvariantCulture))])}";

    /// <summary>
    /// The action of the list the request of this address records for the audience measurement
    /// (fonctionnel/mesure-audience.md, Fonctions suivies): a dialog opened on a record, otherwise that of its criteria
    /// (<see cref="ListQuery.CriteriaAction"/>). None for the whole list.
    /// </summary>
    public (AudienceFunction Function, string? Parameters)? Action
    {
        get
        {
            if (EntryAction is { } entry)
                return entry;
            if (Delete is { } deleted)
                return (Functions.Deletion, $"{Functions.Subject}={deleted}");
            if (Merge is { } merged)
            {
                // A list whose records do not merge never reads a merge from its address.
                var merge = Functions.Merge ?? throw new UnreachableException("A merge in a list whose records do not merge.");
                return (merge, string.IsNullOrWhiteSpace(MergeText)
                    ? $"{Functions.Subject}={merged}"
                    : $"{Functions.Subject}={merged}&{Join([(TextName, MergeText)])}");
            }

            return CriteriaAction(Functions.Search, Functions.Filter, Functions.Initial, Functions.Page);
        }
    }

    /// <summary>The criteria of the address of a list of the administration, and what it opens over the list.</summary>
    protected new sealed class Address(string query) : ListQuery.Address(query)
    {
        public bool FiltersOpen => Text(FiltersName) == FiltersOpenValue;

        public Guid? Delete => Id(DeleteName);

        public Guid? Merge => Id(MergeName);

        public string? MergeText => Text(MergeTextName);
    }
}

/// <summary>The functions the audience measurement follows for the actions of a list of the administration.</summary>
/// <param name="Subject">The name of the record a dialog is opened on, in the parameters of its action (e.g. <c>album</c>).</param>
/// <param name="Filter">None for a list without filters, so that no function the list cannot use is followed.</param>
/// <param name="Initial">None for a list without navigation by initial.</param>
/// <param name="Deletion">The dialog of the deletion of a record, or of its removal (a purchase intent).</param>
/// <param name="Merge">None for a list whose records do not merge (the purchase intents).</param>
public sealed record ListFunctions(
    string Subject,
    AudienceFunction Search,
    AudienceFunction? Filter,
    AudienceFunction? Initial,
    AudienceFunction Page,
    AudienceFunction Deletion,
    AudienceFunction? Merge);