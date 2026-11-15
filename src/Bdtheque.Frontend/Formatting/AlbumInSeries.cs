namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// The identification of an album where a context makes its series plain: the record of the album, the cards of its rest of
/// the series and of the record of the series, the bibliography of an author
/// (fonctionnel/presentation/libelle-d-un-album.md#série-reprise-selon-le-contexte). Its series is never part of it, and its
/// volume is presented once in it: in its name, or beside it.
/// </summary>
/// <param name="Name">Its own title; without one, its volume written in full (« Tome 3 »); failing both, the fallback.</param>
/// <param name="Volume">
/// Its volume, abbreviated, which goes with the name to identify the album (« T. 3 »); <c>null</c> when the name already says
/// it, or for a regular album without number.
/// </param>
/// <param name="NamedByVolume">
/// Whether the name is its volume: a place kept for the volume is then left empty rather than marked as missing.
/// </param>
public sealed record AlbumInSeries(string Name, string? Volume, bool NamedByVolume);
