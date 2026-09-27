using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

public sealed class PublisherTests
{
    [Fact]
    public void Constructor_ValidName_Succeeds()
    {
        var publisher = new Publisher("Dargaud");

        Assert.Equal("Dargaud", publisher.Name);
        Assert.Null(publisher.Website);
        Assert.NotEqual(Guid.Empty, publisher.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        // null input yields ArgumentNullException (subtype of ArgumentException); all are valid guards
        Assert.ThrowsAny<ArgumentException>(() => new Publisher(name!));
    }

    [Fact]
    public void SetWebsite_TrimsAndSetsValue()
    {
        var publisher = new Publisher("Dargaud");

        publisher.SetWebsite("  https://dargaud.com  ");

        Assert.Equal("https://dargaud.com", publisher.Website);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetWebsite_NullOrEmpty_SetsNull(string? url)
    {
        var publisher = new Publisher("Dargaud");
        publisher.SetWebsite("https://dargaud.com");

        publisher.SetWebsite(url);

        Assert.Null(publisher.Website);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://dargaud.com")]
    [InlineData("just text")]
    [InlineData("//missing-scheme.com")]
    public void SetWebsite_InvalidUrl_Throws(string url)
    {
        var publisher = new Publisher("Dargaud");
        Assert.Throws<ArgumentException>(() => publisher.SetWebsite(url));
    }

    [Theory]
    [InlineData("https://dargaud.com")]
    [InlineData("http://dargaud.com/path?q=1")]
    public void SetWebsite_ValidUrl_Succeeds(string url)
    {
        var publisher = new Publisher("Dargaud");
        publisher.SetWebsite(url);
        Assert.Equal(url, publisher.Website);
    }

    [Fact]
    public void SetName_TrimsValue()
    {
        var publisher = new Publisher("  Dargaud  ");

        Assert.Equal("Dargaud", publisher.Name);
    }
}
