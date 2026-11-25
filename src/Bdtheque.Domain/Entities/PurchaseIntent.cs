using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A wish to acquire (Intention d'acquisition) either an <see cref="Entities.Album"/> as a whole —
/// any edition will do — or one specific <see cref="Entities.Edition"/> of it. Created only by the
/// album aggregate (<see cref="Album.AddPurchaseIntent()"/> and its conversions), which enforces the
/// per-album rules.
/// </summary>
/// <remarks>
/// <see cref="AlbumId"/> is set for both kinds of intent (for an edition intent, it is the
/// edition's album): the target is the edition when <see cref="EditionId"/> is set, the album
/// otherwise. Storing the album in both cases is what lets the album aggregate — and the
/// database — see every intent concerning it (see choix-implementation/domaine/intention-d-acquisition-agregat-porte-par-l-album.md).
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

    /// <summary>
    /// Passes the intent to <paramref name="album"/>, with its identity, as the merge of two albums does
    /// (<see cref="Album.TakeEditionsOf"/>): the edition it targets passes with it.
    /// </summary>
    internal void MoveTo(Album album)
    {
        Album = album;
        AlbumId = album.Id;
    }
}
