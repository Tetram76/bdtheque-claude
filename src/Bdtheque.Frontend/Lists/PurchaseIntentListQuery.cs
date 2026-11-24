using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the purchase intents of the administration (visuel/listes/liste-par-liste.md), which its address
/// carries (<see cref="AdminListQuery"/>): its search, on the title of the album or of its series, and the navigation by initial
/// of its albums, without filters; an intent is no record to merge, its dialogs being its removal and its conversion into an
/// intent on the whole album, which deletes the edition it targets.
/// </summary>
/// <param name="ToAlbum">The intent on an edition whose conversion into an intent on the whole album the dialog confirms.</param>
public sealed record PurchaseIntentListQuery(
    string? Text = null,
    string? Entry = null,
    int Page = 1,
    Guid? Delete = null,
    Guid? ToAlbum = null) : AdminListQuery(Text, Entry, Page, FiltersOpen: false, Delete, Merge: null, MergeText: null)
{
    internal const string ToAlbumName = "to-album";

    private static readonly ListFunctions PurchaseIntentFunctions = new(
        "intent", AudienceFunctions.AdminPurchaseIntentsSearch, Filter: null, AudienceFunctions.AdminPurchaseIntentsInitial,
        AudienceFunctions.AdminPurchaseIntentsPage, AudienceFunctions.AdminPurchaseIntentsRemoval, Merge: null);

    protected override string Path => AppPaths.AdminPurchaseIntents;

    protected override string ApiRoute => "/admin/purchase-intents";

    protected override ListFunctions Functions => PurchaseIntentFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    protected override IEnumerable<(string Name, string Value)> EntryParameters =>
        ToAlbum is { } converted ? [(ToAlbumName, converted.ToString())] : [];

    protected override (AudienceFunction Function, string? Parameters)? EntryAction =>
        ToAlbum is { } converted ? (AudienceFunctions.AdminPurchaseIntentsConversion, $"intent={converted}") : null;

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static PurchaseIntentListQuery Parse(string query)
    {
        var address = new Address(query);
        return new PurchaseIntentListQuery(address.Search, address.Entry, address.Page, address.Delete, address.Id(ToAlbumName));
    }
}
