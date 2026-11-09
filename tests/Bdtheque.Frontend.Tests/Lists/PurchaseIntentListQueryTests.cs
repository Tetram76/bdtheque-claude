using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Lists;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// The criteria of the list of the purchase intents of the administration, carried by its address: what differs from the list
/// of the albums (AlbumListQueryTests), its paths, neither filters nor navigation by initial nor merge, and the functions of
/// its actions.
/// </summary>
public sealed class PurchaseIntentListQueryTests
{
    private static readonly Guid Intent = Guid.Parse("01900000-0000-7000-8000-000000000003");

    [Fact]
    public void Href_CarriesTheSearchAndThePage()
    {
        Assert.Equal($"{AppPaths.AdminPurchaseIntents}?q=Tintin&page=2", new PurchaseIntentListQuery(Text: "Tintin", Page: 2).Href());
        Assert.Equal(AppPaths.AdminPurchaseIntents, new PurchaseIntentListQuery().Href());
        Assert.Equal(0, new PurchaseIntentListQuery(Text: "Tintin").ActiveFilterCount);
    }

    [Fact]
    public void ApiPath_AsksThePageOfTheListOfTheIntents()
    {
        Assert.Equal(
            "/admin/purchase-intents?q=Tintin&page=1&pageSize=50",
            new PurchaseIntentListQuery(Text: "Tintin", Delete: Intent).ApiPath());
    }

    [Fact]
    public void Parse_ReadsBackTheAddressOfTheList_LeavingOutWhatItCannotRead_AMergeAmongThem()
    {
        var query = new PurchaseIntentListQuery(Text: "Tintin", Page: 3, Delete: Intent);

        Assert.Equal(query, PurchaseIntentListQuery.Parse(new Uri($"http://frontend{query.Href()}").Query));
        // An intent is no record to merge, and the list has neither filters nor navigation by initial.
        Assert.Equal(new PurchaseIntentListQuery(), PurchaseIntentListQuery.Parse($"?merge={Intent}&entry=B&filters=open&albumId={Intent}"));
    }

    [Theory]
    [InlineData("q=Tintin", "admin.purchase-intents.search", "q=Tintin")]
    [InlineData("q=Tintin&page=2", "admin.purchase-intents.page", "q=Tintin&page=2")]
    [InlineData("delete=01900000-0000-7000-8000-000000000003", "admin.purchase-intents.removal", "intent=01900000-0000-7000-8000-000000000003")]
    public void Action_IsThatOfTheListOfTheIntents(string address, string function, string parameters)
    {
        var action = PurchaseIntentListQuery.Parse(address).Action!.Value;

        Assert.Equal((function, parameters), (action.Function.Key, action.Parameters));
        Assert.Contains(action.Function, AudienceFunctions.Followed);
    }

    [Fact]
    public void Action_OfTheWholeList_IsNone()
    {
        Assert.Null(new PurchaseIntentListQuery().Action);
    }
}
