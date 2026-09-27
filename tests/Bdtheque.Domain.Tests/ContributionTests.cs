using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class ContributionTests
{
    [Fact]
    public void Constructor_AlbumOnly_Succeeds()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        var contribution = new Contribution(album, null, author, ContributionRole.Scenarist);

        Assert.Same(album, contribution.Album);
        Assert.Equal(album.Id, contribution.AlbumId);
        Assert.Null(contribution.Series);
        Assert.Null(contribution.SeriesId);
        Assert.Same(author, contribution.Author);
        Assert.Equal(author.Id, contribution.AuthorId);
        Assert.Equal(ContributionRole.Scenarist, contribution.Role);
        Assert.NotEqual(Guid.Empty, contribution.Id);
    }

    [Fact]
    public void Constructor_SeriesOnly_Succeeds()
    {
        var series = new Series("Tintin");
        var author = new Author(null, null, "Hergé");

        var contribution = new Contribution(null, series, author, ContributionRole.Illustrator);

        Assert.Same(series, contribution.Series);
        Assert.Equal(series.Id, contribution.SeriesId);
        Assert.Null(contribution.Album);
        Assert.Null(contribution.AlbumId);
        Assert.Equal(ContributionRole.Illustrator, contribution.Role);
    }

    [Fact]
    public void Constructor_NeitherAlbumNorSeries_Throws()
    {
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentException>(() => new Contribution(null, null, author, ContributionRole.Colorist));
    }

    [Fact]
    public void Constructor_BothAlbumAndSeries_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var series = new Series("Tintin");
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentException>(() => new Contribution(album, series, author, ContributionRole.Colorist));
    }

    [Fact]
    public void Constructor_NullAuthor_Throws()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Throws<ArgumentNullException>(() => new Contribution(album, null, null!, ContributionRole.Scenarist));
    }

    [Fact]
    public void Constructor_UndefinedRole_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentOutOfRangeException>(() => new Contribution(album, null, author, (ContributionRole)42));
    }

    [Fact]
    public void SetRole_ValidRole_Updates()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");
        var contribution = new Contribution(album, null, author, ContributionRole.Scenarist);

        contribution.SetRole(ContributionRole.Illustrator);

        Assert.Equal(ContributionRole.Illustrator, contribution.Role);
    }

    [Fact]
    public void SetRole_UndefinedRole_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");
        var contribution = new Contribution(album, null, author, ContributionRole.Scenarist);

        Assert.Throws<ArgumentOutOfRangeException>(() => contribution.SetRole((ContributionRole)42));
    }
}
