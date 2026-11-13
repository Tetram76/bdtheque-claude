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
        DomainAssert.Violates<PublisherCollection>(DomainRules.PublisherCollectionNameRequired, nameof(PublisherCollection.Name), () => publisher.AddCollection(name!));
    }

    [Fact]
    public void RemoveCollection_LeavesTheOtherCollections()
    {
        var publisher = new Publisher("Dargaud");
        var removed = publisher.AddCollection("Lucky Comics");
        var kept = publisher.AddCollection("Long courrier");

        publisher.RemoveCollection(removed);

        Assert.Equal([kept], publisher.Collections);
    }

    [Fact]
    public void RemoveCollection_OfAnotherPublisher_Throws()
    {
        var publisher = new Publisher("Dargaud");
        var other = new Publisher("Dupuis").AddCollection("Repérages");

        Assert.Throws<InvalidOperationException>(() => publisher.RemoveCollection(other));
    }

    [Fact]
    public void SetName_TrimsValue()
    {
        var publisher = new Publisher("Dargaud");
        var collection = publisher.AddCollection("  Lucky  ");

        Assert.Equal("Lucky", collection.Name);
    }
}
