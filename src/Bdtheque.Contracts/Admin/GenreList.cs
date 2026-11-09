namespace Bdtheque.Contracts.Admin;

/// <summary>A genre as a row of the list of the administration presents it (visuel/listes/liste-par-liste.md).</summary>
/// <param name="AlbumCount">
/// The albums of the genre, theirs or that of their series (fonctionnel/regles-metier/genres-et-univers-d-un-album.md): those
/// the list of the albums filtered on the genre finds.
/// </param>
/// <param name="SeriesCount">The series of the genre: those the list of the series filtered on the genre finds.</param>
/// <param name="Version">Version of the genre, sent back with its modification or its deletion.</param>
public sealed record GenreAdminListItem(Guid Id, string Label, int AlbumCount, int SeriesCount, DateTimeOffset ModifiedAt, uint Version);
