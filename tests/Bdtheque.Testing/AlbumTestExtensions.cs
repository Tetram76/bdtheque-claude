using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Testing;

public static class AlbumTestExtensions
{
    /// <summary>
    /// Records the acquisition of an edition with its mode alone, without date nor amount: the
    /// shortest acquisition a test can set up, through the single operation the application uses.
    /// </summary>
    public static void RecordAcquisition(this Album album, Edition edition, AcquisitionMode mode) =>
        album.RecordAcquisition(edition, new EditionAcquisition(mode, null, null, null, edition.IsFree, null, null));
}
