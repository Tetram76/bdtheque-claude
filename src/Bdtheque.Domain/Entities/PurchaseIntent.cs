using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A wish to acquire (Intention d'achat) either an <see cref="Entities.Album"/> as a whole —
/// any edition will do — or one specific <see cref="Entities.Edition"/> of it. Created only
/// through <see cref="Album.AddPurchaseIntent()"/>, which enforces the per-album rules.
/// </summary>
/// <remarks>
/// <see cref="AlbumId"/> is set for both kinds of intent (for an edition intent, it is the
/// edition's album): the target is the edition when <see cref="EditionId"/> is set, the album
/// otherwise. Storing the album in both cases is what lets the album aggregate — and the
/// database — see every intent concerning it (see choix-implementation.md).
/// </remarks>
public sealed class PurchaseIntent : EntityBase
{
    public Guid AlbumId { get; private set; }
    public Album Album { get; private set; } = null!;

    public Guid? EditionId { get; private set; }
    public Edition? Edition { get; private set; }

    // EF Core parameterless constructor
    private PurchaseIntent() { }

    internal PurchaseIntent(Album album, Edition? edition)
    {
        Album = album;
        AlbumId = album.Id;
        Edition = edition;
        EditionId = edition?.Id;
    }
}
