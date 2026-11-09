using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the purchase intents of the administration (visuel/listes/liste-par-liste.md), which its address
/// carries (<see cref="AdminListQuery"/>): its search, on the title of the album or of its series, without filters nor
/// navigation by initial; an intent is no record to merge, its removal being its only dialog.
/// </summary>
public sealed record PurchaseIntentListQuery(
    string? Text = null,
    int Page = 1,
    Guid? Delete = null) : AdminListQuery(Text, Entry: null, Page, FiltersOpen: false, Delete, Merge: null, MergeText: null)
{
    private static readonly ListFunctions PurchaseIntentFunctions = new(
        "intent", AudienceFunctions.AdminPurchaseIntentsSearch, Filter: null, Initial: null, AudienceFunctions.AdminPurchaseIntentsPage,
        AudienceFunctions.AdminPurchaseIntentsRemoval, Merge: null);

    protected override string Path => AppPaths.AdminPurchaseIntents;

    protected override string ApiRoute => "/admin/purchase-intents";

    protected override ListFunctions Functions => PurchaseIntentFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    /// <inheritdoc cref="AdminListQuery.Address"/>
    public static PurchaseIntentListQuery Parse(string query)
    {
        var address = new Address(query);
        return new PurchaseIntentListQuery(address.Search, address.Page, address.Delete);
    }
}
