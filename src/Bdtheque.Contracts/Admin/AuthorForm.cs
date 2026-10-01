namespace Bdtheque.Contracts.Admin;

/// <summary>An author as its administration form edits it.</summary>
/// <param name="Version">Version of the author, sent back with any modification or deletion.</param>
public sealed record AuthorForm(
    Guid Id, string? LastName, string? FirstName, string? Pseudonym, string? Biography, string? Nationality, uint Version);

public sealed record CreateAuthorRequest(
    string? LastName, string? FirstName, string? Pseudonym, string? Biography, string? Nationality);

/// <param name="Version">Version of the author the form was read at.</param>
public sealed record UpdateAuthorRequest(
    string? LastName, string? FirstName, string? Pseudonym, string? Biography, string? Nationality, uint Version);
