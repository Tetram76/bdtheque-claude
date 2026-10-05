using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class ContributionTests
{
    [Fact]
    public void AddContribution_OnAnAlbum_CreditsTheAuthorOnTheAlbumOnly()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        var contribution = album.AddContribution(author, ContributionRole.Scenarist);

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
    public void AddTemplateContribution_OnASeries_CreditsTheAuthorOnTheSeriesOnly()
    {
        var series = new Series("Tintin");
        var author = new Author(null, null, "Hergé");

        var contribution = series.AddTemplateContribution(author, ContributionRole.Illustrator);

        Assert.Same(series, contribution.Series);
        Assert.Equal(series.Id, contribution.SeriesId);
        Assert.Null(contribution.Album);
        Assert.Null(contribution.AlbumId);
        Assert.Equal(ContributionRole.Illustrator, contribution.Role);
    }

    [Fact]
    public void AddContribution_NullAuthor_Throws()
    {
        var album = new Album("Le Lotus bleu", null);

        Assert.Throws<ArgumentNullException>(() => album.AddContribution(null!, ContributionRole.Scenarist));
    }

    [Fact]
    public void AddContribution_UndefinedRole_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentOutOfRangeException>(() => album.AddContribution(author, (ContributionRole)42));
    }

    [Fact]
    public void SetRole_ValidRole_Updates()
    {
        var album = new Album("Le Lotus bleu", null);
        var contribution = album.AddContribution(new Author(null, null, "Hergé"), ContributionRole.Scenarist);

        contribution.SetRole(ContributionRole.Illustrator);

        Assert.Equal(ContributionRole.Illustrator, contribution.Role);
    }

    [Fact]
    public void SetRole_UndefinedRole_Throws()
    {
        var album = new Album("Le Lotus bleu", null);
        var contribution = album.AddContribution(new Author(null, null, "Hergé"), ContributionRole.Scenarist);

        Assert.Throws<ArgumentOutOfRangeException>(() => contribution.SetRole((ContributionRole)42));
    }
}
