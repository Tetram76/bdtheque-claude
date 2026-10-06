namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// An entry of the public list of the purchase intents, which leads to the record of the album, or of
/// the edition targeted (fonctionnel.md § Structure de l'application).
/// </summary>
/// <param name="Edition">The edition targeted; <c>null</c> for an intent on the whole album.</param>
public sealed record PurchaseIntentListItem(Guid Id, AlbumSummary Album, EditionSummary? Edition);
