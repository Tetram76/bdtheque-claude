using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class PublisherCollectionTests
{
    [Fact]
    public void Constructor_ValidArguments_Succeeds()
    {
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("Lucky Comics");

        Assert.Equal("Lucky Comics", collection.Name);
        Assert.Same(publisher, collection.Publisher);
        Assert.Equal(publisher.Id, collection.PublisherId);
        Assert.NotEqual(Guid.Empty, collection.Id);
    }

    [Fact]
    public void AddCollection_AppearsInPublisherCollections()
    {
        var publisher = new Publisher("Dargaud");

        var collection = publisher.AddCollection("Lucky Comics");

        Assert.Contains(collection, publisher.Collections);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        var publisher = new Publisher("Dargaud");
        DomainAssert.Violates(DomainRules.PublisherCollectionNameRequired, () => publisher.AddCollection(name!));
    }

    [Fact]
    public void SetName_TrimsValue()
    {
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("  Lucky  ");

        Assert.Equal("Lucky", collection.Name);
    }
}
