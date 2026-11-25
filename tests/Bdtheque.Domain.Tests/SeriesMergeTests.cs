using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The merge of two series (fonctionnel/regles-metier/fusion-des-fiches.md): the albums of the other series pass to the one that
/// remains, which keeps, credit by credit, the contributions retained of both, a credit both series hold being kept once.
/// </summary>
public sealed class SeriesMergeTests
{
    private readonly Author _goscinny = new(null, null, "Goscinny");
    private readonly Author _uderzo = new("Uderzo", "Albert", null);
    private readonly Author _morris = new(null, null, "Morris");

    [Fact]
    public void AlbumTransferSeries_AttachesTheAlbumToTheKeptSeries()
    {
        var merged = new Series("Asterix");
        var kept = new Series("Astérix");
        var album = new Album(null, merged, []);

        album.TransferSeries(merged, kept);

        Assert.Same(kept, album.Series);
        Assert.Equal(kept.Id, album.SeriesId);
    }

    [Fact]
    public void AlbumTransferSeries_OfAnotherSeries_LeavesTheAlbumWhereItIs()
    {
        var other = new Series("Lucky Luke");
        var album = new Album(null, other, []);

        album.TransferSeries(new Series("Asterix"), new Series("Astérix"));

        Assert.Same(other, album.Series);
    }

    [Fact]
    public void AlbumTransferSeries_ToTheSameSeries_IsRefused()
    {
        var series = new Series("Astérix");
        var album = new Album(null, series, []);

        Assert.Throws<ArgumentException>(() => album.TransferSeries(series, series));
    }

    [Fact]
    public void MergeTemplateContributions_MovesTheRetainedContributionsOfTheOtherSeries_TheirIdentityKept()
    {
        var kept = SeriesCredited("Astérix", (_goscinny, ContributionRole.Scenarist));
        var merged = SeriesCredited("Asterix", (_uderzo, ContributionRole.Illustrator));
        var moved = Assert.Single(merged.TemplateContributions);

        kept.MergeTemplateContributions(merged, [.. kept.TemplateContributions.Select(c => c.Id), moved.Id]);

        Assert.Contains(moved, kept.TemplateContributions);
        Assert.Same(kept, moved.Series);
        Assert.Equal(kept.Id, moved.SeriesId);
        Assert.Empty(merged.TemplateContributions);
        Assert.Equivalent(
            new[] { (_goscinny.Id, ContributionRole.Scenarist), (_uderzo.Id, ContributionRole.Illustrator) },
            kept.TemplateContributions.Select(c => (c.AuthorId, c.Role)));
    }

    [Fact]
    public void MergeTemplateContributions_RemovesTheContributionsDiscarded_OfEitherSeries()
    {
        var kept = SeriesCredited("Astérix", (_goscinny, ContributionRole.Scenarist), (_morris, ContributionRole.Colorist));
        var merged = SeriesCredited("Asterix", (_uderzo, ContributionRole.Illustrator));
        var retained = kept.TemplateContributions.Single(c => c.AuthorId == _goscinny.Id);

        kept.MergeTemplateContributions(merged, [retained.Id]);

        Assert.Same(retained, Assert.Single(kept.TemplateContributions));
        // Discarded, the contribution of the other series stays with it, and is deleted with it.
        Assert.Single(merged.TemplateContributions);
    }

    [Fact]
    public void MergeTemplateContributions_KeepsACreditBothSeriesHoldOnce_ByTheContributionThatReplacesTheOtherOne()
    {
        var kept = SeriesCredited("Astérix", (_goscinny, ContributionRole.Scenarist));
        var merged = SeriesCredited("Asterix", (_goscinny, ContributionRole.Scenarist), (_uderzo, ContributionRole.Illustrator));
        var held = Assert.Single(kept.TemplateContributions);
        var duplicate = merged.TemplateContributions.Single(c => c.AuthorId == _goscinny.Id);

        Assert.Equal((duplicate.Id, held.Id), Assert.Single(kept.CreditsHeldOf(merged)));

        // A credit both hold is the same value on both sides: nothing is chosen of it, and it is kept whatever is retained.
        kept.MergeTemplateContributions(merged, []);

        Assert.Same(held, Assert.Single(kept.TemplateContributions));
    }

    [Fact]
    public void MergeTemplateContributions_OfAContributionOfNeitherSeries_IsRefusedAndChangesNothing()
    {
        var kept = SeriesCredited("Astérix", (_goscinny, ContributionRole.Scenarist));
        var merged = SeriesCredited("Asterix", (_uderzo, ContributionRole.Illustrator));
        var foreign = Assert.Single(SeriesCredited("Lucky Luke", (_morris, ContributionRole.Illustrator)).TemplateContributions);

        Assert.Throws<ArgumentException>(() => kept.MergeTemplateContributions(merged, [foreign.Id]));

        Assert.Single(kept.TemplateContributions);
        Assert.Single(merged.TemplateContributions);
    }

    [Fact]
    public void MergeTemplateContributions_WithItself_IsRefused()
    {
        var series = SeriesCredited("Astérix", (_goscinny, ContributionRole.Scenarist));

        Assert.Throws<ArgumentException>(() => series.MergeTemplateContributions(series, []));
    }

    private static Series SeriesCredited(string title, params (Author Author, ContributionRole Role)[] credits)
    {
        var series = new Series(title);
        series.SetTemplateContributions(credits);
        return series;
    }
}
