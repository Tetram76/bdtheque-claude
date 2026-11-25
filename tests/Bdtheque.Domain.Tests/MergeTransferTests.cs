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

    // A contribution keeps its identity, which a saved search may designate: it is credited to the kept author, not replaced.
    [Fact]
    public void AlbumTransferAuthor_KeepsTheContributionsTheyAre_AndSaysWhichOneReplacesADuplicate()
    {
        var merged = new Author("Gotlib", "Marcel", null);
        var kept = new Author("Gotlieb", "Marcel", "Gotlib");
        var album = new Album("Rubrique-à-brac", null,
            [(kept, ContributionRole.Illustrator), (merged, ContributionRole.Illustrator), (merged, ContributionRole.Colorist)]);
        var keptIllustrator = album.Contributions.Single(c => c.AuthorId == kept.Id).Id;
        var duplicate = album.Contributions.Single(c => c.AuthorId == merged.Id && c.Role == ContributionRole.Illustrator).Id;
        var colorist = album.Contributions.Single(c => c.Role == ContributionRole.Colorist).Id;

        var replaced = album.TransferAuthor(merged, kept);

        Assert.Equivalent(new[] { keptIllustrator, colorist }, album.Contributions.Select(c => c.Id));
        Assert.Same(kept, album.Contributions.Single(c => c.Id == colorist).Author);
        Assert.Equal([(duplicate, keptIllustrator)], replaced);
    }

    [Fact]
    public void SeriesTransferAuthor_KeepsTheContributionItCredits()
    {
        var merged = new Author("Gotlib", "Marcel", null);
        var kept = new Author("Gotlieb", "Marcel", "Gotlib");
        var series = new Series("Gai-Luron");
        series.SetTemplateContributions([(merged, ContributionRole.Scenarist)]);
        var contribution = Assert.Single(series.TemplateContributions).Id;

        var replaced = series.TransferAuthor(merged, kept);

        Assert.Equal(contribution, Assert.Single(series.TemplateContributions).Id);
        Assert.Equal(kept.Id, series.TemplateContributions.Single().AuthorId);
        Assert.Empty(replaced);
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
    [Fact]
    public void AlbumTransferUniverse_OfBothUniverses_KeepsTheKeptOneOnce()
    {
        var merged = new Universe("Star wars");
        var kept = new Universe("Star Wars");
        var album = new Album("Dark Empire", null, []);
        album.AddUniverse(merged);
        album.AddUniverse(kept);

        album.TransferUniverse(merged, kept);

        Assert.Same(kept, Assert.Single(album.Universes));
    }

    [Fact]
    public void SeriesTransferUniverse_ReplacesTheMergedUniverseByTheKeptOne()
    {
        var merged = new Universe("Star wars");
        var kept = new Universe("Star Wars");
        var series = new Series("Clone Wars");
        series.AddUniverse(merged);

        series.TransferUniverse(merged, kept);

        Assert.Same(kept, Assert.Single(series.Universes));
    }

    [Fact]
    public void UniverseAbsorb_TakesTheSubUniversesOfTheMergedOneButThoseLeftOut()
    {
        var kept = new Universe("Star Wars");
        var merged = new Universe("Star wars");
        var moved = new Universe("Rebels");
        var paired = new Universe("Clone wars");
        moved.SetParent(merged);
        paired.SetParent(merged);

        kept.Absorb(merged, parent: null, leftOut: [paired.Id]);

        Assert.Same(kept, moved.Parent);
        Assert.Same(merged, paired.Parent);
        Assert.Same(moved, Assert.Single(kept.Children));
    }

    [Fact]
    public void UniverseAbsorb_TakesTheParentChosen()
    {
        var lucasfilm = new Universe("Lucasfilm");
        var kept = new Universe("Star Wars");
        var merged = new Universe("Star wars");
        merged.SetParent(lucasfilm);

        kept.Absorb(merged, lucasfilm, leftOut: []);

        Assert.Same(lucasfilm, kept.Parent);
    }

    [Fact]
    public void UniverseAbsorb_OfOneOfItsSubUniverses_KeepsItsSubUniversesUnderItself()
    {
        var pratt = new Universe("Univers Hugo Pratt");
        var kept = new Universe("Corto Maltese");
        var merged = new Universe("Corto");
        var child = new Universe("Jeunesse");
        kept.SetParent(pratt);
        merged.SetParent(kept);
        child.SetParent(merged);

        kept.Absorb(merged, pratt, leftOut: []);

        Assert.Same(kept, child.Parent);
        Assert.Same(pratt, kept.Parent);
    }

    // The universe that remains stands deeper under the merged one, below a sub-universe of the latter, which passes under it:
    // Pratt › Corto › Jeunesse › Corto Maltese becomes Pratt › Corto Maltese › Jeunesse.
    [Fact]
    public void UniverseAbsorb_OfAnAncestorAboveASubUniverse_TakesThatSubUniverseUnderItself()
    {
        var pratt = new Universe("Univers Hugo Pratt");
        var merged = new Universe("Corto");
        var child = new Universe("Jeunesse");
        var kept = new Universe("Corto Maltese");
        merged.SetParent(pratt);
        child.SetParent(merged);
        kept.SetParent(child);

        kept.Absorb(merged, pratt, leftOut: []);

        Assert.Same(pratt, kept.Parent);
        Assert.Same(kept, child.Parent);
    }

    [Fact]
    public void UniverseAbsorb_UnderTheMergedUniverse_Violates()
    {
        var kept = new Universe("Corto");
        var merged = new Universe("Corto Maltese");
        kept.SetParent(merged);

        DomainAssert.Violates<Universe>(DomainRules.UniverseHierarchyCycle, nameof(Universe.ParentId), () => kept.Absorb(merged, merged, leftOut: []));
    }

    [Fact]
    public void UniverseAbsorb_UnderASubUniverseOfTheMergedOne_Violates()
    {
        var kept = new Universe("Star Wars");
        var merged = new Universe("Star wars");
        var sub = new Universe("Rebels");
        sub.SetParent(merged);

        DomainAssert.Violates<Universe>(DomainRules.UniverseHierarchyCycle, nameof(Universe.ParentId), () => kept.Absorb(merged, sub, leftOut: []));
    }

    [Fact]
    public void UniverseAbsorb_ItSelf_Throws()
    {
        var universe = new Universe("Star Wars");

        Assert.Throws<ArgumentException>(() => universe.Absorb(universe, null, []));
    }
}
