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
        // null input yields ArgumentNullException (subtype of ArgumentException); all are valid guards
        Assert.ThrowsAny<ArgumentException>(() => new Universe(name!));
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

        Assert.Throws<ArgumentException>(() => universe.SetParent(universe));
    }

    [Fact]
    public void SetParent_DirectCycle_Throws()
    {
        var a = new Universe("A");
        var b = new Universe("B");
        b.SetParent(a);

        // A → B already; making B parent of A would create A → B → A
        Assert.Throws<ArgumentException>(() => a.SetParent(b));
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
        Assert.Throws<ArgumentException>(() => a.SetParent(c));
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
}
