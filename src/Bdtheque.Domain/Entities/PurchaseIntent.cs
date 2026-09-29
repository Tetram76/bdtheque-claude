using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A wish to acquire (Intention d'achat) either an <see cref="Entities.Album"/> in any edition,
/// or one specific <see cref="Entities.Edition"/> — never both, never neither.
/// </summary>
public sealed class PurchaseIntent : EntityBase
{
    public Guid? AlbumId { get; private set; }
    public Album? Album { get; private set; }

    public Guid? EditionId { get; private set; }
    public Edition? Edition { get; private set; }

    // EF Core parameterless constructor
    private PurchaseIntent() { }

    // Private: only reachable through ForAlbum/ForEdition below, which each pass exactly one
    // non-null target — so the album/edition exclusivity has nothing left to validate here.
    private PurchaseIntent(Album? album, Edition? edition)
    {
        Album = album;
        AlbumId = album?.Id;
        Edition = edition;
        EditionId = edition?.Id;
    }

    /// <summary>Creates an intent satisfied by any edition of the given album.</summary>
    public static PurchaseIntent ForAlbum(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        return new PurchaseIntent(album, null);
    }

    /// <summary>Creates an intent targeting one specific edition.</summary>
    public static PurchaseIntent ForEdition(Edition edition)
    {
        ArgumentNullException.ThrowIfNull(edition);
        return new PurchaseIntent(null, edition);
    }
}
