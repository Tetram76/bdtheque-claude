namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// An album where a context makes its series plain: the record of the album, the rest of its series, the record of the series,
/// the bibliography of an author (fonctionnel/presentation/libelle-d-un-album.md#série-reprise-selon-le-contexte). Its series is
/// never part of it.
/// </summary>
/// <param name="Name">Its own title; without one, its volume written in full (« Tome 3 »); failing both, the fallback.</param>
/// <param name="Volume">
/// Its volume, abbreviated, presented beside the name (« T. 3 »): where it identifies the album with the name
/// (<see cref="RecordLabels.InItsSeries"/>), <c>null</c> when the name already says it; in the column of the rest of the series
/// (<see cref="RecordLabels.InTheRestOfItsSeries"/>), always given. <c>null</c> for a regular album without number.
/// </param>
public sealed record AlbumInSeries(string Name, string? Volume);
