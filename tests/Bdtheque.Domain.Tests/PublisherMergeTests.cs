using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// The merge of two publishers (fonctionnel/regles-metier/fusion-des-fiches.md): the publisher that remains takes the collections
/// of the other one, two homonymous collections becoming one, and every edition and series of either designates it, with the
/// collection that remains of theirs.
/// </summary>
public sealed class PublisherMergeTests
{
    [Fact]
    public void Absorb_MovesTheCollectionsOfTheMergedPublisherToTheKeptOne_KeepingTheirIdentity()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var own = kept.AddCollection("Repérages");
        var taken = merged.AddCollection("Aire Libre");

        var merge = kept.Absorb(merged, []);

        Assert.Equal([own, taken], kept.Collections);
        Assert.Empty(merged.Collections);
        Assert.Equal((kept.Id, kept), (taken.PublisherId, taken.Publisher));
        Assert.Empty(merge.Replaced);
    }

    [Fact]
    public void Absorb_OfHomonymousCollections_KeepsTheOneThatRemainsAndRemovesTheOther()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var ofKept = kept.AddCollection("aire libre");
        var ofMerged = merged.AddCollection("Aire Libre");

        // The collection of the merged publisher gives its name: it is the one that remains.
        var merge = kept.Absorb(merged, [(Remaining: ofMerged, Removed: ofKept)]);

        Assert.Same(ofMerged, Assert.Single(kept.Collections));
        Assert.Equal(kept.Id, ofMerged.PublisherId);
        Assert.Empty(merged.Collections);
        Assert.Same(ofMerged, Assert.Single(merge.Replaced).Value);
        Assert.Equal(ofKept.Id, merge.Replaced.Single().Key);
    }

    [Fact]
    public void Absorb_OfItself_IsRefused()
    {
        var publisher = new Publisher("Dupuis");

        Assert.Throws<ArgumentException>(() => publisher.Absorb(publisher, []));
    }

    [Fact]
    public void Absorb_OfAPairNotOneCollectionOfEachPublisher_IsRefused()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var first = kept.AddCollection("Aire Libre");
        var second = kept.AddCollection("Repérages");

        Assert.Throws<ArgumentException>(() => kept.Absorb(merged, [(first, second)]));
        Assert.Equal(2, kept.Collections.Count);
    }

    [Fact]
    public void EditionsOfTheMergedPublisher_PassToTheKeptOne_WithTheCollectionThatRemains()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var ofKept = kept.AddCollection("Aire Libre");
        var ofMerged = merged.AddCollection("aire libre");
        var taken = merged.AddCollection("Repérages");
        var album = new Album("Le Pouvoir des innocents", null);
        var inHomonym = new Edition(album, merged);
        inHomonym.SetPublisher(merged, ofMerged);
        var inTaken = new Edition(album, merged);
        inTaken.SetPublisher(merged, taken);
        var withoutCollection = new Edition(album, merged);
        var ofTheKept = new Edition(album, kept);
        ofTheKept.SetPublisher(kept, ofKept);

        album.TransferPublisher(kept.Absorb(merged, [(Remaining: ofKept, Removed: ofMerged)]));

        Assert.Equal(
            [(kept, ofKept), (kept, taken), (kept, (PublisherCollection?)null), (kept, ofKept)],
            album.Editions.Select(e => (e.Publisher, e.PublisherCollection)));
        Assert.All(album.Editions, e => Assert.Equal((e.Publisher.Id, e.PublisherCollection?.Id), (e.PublisherId, e.PublisherCollectionId)));
    }

    [Fact]
    public void EditionsOfTheKeptPublisher_InTheCollectionItLoses_PassToTheCollectionThatRemains()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var ofKept = kept.AddCollection("aire libre");
        var ofMerged = merged.AddCollection("Aire Libre");
        var album = new Album("Le Pouvoir des innocents", null);
        var edition = new Edition(album, kept);
        edition.SetPublisher(kept, ofKept);

        album.TransferPublisher(kept.Absorb(merged, [(Remaining: ofMerged, Removed: ofKept)]));

        Assert.Equal((kept, ofMerged), (edition.Publisher, edition.PublisherCollection));
    }

    [Fact]
    public void EditionsOfAnotherPublisher_AreLeftAsTheyAre()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var other = new Publisher("Dargaud");
        var collection = other.AddCollection("Poussière d'étoiles");
        var album = new Album("Blacksad", null);
        var edition = new Edition(album, other);
        edition.SetPublisher(other, collection);

        album.TransferPublisher(kept.Absorb(merged, []));

        Assert.Equal((other, collection), (edition.Publisher, edition.PublisherCollection));
    }

    [Fact]
    public void TheTemplateOfASeries_PassesToTheKeptPublisher_WithTheCollectionThatRemains()
    {
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var ofKept = kept.AddCollection("Aire Libre");
        var ofMerged = merged.AddCollection("aire libre");
        var series = new Series("Le Pouvoir des innocents");
        series.SetTemplate(merged, ofMerged);
        var withoutCollection = new Series("Spirou et Fantasio");
        withoutCollection.SetTemplate(merged, null);
        var withoutPublisher = new Series("Blacksad");

        var merge = kept.Absorb(merged, [(Remaining: ofKept, Removed: ofMerged)]);
        series.TransferPublisher(merge);
        withoutCollection.TransferPublisher(merge);
        withoutPublisher.TransferPublisher(merge);

        Assert.Equal((kept.Id, ofKept.Id), (series.TemplatePublisherId, series.TemplatePublisherCollectionId));
        Assert.Equal((kept.Id, (Guid?)null), (withoutCollection.TemplatePublisherId, withoutCollection.TemplatePublisherCollectionId));
        Assert.Equal(((Guid?)null, (Guid?)null), (withoutPublisher.TemplatePublisherId, withoutPublisher.TemplatePublisherCollectionId));
    }

    [Fact]
    public void ARecordWhoseCollectionIsNotLoaded_IsRefusedRatherThanLosingIt()
    {
        // A collection only known by its identifier would be lost by the transfer: a programming error, never a user's.
        var kept = new Publisher("Dupuis");
        var merged = new Publisher("dupuis");
        var merge = kept.Absorb(merged, []);

        Assert.Throws<InvalidOperationException>(() => merge.CollectionReplacing(Guid.CreateVersion7(), null));
    }
}
