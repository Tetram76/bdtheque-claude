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

    // A publisher is filed under the initial of its name, as it is sorted, article included (visuel/listes/liste-par-liste.md,
    // navigation par initiale), the entries being those of every navigation
    // (fonctionnel/presentation/tri-et-navigation-par-initiale.md).
    [Theory]
    [InlineData("Dargaud", "D")]
    [InlineData("Le Lombard", "L")]
    [InlineData("Éditions Delcourt", "E")]
    [InlineData("  çà et là", "C")]
    [InlineData("6 pieds sous terre", "#")]
    [InlineData("(Æ)Lab", "@")]
    public void Constructor_ComputesNavigationEntry(string name, string expectedEntry)
    {
        Assert.Equal(expectedEntry, new Publisher(name).NavigationEntry);
    }

    [Fact]
    public void SetName_RecomputesNavigationEntry()
    {
        var publisher = new Publisher("Dargaud");

        publisher.SetName("Glénat");

        Assert.Equal("G", publisher.NavigationEntry);
    }

    [Fact]
    public void SetName_Rejected_KeepsNavigationEntry()
    {
        var publisher = new Publisher("Dargaud");

        DomainAssert.Violates<Publisher>(DomainRules.PublisherNameRequired, nameof(Publisher.Name), () => publisher.SetName(""));

        Assert.Equal("D", publisher.NavigationEntry);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        DomainAssert.Violates<Publisher>(DomainRules.PublisherNameRequired, nameof(Publisher.Name), () => new Publisher(name!));
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
        DomainAssert.Violates<Publisher>(DomainRules.PublisherWebsiteInvalid, nameof(Publisher.Website), () => publisher.SetWebsite(url));
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
