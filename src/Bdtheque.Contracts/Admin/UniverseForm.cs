using Bdtheque.Contracts.Catalog;

namespace Bdtheque.Contracts.Admin;

/// <summary>A universe as its administration form edits it.</summary>
/// <param name="Parent">Its parent with the ancestors of the parent, as the form presents it; <c>null</c> at the top.</param>
/// <param name="Version">Version of the universe, sent back with any modification or deletion.</param>
public sealed record UniverseForm(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentId,
    UniverseInHierarchy? Parent,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    uint Version);

/// <param name="ParentId">Its parent: an existing universe, or the key of the parent the panel created on the spot.</param>
/// <param name="NewRecords">The parent created on the spot, if any, created with the universe (<see cref="Admin.NewRecords"/>).</param>
public sealed record CreateUniverseRequest(string Name, string? Description, Guid? ParentId, NewRecords? NewRecords = null);

/// <param name="ParentId">Its parent: an existing universe, or the key of the parent the panel created on the spot.</param>
/// <param name="Version">Version of the universe the form was read at.</param>
/// <param name="NewRecords">The parent created on the spot, if any, created with the universe (<see cref="Admin.NewRecords"/>).</param>
public sealed record UpdateUniverseRequest(string Name, string? Description, Guid? ParentId, uint Version, NewRecords? NewRecords = null);
