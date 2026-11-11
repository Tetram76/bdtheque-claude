using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;

namespace Bdtheque.Frontend.Components.Records;

/// <summary>Where an album of a card stands with regard to the collection, as the lists tell it (visuel/liste-albums/contenu.md).</summary>
internal static class AlbumCardStates
{
    /// <summary>In the collection when owned; otherwise wished for when an intent targets it, or catalogued only.</summary>
    public static AlbumCollectionState Of(AlbumCard card) => card switch
    {
        { Album.IsInCollection: true } => AlbumCollectionState.InCollection,
        { HasPurchaseIntent: true } => AlbumCollectionState.PurchaseIntent,
        _ => AlbumCollectionState.OutsideCollection,
    };
}
