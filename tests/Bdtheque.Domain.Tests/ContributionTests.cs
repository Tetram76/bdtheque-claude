using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class ContributionTests
{
    [Fact]
    public void ForAlbum_Valid_Succeeds()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        var contribution = Contribution.ForAlbum(album, author, ContributionRole.Scenarist);

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
    public void ForSeriesTemplate_Valid_Succeeds()
    {
        var series = new Series("Tintin");
        var author = new Author(null, null, "Hergé");

        var contribution = Contribution.ForSeriesTemplate(series, author, ContributionRole.Illustrator);

        Assert.Same(series, contribution.Series);
        Assert.Equal(series.Id, contribution.SeriesId);
        Assert.Null(contribution.Album);
        Assert.Null(contribution.AlbumId);
        Assert.Equal(ContributionRole.Illustrator, contribution.Role);
    }

    [Fact]
    public void ForAlbum_NullAlbum_Throws()
    {
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentNullException>(() => Contribution.ForAlbum(null!, author, ContributionRole.Scenarist));
    }

    [Fact]
    public void ForSeriesTemplate_NullSeries_Throws()
    {
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentNullException>(() => Contribution.ForSeriesTemplate(null!, author, ContributionRole.Illustrator));
    }

    [Fact]
    public void ForAlbum_NullAuthor_Throws()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Throws<ArgumentNullException>(() => Contribution.ForAlbum(album, null!, ContributionRole.Scenarist));
    }

    [Fact]
    public void ForAlbum_UndefinedRole_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentOutOfRangeException>(() => Contribution.ForAlbum(album, author, (ContributionRole)42));
    }

    [Fact]
    public void SetRole_ValidRole_Updates()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");
        var contribution = Contribution.ForAlbum(album, author, ContributionRole.Scenarist);

        contribution.SetRole(ContributionRole.Illustrator);

        Assert.Equal(ContributionRole.Illustrator, contribution.Role);
    }

    [Fact]
    public void SetRole_UndefinedRole_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");
        var contribution = Contribution.ForAlbum(album, author, ContributionRole.Scenarist);

        Assert.Throws<ArgumentOutOfRangeException>(() => contribution.SetRole((ContributionRole)42));
    }
}
