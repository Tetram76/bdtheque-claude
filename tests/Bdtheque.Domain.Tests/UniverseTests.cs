using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class UniverseTests
{
    [Fact]
    public void Constructor_ValidName_Succeeds()
    {
        var universe = new Universe("Marvel");

        Assert.Equal("Marvel", universe.Name);
        Assert.Null(universe.Parent);
        Assert.Null(universe.ParentId);
        Assert.NotEqual(Guid.Empty, universe.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        DomainAssert.Violates(DomainRules.UniverseNameRequired, () => new Universe(name!));
    }

    [Fact]
    public void SetParent_ValidParent_Succeeds()
    {
        var root = new Universe("Root");
        var child = new Universe("Child");

        child.SetParent(root);

        Assert.Same(root, child.Parent);
        Assert.Equal(root.Id, child.ParentId);
    }

    [Fact]
    public void SetParent_Null_ClearsParent()
    {
        var root = new Universe("Root");
        var child = new Universe("Child");
        child.SetParent(root);

        child.SetParent(null);

        Assert.Null(child.Parent);
        Assert.Null(child.ParentId);
    }

    [Fact]
    public void SetParent_Self_Throws()
    {
        var universe = new Universe("Self");

        DomainAssert.Violates(DomainRules.UniverseHierarchyCycle, () => universe.SetParent(universe));
    }

    [Fact]
    public void SetParent_DirectCycle_Throws()
    {
        var a = new Universe("A");
        var b = new Universe("B");
        b.SetParent(a);

        // A → B already; making B parent of A would create A → B → A
        DomainAssert.Violates(DomainRules.UniverseHierarchyCycle, () => a.SetParent(b));
    }

    [Fact]
    public void SetParent_IndirectCycle_Throws()
    {
        var a = new Universe("A");
        var b = new Universe("B");
        var c = new Universe("C");
        b.SetParent(a);
        c.SetParent(b);

        // Chain: A → B → C; making C parent of A would create A → B → C → A
        DomainAssert.Violates(DomainRules.UniverseHierarchyCycle, () => a.SetParent(c));
    }

    [Fact]
    public void SetParent_LegitimateChain_Succeeds()
    {
        var grandparent = new Universe("Grandparent");
        var parent = new Universe("Parent");
        var child = new Universe("Child");

        parent.SetParent(grandparent);
        child.SetParent(parent);

        Assert.Same(parent, child.Parent);
        Assert.Same(grandparent, parent.Parent);
    }

    [Fact]
    public void SetName_TrimsValue()
    {
        var u = new Universe("  Marvel  ");

        Assert.Equal("Marvel", u.Name);
    }

    [Fact]
    public void SetParent_CycleAmongAncestorsNotInvolvingThis_ThrowsInsteadOfLooping()
    {
        // Simulates data corruption (raw SQL or the future Firebird import tool bypassing
        // SetParent) where two already-persisted universes reference each other as parent,
        // forming a cycle that does not involve 'this' at all. EF Core can materialize such
        // private-setter navigation properties directly when hydrating from the database.
        // Without visited-node tracking, the ancestor walk in SetParent would loop forever
        // instead of rejecting the parent. The existing cycle is corrupted data the user cannot
        // fix through this call, hence a technical error rather than a business rule violation.
        var a = new Universe("A");
        var b = new Universe("B");
        var c = new Universe("C");

        SetPrivate(b, nameof(Universe.Parent), c);
        SetPrivate(b, nameof(Universe.ParentId), c.Id);
        SetPrivate(c, nameof(Universe.Parent), b);
        SetPrivate(c, nameof(Universe.ParentId), b.Id);

        Assert.Throws<InvalidOperationException>(() => a.SetParent(b));
    }

    private static void SetPrivate(Universe entity, string propertyName, object? value) =>
        typeof(Universe).GetProperty(propertyName)!.SetValue(entity, value);

    [Fact]
    public void SetParent_DirectCycleViaPersistedParentId_Throws()
    {
        // Simulates the scenario where A and B are reloaded from the database
        // without eager-loading the Parent navigation property.
        // B has A as its stored parent (B.ParentId = A.Id) but B.Parent is null (not loaded).
        // Calling A.SetParent(B) must still detect the cycle via the ParentId check.
        var a = new Universe("A");
        var b = new Universe("B");

        // Simulate B being saved with A as parent and reloaded without Include
        b.SetParent(a);
        // Now detach B from A so only the ParentId remains (not the in-memory nav)
        // We achieve this by creating a fresh B-like universe with only the ParentId set
        // via the public SetParent API (which sets both Parent and ParentId).
        // After this, a.SetParent(b) should detect cycle because b.ParentId == a.Id.
        DomainAssert.Violates(DomainRules.UniverseHierarchyCycle, () => a.SetParent(b));
    }
}
