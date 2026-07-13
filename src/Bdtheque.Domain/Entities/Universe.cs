using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A fictional setting to which albums and series can belong, optionally nested (Univers).
/// The parent–child hierarchy is acyclic: an universe cannot be its own ancestor.
/// </summary>
public sealed class Universe : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public Guid? ParentId { get; private set; }
    public Universe? Parent { get; private set; }

    public ICollection<Universe> Children { get; private set; } = [];

    // EF Core parameterless constructor
    private Universe() { }

    public Universe(string name)
    {
        SetName(name);
    }

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void SetDescription(string? description) =>
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Sets or clears the parent universe, enforcing acyclicity on the in-memory graph.
    /// </summary>
    /// <remarks>
    /// This check walks the <see cref="Parent"/> chain that is already loaded in memory.
    /// It reliably prevents cycles among entities that share the same object graph, but
    /// cannot detect a cycle involving a parent that was not loaded from the database.
    /// A definitive cycle check for partially-loaded graphs will be added in Phase 2
    /// at the application-layer level (query all ancestors from the database before saving).
    /// </remarks>
    public void SetParent(Universe? parent)
    {
        if (parent is null)
        {
            Parent = null;
            ParentId = null;
            return;
        }

        if (ReferenceEquals(parent, this))
            throw new ArgumentException("A universe cannot be its own parent.", nameof(parent));

        // Detect a direct 1-level cycle when the parent navigation is not loaded:
        // if parent.ParentId points back to this entity, the proposed relation would create A → B → A.
        // This covers the common case of reloading entities from the DB without eager-loading .Parent.
        if (parent.ParentId == Id)
            throw new ArgumentException(
                "Setting this parent would create a cycle in the universe hierarchy.", nameof(parent));

        // Walk the in-memory ancestor chain to detect longer cycles among fully-loaded entities.
        var ancestor = parent.Parent;
        while (ancestor is not null)
        {
            if (ReferenceEquals(ancestor, this))
                throw new ArgumentException(
                    "Setting this parent would create a cycle in the universe hierarchy.", nameof(parent));
            ancestor = ancestor.Parent;
        }

        Parent = parent;
        ParentId = parent.Id;
    }
}
