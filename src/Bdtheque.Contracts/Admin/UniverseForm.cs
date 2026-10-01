namespace Bdtheque.Contracts.Admin;

/// <summary>A universe as its administration form edits it.</summary>
/// <param name="Version">Version of the universe, sent back with any modification or deletion.</param>
public sealed record UniverseForm(Guid Id, string Name, string? Description, Guid? ParentId, uint Version);

public sealed record CreateUniverseRequest(string Name, string? Description, Guid? ParentId);

/// <param name="Version">Version of the universe the form was read at.</param>
public sealed record UpdateUniverseRequest(string Name, string? Description, Guid? ParentId, uint Version);
