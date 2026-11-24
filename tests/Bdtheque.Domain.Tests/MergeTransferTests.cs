using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// What a merge transfers to the record that remains (fonctionnel/regles-metier/fusion-des-fiches.md): every record that
/// designated the other one designates it instead, what designated both keeping it once.
/// </summary>
public sealed class MergeTransferTests
{
    [Fact]
    public void AlbumTransferGenre_ReplacesTheMergedGenreByTheKeptOne()
    {
        var merged = new Genre("SF");
        var kept = new Genre("Science-fiction");
        var album = new Album("Valérian", null, []);
        album.AddGenre(merged);

        album.TransferGenre(merged, kept);

        Assert.Same(kept, Assert.Single(album.Genres));
    }

    [Fact]
    public void AlbumTransferGenre_OfBothGenres_KeepsTheKeptOneOnce()
    {
        var merged = new Genre("SF");
        var kept = new Genre("Science-fiction");
        var album = new Album("Valérian", null, []);
        album.AddGenre(kept);
        album.AddGenre(merged);

        album.TransferGenre(merged, kept);

        Assert.Same(kept, Assert.Single(album.Genres));
    }

    [Fact]
    public void AlbumTransferGenre_WithoutTheMergedGenre_LeavesItsGenresAsTheyAre()
    {
        var other = new Genre("Western");
        var album = new Album("Blueberry", null, []);
        album.AddGenre(other);

        album.TransferGenre(new Genre("SF"), new Genre("Science-fiction"));

        Assert.Same(other, Assert.Single(album.Genres));
    }

    [Fact]
    public void SeriesTransferGenre_ReplacesTheMergedGenreByTheKeptOne()
    {
        var merged = new Genre("SF");
        var kept = new Genre("Science-fiction");
        var series = new Series("Valérian");
        series.AddGenre(merged);
        series.AddGenre(kept);

        series.TransferGenre(merged, kept);

        Assert.Same(kept, Assert.Single(series.Genres));
    }

    [Fact]
    public void AlbumTransferAuthor_CreditsTheKeptAuthorInTheSameRoles()
    {
        var merged = new Author("Gotlib", "Marcel", null);
        var kept = new Author("Gotlieb", "Marcel", "Gotlib");
        var other = new Author(null, null, "Goscinny");
        var album = new Album("Les Dingodossiers", null,
            [(merged, ContributionRole.Illustrator), (merged, ContributionRole.Scenarist), (other, ContributionRole.Scenarist)]);

        album.TransferAuthor(merged, kept);

        Assert.Equivalent(
            new[] { (kept.Id, ContributionRole.Illustrator), (kept.Id, ContributionRole.Scenarist), (other.Id, ContributionRole.Scenarist) },
            album.Contributions.Select(c => (c.AuthorId, c.Role)));
    }

    [Fact]
    public void AlbumTransferAuthor_OfBothAuthorsInTheSameRole_CreditsTheKeptOneOnce()
    {
        var merged = new Author("Gotlib", "Marcel", null);
        var kept = new Author("Gotlieb", "Marcel", "Gotlib");
        var album = new Album("Rubrique-à-brac", null,
            [(kept, ContributionRole.Illustrator), (merged, ContributionRole.Illustrator), (merged, ContributionRole.Colorist)]);

        album.TransferAuthor(merged, kept);

        Assert.Equivalent(
            new[] { (kept.Id, ContributionRole.Illustrator), (kept.Id, ContributionRole.Colorist) },
            album.Contributions.Select(c => (c.AuthorId, c.Role)));
        Assert.All(album.Contributions, c => Assert.Same(album, c.Album));
    }

    [Fact]
    public void SeriesTransferAuthor_CreditsTheKeptAuthorOnTheSeries()
    {
        var merged = new Author("Gotlib", "Marcel", null);
        var kept = new Author("Gotlieb", "Marcel", "Gotlib");
        var series = new Series("Gai-Luron");
        series.SetTemplateContributions([(merged, ContributionRole.Illustrator), (kept, ContributionRole.Illustrator)]);

        series.TransferAuthor(merged, kept);

        var contribution = Assert.Single(series.TemplateContributions);
        Assert.Equal((kept.Id, ContributionRole.Illustrator), (contribution.AuthorId, contribution.Role));
        Assert.Same(series, contribution.Series);
    }

    [Fact]
    public void TransferGenre_ToItself_Throws()
    {
        var genre = new Genre("SF");

        Assert.Throws<ArgumentException>(() => new Album("Valérian", null, []).TransferGenre(genre, genre));
    }

    [Fact]
    public void TransferAuthor_ToItself_Throws()
    {
        var author = new Author("Gotlib", "Marcel", null);

        Assert.Throws<ArgumentException>(() => new Series("Gai-Luron").TransferAuthor(author, author));
    }
}
