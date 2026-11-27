using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// A universe as its side panel holds it (visuel/listes/saisie-selon-l-entite.md), changed or new: what is typed, its parent
/// as presented, with its ancestors, and the parent created on the spot, if any, with the version it was read at. Persisted
/// from the prerendering into the interactive circuit, which reads it once.
/// </summary>
public sealed class UniverseEntry
{
    /// <summary>None for a new universe.</summary>
    public Guid? Id { get; set; }

    /// <summary>The version the universe was read at, which its saving is written under.</summary>
    public uint Version { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set => field = EntryText.Typed(value); }

    /// <summary>The parent chosen, named with its ancestors; none at the top of the hierarchy.</summary>
    public RecordOption? Parent { get; set; }

    /// <summary>
    /// The parent created on the spot, chosen or not since: suggested again when its name is typed, rather than created twice,
    /// and created with the universe only while it is its parent.
    /// </summary>
    public NewUniverse? NewParent { get; set; }

    /// <summary>The parent created on the spot, while it is the parent chosen. A method, so that it is no part of the state persisted.</summary>
    public NewUniverse? ChosenNewParent() => NewParent is { } created && Parent?.Id == created.Key ? created : null;

    public static UniverseEntry Of(UniverseForm universe) => new()
    {
        Id = universe.Id,
        Version = universe.Version,
        CreatedAt = universe.CreatedAt,
        ModifiedAt = universe.ModifiedAt,
        Name = universe.Name,
        Description = universe.Description,
        Parent = universe.Parent is { } parent ? Option(parent) : null,
    };

    /// <summary>A new universe; opened on a search that finds none, the universe searched.</summary>
    public static UniverseEntry New(string? searched) => new() { Name = searched };

    public UniverseEntry Copy() => (UniverseEntry)MemberwiseClone();

    /// <summary>
    /// The parent created on the spot, sent by its name alone, under the key the field gave it, which the universe cites in
    /// place of its identifier: it is created with the universe, in the same transaction, or not at all.
    /// </summary>
    public NewRecords? NewRecordsSent() => ChosenNewParent() is { } created ? NewRecords.None with { Universes = [created] } : null;

    public CreateUniverseRequest ToCreation(NewRecords? sent) => new(Name ?? "", Description, Parent?.Id, sent);

    public UpdateUniverseRequest ToUpdate(NewRecords? sent) => new(Name ?? "", Description, Parent?.Id, Version, sent);

    /// <summary>A universe presented with its ancestors (« Univers Hugo Pratt › Corto Maltese »).</summary>
    public static RecordOption Option(UniverseInHierarchy universe) =>
        new(universe.Universe.Id, string.Join(" › ", universe.Ancestors.Select(a => a.Name).Append(universe.Universe.Name)), null);

    /// <summary>A parent created on the spot, by its name alone: it has no ancestors.</summary>
    public static RecordOption Option(NewUniverse universe) => new(universe.Key, universe.Name, null);
}
