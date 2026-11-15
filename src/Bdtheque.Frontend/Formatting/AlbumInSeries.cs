namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// An album as a context that makes its series plain presents it: the record of the album, its rest of the series, the
/// record of the series, the bibliography of an author (fonctionnel/presentation/libelle-d-un-album.md#série-reprise-selon-le-contexte).
/// Its series is never part of it, and its volume is presented once: in its name, or beside it.
/// </summary>
/// <param name="Name">Its own title; without one, its volume written in full (« Tome 3 »); failing both, the fallback.</param>
/// <param name="Volume">
/// Its volume, abbreviated, to present beside the name (« T. 3 »); <c>null</c> when the name already says it, or for a regular
/// album without number.
/// </param>
/// <param name="NamedByVolume">
/// Whether the name is its volume: a place kept for the volume (a column) is then left empty rather than marked as missing.
/// </param>
public sealed record AlbumInSeries(string Name, string? Volume, bool NamedByVolume);
