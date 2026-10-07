using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class ContributionTests
{
    [Fact]
    public void ContributionOfAnAlbum_CreditsTheAuthorOnTheAlbumOnly()
    {
        var author = new Author(null, null, "Hergé");

        var album = new Album("Le Lotus bleu", null, [(author, ContributionRole.Scenarist)]);

        var contribution = Assert.Single(album.Contributions);
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
    public void ContributionOfAnAlbum_NullAuthor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Album("Le Lotus bleu", null, [(null!, ContributionRole.Scenarist)]));
    }

    [Fact]
    public void ContributionOfAnAlbum_UndefinedRole_Throws()
    {
        var author = new Author(null, null, "Hergé");

        Assert.Throws<ArgumentOutOfRangeException>(() => new Album("Le Lotus bleu", null, [(author, (ContributionRole)42)]));
    }
}
