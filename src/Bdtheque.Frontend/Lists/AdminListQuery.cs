using System.Diagnostics;
using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of the administration (visuel/listes/listes-de-l-administration.md, after the model of the list of
/// the albums): those of every list (<see cref="ListQuery"/>), and the dialogs of a row its address opens, the deletion and the
/// merge of a record, or the entry of a record in the list itself.
/// </summary>
/// <param name="Delete">The record whose deletion dialog is open.</param>
/// <param name="Merge">The record whose merge dialog is open.</param>
/// <param name="MergeText">The search of the other record, in the merge dialog.</param>
public abstract record AdminListQuery(string? Text, string? Entry, int Page, bool FiltersOpen, Guid? Delete, Guid? Merge, string? MergeText)
    : ListQuery(Text, Entry, Page, FiltersOpen)
{
    internal const string DeleteName = "delete";
    internal const string MergeName = "merge";
    internal const string MergeTextName = "mergeQ";

    /// <summary>The functions the audience measurement follows for the actions of the list.</summary>
    protected abstract ListFunctions Functions { get; }

    protected sealed override ListCriteriaFunctions CriteriaFunctions => new(Functions.Search, Functions.Filter, Functions.Initial, Functions.Page);

    /// <summary>
    /// The records of the list of the API found by the search of the other record of a merge, the record itself being one of
    /// them: one more than <paramref name="count"/>, so that <paramref name="count"/> others remain.
    /// </summary>
    public string MergeCandidatesApiPath(string text, int count) =>
        $"{ApiRoute}?{Join([(TextName, text), ("pageSize", (count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))])}";

    /// <summary>
    /// The entry of a record the address opens over the list, for a list whose records are entered in it
    /// (visuel/listes/saisie-selon-l-entite.md): none by default, the records being entered on a page of their own.
    /// </summary>
    protected virtual IEnumerable<(string Name, string Value)> EntryParameters => [];

    /// <summary>The action of the entry opened by the address, if any, which <see cref="ListQuery.Action"/> records.</summary>
    protected virtual (AudienceFunction Function, string? Parameters)? EntryAction => null;

    protected sealed override IEnumerable<(string Name, string Value)> DialogParameters
    {
        get
        {
            var parameters = new List<(string, string)>();
            if (Delete is { } deleted)
                parameters.Add((DeleteName, deleted.ToString()));
            if (Merge is { } merged)
            {
                parameters.Add((MergeName, merged.ToString()));
                if (!string.IsNullOrWhiteSpace(MergeText))
                    parameters.Add((MergeTextName, MergeText));
            }

            parameters.AddRange(EntryParameters);
            return parameters;
        }
    }

    protected sealed override (AudienceFunction Function, string? Parameters)? DialogAction
    {
        get
        {
            if (EntryAction is { } entry)
                return entry;
            if (Delete is { } deleted)
                return (Functions.Deletion, $"{Functions.Subject}={deleted}");
            if (Merge is not { } merged)
                return null;

            // A list whose records do not merge never reads a merge from its address.
            var merge = Functions.Merge ?? throw new UnreachableException("A merge in a list whose records do not merge.");
            return (merge, string.IsNullOrWhiteSpace(MergeText)
                ? $"{Functions.Subject}={merged}"
                : $"{Functions.Subject}={merged}&{Join([(TextName, MergeText)])}");
        }
    }

    /// <inheritdoc cref="ListQuery.Address"/>
    /// <remarks>The dialogs of a row, besides the criteria of every list.</remarks>
    protected new sealed class Address(string query) : ListQuery.Address(query)
    {
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
