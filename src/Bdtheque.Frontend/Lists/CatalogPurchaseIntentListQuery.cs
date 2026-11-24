using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the public list of the purchase intents, which its address carries (<see cref="ListQuery"/>): its search, on
/// the title of the album or of its series, and the navigation by initial of its albums, without filters, not even that of the
/// possession, of no interest there (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
public sealed record CatalogPurchaseIntentListQuery(string? Text = null, string? Entry = null, int Page = 1)
    : ListQuery(Text, Entry, Page, FiltersOpen: false)
{
    private static readonly ListCriteriaFunctions PurchaseIntentFunctions = new(
        AudienceFunctions.PurchaseIntentsSearch, Filter: null, AudienceFunctions.PurchaseIntentsInitial, AudienceFunctions.PurchaseIntentsPage);

    /// <summary>The whole list, as the menu of the consultation opens it.</summary>
    public static readonly CatalogPurchaseIntentListQuery All = new();

    protected override string Path => AppPaths.PurchaseIntents;

    protected override string ApiRoute => "/catalog/purchase-intents";

    protected override ListCriteriaFunctions CriteriaFunctions => PurchaseIntentFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [];

    /// <inheritdoc cref="ListQuery.Address"/>
    public static CatalogPurchaseIntentListQuery Parse(string query)
    {
        var address = new Address(query);
        return new CatalogPurchaseIntentListQuery(address.Search, address.Entry, address.Page);
    }
}
