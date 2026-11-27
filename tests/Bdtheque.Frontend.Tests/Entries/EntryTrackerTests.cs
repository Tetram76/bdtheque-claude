using Bdtheque.Frontend.Entries;

namespace Bdtheque.Frontend.Tests.Entries;

/// <summary>
/// What an entry form tells of the data the user changed (visuel/saisie-album/contenu.md, données modifiées) and of the
/// entry they left behind when the record was reloaded after it was modified meanwhile (#329): data compared on what is
/// entered against what was read, never on the history of the typing.
/// </summary>
public sealed class EntryTrackerTests
{
    private sealed class Entry
    {
        public string? Title { get; set; }

        public int? Year { get; set; }

        public Entry Copy() => (Entry)MemberwiseClone();
    }

    private Entry _read = new() { Title = "Les Bijoux", Year = 1963 };
    private Entry _entry = new() { Title = "Les Bijoux", Year = 1963 };

    private EntryTracker<Entry> Tracker() => new(() => _read, () => _entry,
    [
        new EntryDatum<Entry>("title", "Titre", e => e.Title, e => e.Title, (from, to) => to.Title = from.Title),
        new EntryDatum<Entry>("year", "Année", e => e.Year?.ToString(), e => e.Year?.ToString(), (from, to) => to.Year = from.Year),
    ]);

    [Fact]
    public void Datum_IsModified_WhileItDiffersFromWhatWasRead()
    {
        var tracker = Tracker();

        _entry.Title = "Les Bijoux de la Castafiore";
        Assert.True(tracker.IsModified("title"));
        Assert.False(tracker.IsModified("year"));

        _entry.Title = "Les Bijoux";
        Assert.False(tracker.IsModified("title"));
    }

    [Fact]
    public void Changes_AreTheModifiedData_WithWhatWasEntered()
    {
        var tracker = Tracker();
        _entry.Year = 1964;

        Assert.Equal([new EntryChange("Année", "1964")], tracker.Changes());
    }

    [Fact]
    public void Abandoned_Entry_OffersOnlyWhatTheUserChanged_AndWhatDiffersFromTheRecordReloaded()
    {
        var tracker = Tracker();
        _entry.Title = "Le Lotus";
        _entry.Year = 1964;
        // Reloaded: another modification set the year the user typed, and the title of the record changed nothing.
        var reloaded = new Entry { Title = "Les Bijoux", Year = 1964 };

        tracker.Abandon();
        _read = reloaded;
        _entry = reloaded.Copy();

        Assert.Equal("Le Lotus", tracker.Redo("title"));
        Assert.Null(tracker.Redo("year"));
        Assert.True(tracker.HasRedo);
    }

    [Fact]
    public void Abandoned_Entry_DoesNotOffer_ADatumTheUserLeft_ThatTheOtherModificationChanged()
    {
        var tracker = Tracker();
        _entry.Title = "Le Lotus";

        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1970 };
        _entry = _read.Copy();

        Assert.Null(tracker.Redo("year"));
    }

    [Fact]
    public void Taking_ADatum_PutsWhatWasEntered_AndTheLineDisappears()
    {
        var tracker = Tracker();
        _entry.Title = "Le Lotus";
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        tracker.Take("title");

        Assert.Equal("Le Lotus", _entry.Title);
        Assert.Null(tracker.Redo("title"));
        Assert.True(tracker.IsModified("title"));
        Assert.False(tracker.HasRedo);
    }

    [Fact]
    public void Taking_ADatum_SettlesIt_EvenWhenTheTakeMergesWhatWasEntered_WithWhatChangedSince()
    {
        // e.g. the additions and removals of the collections of a publisher, taken into a list changed since the reload.
        var tracker = new EntryTracker<Entry>(() => _read, () => _entry,
        [
            new EntryDatum<Entry>("title", "Titre", e => e.Title, e => e.Title, (from, to) => to.Title = $"{to.Title} + {from.Title}"),
        ]);
        _entry.Title = "Le Lotus";
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        tracker.Take("title");
        _entry.Title = "Autre chose";

        Assert.Null(tracker.Redo("title"));
        Assert.False(tracker.HasRedo);
    }

    [Fact]
    public void Typing_WhatWasEntered_ByHand_HasTheLineDisappear()
    {
        var tracker = Tracker();
        _entry.Title = "Le Lotus";
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        _entry.Title = "Le Lotus";

        Assert.Null(tracker.Redo("title"));
    }

    [Fact]
    public void Rejecting_ADatum_LeavesTheField_AndTheLineDisappears()
    {
        var tracker = Tracker();
        _entry.Title = "Le Lotus";
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        tracker.Reject("title");

        Assert.Equal("Les Bijoux", _entry.Title);
        Assert.Null(tracker.Redo("title"));
        Assert.False(tracker.HasRedo);
    }

    [Fact]
    public void Reloading_Again_WithNothingEnteredSince_KeepsWhatWasEnteredBefore()
    {
        var tracker = Tracker();
        _entry.Title = "Le Lotus";
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        Assert.Equal("Le Lotus", tracker.Redo("title"));
    }

    [Fact]
    public void Datum_NotShown_ThenOrNow_IsNotOfferedAgain()
    {
        // e.g. the range of an omnibus, once the album is no longer one, or was not when entered.
        var tracker = new EntryTracker<Entry>(() => _read, () => _entry,
        [
            new EntryDatum<Entry>("title", "Titre", e => e.Title, e => e.Title, (from, to) => to.Title = from.Title, e => e.Year is not null),
        ]);
        _entry.Title = "Le Lotus";
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = null };
        _entry = _read.Copy();

        Assert.Null(tracker.Redo("title"));
        Assert.False(tracker.HasRedo);

        _entry.Year = 1963;

        Assert.Equal("Le Lotus", tracker.Redo("title"));
    }

    [Fact]
    public void Redo_ShowsAnEmptiedDatum_AsEmptied()
    {
        var tracker = Tracker();
        _entry.Title = null;
        tracker.Abandon();
        _read = new Entry { Title = "Les Bijoux", Year = 1963 };
        _entry = _read.Copy();

        Assert.Equal("", tracker.Redo("title"));
    }
}
