using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The merge of two albums (fonctionnel/regles-metier/fusion-des-fiches.md): the editions and purchase intents of the other album
/// pass to the one that remains, with their identity, an intent on the whole album being withdrawn once the album has an edition;
/// the album keeps, credit by credit, the contributions retained of both, a credit both albums hold being kept once.
/// </summary>
public sealed class AlbumMergeTests
{
    private static readonly Publisher Casterman = new("Casterman");
    private readonly Author _herge = new("Hergé", null, null);
    private readonly Author _jacobs = new("Jacobs", "Edgar P.", null);

    [Fact]
    public void TakeEditionsOf_MovesTheEditionsAndTheirIntents_TheirIdentityKept()
    {
        var kept = new Album("Le Lotus bleu", null);
        var merged = new Album("Lotus bleu", null);
        var owned = new Edition(merged, Casterman);
        merged.RecordAcquisition(owned, AcquisitionMode.Purchase);
        var targeted = new Edition(merged, Casterman);
        var intent = merged.AddPurchaseIntent(targeted);

        kept.TakeEditionsOf(merged);

        Assert.Equivalent(new[] { owned, targeted }, kept.Editions);
        Assert.All(kept.Editions, e => Assert.Equal((kept, kept.Id), (e.Album, e.AlbumId)));
        Assert.Same(intent, Assert.Single(kept.PurchaseIntents));
        Assert.Equal((kept, kept.Id, targeted.Id), (intent.Album, intent.AlbumId, intent.EditionId));
        Assert.Empty(merged.Editions);
        Assert.Empty(merged.PurchaseIntents);
    }

    [Fact]
    public void TakeEditionsOf_WithdrawsTheIntentOnTheWholeAlbum_OnceTheAlbumHasAnEdition()
    {
        var kept = new Album("Le Lotus bleu", null);
        kept.AddPurchaseIntent();
        var merged = new Album("Lotus bleu", null);
        merged.RecordAcquisition(new Edition(merged, Casterman), AcquisitionMode.Purchase);

        Assert.Equal([kept.PurchaseIntents.Single()], Album.PurchaseIntentsWithdrawnByMerge(kept, merged));
        kept.TakeEditionsOf(merged);

        Assert.Single(kept.Editions);
        Assert.Empty(kept.PurchaseIntents);
    }

    [Fact]
    public void TakeEditionsOf_LeavesTheIntentOnTheWholeOtherAlbum_DeletedWithIt_OnceTheAlbumHasAnEdition()
    {
        var kept = new Album("Le Lotus bleu", null);
        kept.RecordAcquisition(new Edition(kept, Casterman), AcquisitionMode.Purchase);
        var merged = new Album("Lotus bleu", null);
        var intent = merged.AddPurchaseIntent();

        Assert.Equal([intent], Album.PurchaseIntentsWithdrawnByMerge(kept, merged));
        kept.TakeEditionsOf(merged);

        Assert.Empty(kept.PurchaseIntents);
        Assert.Same(intent, Assert.Single(merged.PurchaseIntents));
    }

    [Fact]
    public void TakeEditionsOf_PassesTheIntentOnTheWholeOtherAlbum_ToAnAlbumWithoutEdition()
    {
        var kept = new Album("Le Lotus bleu", null);
        var merged = new Album("Lotus bleu", null);
        var intent = merged.AddPurchaseIntent();

        Assert.Empty(Album.PurchaseIntentsWithdrawnByMerge(kept, merged));
        kept.TakeEditionsOf(merged);

        Assert.Same(intent, Assert.Single(kept.PurchaseIntents));
        Assert.Equal(kept.Id, intent.AlbumId);
        Assert.Empty(merged.PurchaseIntents);
    }

    [Fact]
    public void TakeEditionsOf_KeepsAnIntentOnTheWholeAlbumBothAlbumsHold_Once()
    {
        var kept = new Album("Le Lotus bleu", null);
        var held = kept.AddPurchaseIntent();
        var merged = new Album("Lotus bleu", null);
        merged.AddPurchaseIntent();

        Assert.Empty(Album.PurchaseIntentsWithdrawnByMerge(kept, merged));
        kept.TakeEditionsOf(merged);

        // The other one stays with its album, deleted with it: nothing is lost, the wish being the same.
        Assert.Same(held, Assert.Single(kept.PurchaseIntents));
        Assert.Single(merged.PurchaseIntents);
    }

    [Fact]
    public void TakeEditionsOf_WithItself_IsRefused()
    {
        var album = new Album("Le Lotus bleu", null);

        DomainAssert.Throws<ArgumentException>(() => album.TakeEditionsOf(album));
    }

    [Fact]
    public void MergeContributions_MovesTheRetainedContributionsOfTheOtherAlbum_TheirIdentityKept()
    {
        var kept = new Album("Le Lotus bleu", null, [(_herge, ContributionRole.Scenarist)]);
        var merged = new Album("Lotus bleu", null, [(_jacobs, ContributionRole.Colorist)]);
        var moved = Assert.Single(merged.Contributions);

        kept.MergeContributions(merged, [.. kept.Contributions.Select(c => c.Id), moved.Id]);

        Assert.Contains(moved, kept.Contributions);
        Assert.Equal((kept, kept.Id), (moved.Album, moved.AlbumId));
        Assert.Empty(merged.Contributions);
    }

    [Fact]
    public void MergeContributions_RemovesTheContributionsDiscarded_AndKeepsACreditBothHoldOnce()
    {
        var kept = new Album("Le Lotus bleu", null, [(_herge, ContributionRole.Scenarist), (_jacobs, ContributionRole.Colorist)]);
        var merged = new Album("Lotus bleu", null, [(_herge, ContributionRole.Scenarist), (_jacobs, ContributionRole.Illustrator)]);
        var held = kept.Contributions.Single(c => c.Role == ContributionRole.Scenarist);
        var duplicate = merged.Contributions.Single(c => c.Role == ContributionRole.Scenarist);

        Assert.Equal((duplicate.Id, held.Id), Assert.Single(kept.CreditsHeldOf(merged)));
        kept.MergeContributions(merged, []);

        Assert.Same(held, Assert.Single(kept.Contributions));
    }

    [Fact]
    public void MergeContributions_OfAContributionOfNeitherAlbum_IsRefusedAndChangesNothing()
    {
        var kept = new Album("Le Lotus bleu", null, [(_herge, ContributionRole.Scenarist)]);
        var merged = new Album("Lotus bleu", null, [(_jacobs, ContributionRole.Colorist)]);
        var foreign = Assert.Single(new Album("Tintin au Tibet", null, [(_herge, ContributionRole.Illustrator)]).Contributions);

        DomainAssert.Throws<ArgumentException>(() => kept.MergeContributions(merged, [foreign.Id]));

        Assert.Single(kept.Contributions);
        Assert.Single(merged.Contributions);
    }
}
