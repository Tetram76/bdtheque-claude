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
    /// The check combines two complementary strategies:
    /// <list type="bullet">
    ///   <item>ID comparison: catches cycles involving the same entity represented as different
    ///   object instances (e.g. loaded in separate DbContext sessions).</item>
    ///   <item><see cref="ParentId"/> check at every step: catches cycles in mixed
    ///   loaded/unloaded graphs — when the loaded part of the ancestor chain has a node
    ///   whose stored <see cref="ParentId"/> points back to <c>this</c>, the cycle is
    ///   detected even if the node's <see cref="Parent"/> navigation is not yet resolved.</item>
    /// </list>
    /// Cycles that span more than one unloaded navigation hop (e.g. A-&gt;B-&gt;C where both B
    /// and C navigations are null) cannot be detected without querying the database.
    /// A definitive cycle check for such partially-loaded graphs will be added in Phase 2
    /// at the application-layer level (query all ancestor IDs from the database before saving).
    /// </remarks>
    public void SetParent(Universe? parent)
    {
        if (parent is null)
        {
            Parent = null;
            ParentId = null;
            return;
        }

        // Compare by ID to handle detached / differently-instanced entities representing the same row.
        if (parent.Id == Id)
            throw new ArgumentException("A universe cannot be its own parent.", nameof(parent));

        // Walk the proposed parent's ancestor chain using both the loaded navigation and the
        // stored ParentId. At each step:
        //   • ParentId is checked first — catches cycles where the current node's parent is
        //     not loaded in memory but its stored ID already points back to 'this'.
        //   • The loaded Parent navigation is then followed to continue the walk.
        // The loop terminates when both the navigation and the stored ID are absent (root node
        // reached) or when we can no longer advance (navigation unloaded, ParentId unknown).
        var ancestor = parent;
        while (true)
        {
            if (ancestor.ParentId == Id)
                throw new ArgumentException(
                    "Setting this parent would create a cycle in the universe hierarchy.", nameof(parent));

            if (ancestor.Parent is null)
                break;

            ancestor = ancestor.Parent;

            if (ancestor.Id == Id)
                throw new ArgumentException(
                    "Setting this parent would create a cycle in the universe hierarchy.", nameof(parent));
        }

        Parent = parent;
        ParentId = parent.Id;
    }
}
