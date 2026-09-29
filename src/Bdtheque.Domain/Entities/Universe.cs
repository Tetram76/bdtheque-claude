using Bdtheque.Domain.Common;
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

    public void SetName(string name) =>
        Name = DomainText.Required(name, DomainRules.UniverseNameRequired, "A universe must have a name.");

    public void SetDescription(string? description) => Description = DomainText.NullIfBlank(description);

    /// <summary>
    /// Sets or clears the parent universe, enforcing acyclicity on the in-memory graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method walks the proposed parent's ancestor chain, tracking every visited ancestor
    /// ID. At every step it rejects a repeated ID (catches cycles among ancestors, even ones
    /// that do not involve <c>this</c>), then the stored <see cref="ParentId"/> (catches cycles
    /// where the navigation is not loaded but the FK value already points back to <c>this</c>),
    /// then follows the loaded <see cref="Parent"/> navigation.
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
    /// <exception cref="DomainRuleViolationException">
    /// <see cref="DomainRules.UniverseHierarchyCycle"/>: <paramref name="parent"/> is this
    /// universe itself or one of its descendants.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an ancestor in the chain has a stored <see cref="ParentId"/> but its
    /// <see cref="Parent"/> navigation is not loaded, making a complete cycle check impossible,
    /// or when the ancestors of <paramref name="parent"/> already form a cycle (corrupted data).
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
            throw CycleViolation();

        // Walk the ancestor chain. At each node:
        //   1. If the node is 'this', or its ParentId points back to 'this' (even though the
        //      corresponding Parent navigation is not loaded), the requested parent is one of
        //      this universe's descendants: the user's choice would create a cycle.
        //   2. If the node repeats another one already visited, the ancestors already form a
        //      cycle that does not involve 'this' at all (e.g. corrupted data written by raw SQL
        //      or the future Firebird import tool, which bypasses this method): an unbounded
        //      walk would otherwise loop forever. The user cannot fix that through this call,
        //      hence a technical error.
        //   3. If the node has a stored ParentId but no loaded Parent navigation, the chain
        //      is incomplete: throw rather than silently skip — a skipped node may be an ancestor
        //      of 'this', which would produce a persisted cycle.
        //   4. Otherwise, follow the loaded Parent to the next ancestor.
        var visited = new HashSet<Guid>();
        var ancestor = parent;
        while (true)
        {
            if (ancestor.Id == Id || ancestor.ParentId == Id)
                throw CycleViolation();

            if (!visited.Add(ancestor.Id))
                throw new InvalidOperationException(
                    $"The ancestors of universe '{parent.Name}' (Id = {parent.Id}) already form a cycle.");

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
        }

        Parent = parent;
        ParentId = parent.Id;
    }

    private static DomainRuleViolationException CycleViolation() =>
        new(DomainRules.UniverseHierarchyCycle, "A universe cannot be its own ancestor.");
}
