using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class PublisherCollectionTests
{
    [Fact]
    public void Constructor_ValidArguments_Succeeds()
    {
        var publisher = new Publisher("Dargaud");
        var collection = new PublisherCollection("Lucky Comics", publisher);

        Assert.Equal("Lucky Comics", collection.Name);
        Assert.Same(publisher, collection.Publisher);
        Assert.Equal(publisher.Id, collection.PublisherId);
        Assert.NotEqual(Guid.Empty, collection.Id);
    }

    [Fact]
    public void Constructor_NullPublisher_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PublisherCollection("Lucky Comics", null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        var publisher = new Publisher("Dargaud");
        DomainAssert.Violates(DomainRules.PublisherCollectionNameRequired, () => new PublisherCollection(name!, publisher));
    }

    [Fact]
    public void SetName_TrimsValue()
    {
        var publisher = new Publisher("Dargaud");
        var collection = new PublisherCollection("  Lucky  ", publisher);

        Assert.Equal("Lucky", collection.Name);
    }
}
