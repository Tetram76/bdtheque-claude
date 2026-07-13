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
    /// <para>
    /// The method walks the proposed parent's ancestor chain. At every step it checks the
    /// stored <see cref="ParentId"/> (catches cycles where the navigation is not loaded but
    /// the FK value already points back to <c>this</c>) and then the loaded
    /// <see cref="Parent"/> navigation (catches cycles among fully-loaded entities).
    /// </para>
    /// <para>
    /// <b>Prerequisite:</b> the full ancestor chain of <paramref name="parent"/> must be
    /// loaded in memory before calling this method. If any node in the chain has a non-null
    /// <see cref="ParentId"/> but a null <see cref="Parent"/> navigation, the method throws
    /// <see cref="InvalidOperationException"/> rather than silently accepting an assignment
    /// that may introduce a cycle. The application-layer command handler is responsible for
    /// loading all ancestors (e.g. <c>Include(u => u.Parent).ThenInclude(p => p.Parent)…</c>
    /// or a recursive ancestor query) before calling this method.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="parent"/> is the same universe (self-reference) or when
    /// the in-memory ancestor chain reveals a cycle.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an ancestor in the chain has a stored <see cref="ParentId"/> but its
    /// <see cref="Parent"/> navigation is not loaded, making a complete cycle check impossible.
    /// </exception>
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

        // Walk the ancestor chain. At each node:
        //   1. If the node's ParentId points back to 'this', a cycle is detected.
        //   2. If the node has a stored ParentId but no loaded Parent navigation, the chain
        //      is incomplete: throw rather than silently skip — a skipped node may be an ancestor
        //      of 'this', which would produce a persisted cycle.
        //   3. Otherwise, follow the loaded Parent to the next ancestor.
        var ancestor = parent;
        while (true)
        {
            if (ancestor.ParentId == Id)
                throw new ArgumentException(
                    "Setting this parent would create a cycle in the universe hierarchy.", nameof(parent));

            if (ancestor.Parent is null)
            {
                if (ancestor.ParentId.HasValue)
                    throw new InvalidOperationException(
                        $"Universe '{ancestor.Name}' (Id = {ancestor.Id}) has a stored parent " +
                        $"(ParentId = {ancestor.ParentId}) that is not loaded in memory. " +
                        "Load the full ancestor chain before calling SetParent.");
                break; // root node reached — chain is complete
            }

            ancestor = ancestor.Parent;

            if (ancestor.Id == Id)
                throw new ArgumentException(
                    "Setting this parent would create a cycle in the universe hierarchy.", nameof(parent));
        }

        Parent = parent;
        ParentId = parent.Id;
    }
}
