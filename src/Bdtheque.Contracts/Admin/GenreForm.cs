namespace Bdtheque.Contracts.Admin;

/// <summary>A genre as its administration form edits it.</summary>
/// <param name="Version">Version of the genre, sent back with any modification or deletion.</param>
public sealed record GenreForm(Guid Id, string Label, uint Version);

public sealed record CreateGenreRequest(string Label);

/// <param name="Version">Version of the genre the form was read at.</param>
public sealed record UpdateGenreRequest(string Label, uint Version);
