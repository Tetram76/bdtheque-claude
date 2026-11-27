using Microsoft.AspNetCore.Components;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// A datum of a record being entered, as the user sees it: a field, or a list a line presents (the authors of a role, the
/// genres). Written once per form, for the three things that compare what is entered to what was read
/// (choix-implementation/socle/frontend.md, fiches de saisie): the mark of a datum modified, « Voir mes changements », and
/// what the user had entered, offered again once the record is reloaded after it was modified meanwhile.
/// </summary>
/// <param name="Key">Its identifier in the form.</param>
/// <param name="Label">Its name, that of its field.</param>
/// <param name="Value">What tells two states of the datum apart: equal values, the datum is the same.</param>
/// <param name="Text">What the user entered in it, as they read it; <c>null</c> or blank for a datum emptied.</param>
/// <param name="Take">Puts the datum of the first entry into the second.</param>
/// <param name="Shown">
/// Whether the form presents the datum in an entry (e.g. the range of an omnibus only): what was entered is offered again only
/// when the datum was presented then and is now; always presented by default.
/// </param>
public sealed record EntryDatum<TEntry>(
    string Key, string Label, Func<TEntry, string?> Value, Func<TEntry, string?> Text, Action<TEntry, TEntry> Take,
    Func<TEntry, bool>? Shown = null);

/// <summary>
/// What a control presents of its datum (<see cref="EntryTracker{TEntry}.State"/>): whether it is modified, and what the user
/// had entered in it before the record was reloaded, which they take or reject.
/// </summary>
/// <param name="Redo">What they had entered, still to take or reject; <c>null</c> for nothing (an empty text for a datum emptied).</param>
public sealed record EntryDatumState(bool Modified, string? Redo, EventCallback Take, EventCallback Reject)
{
    public static readonly EntryDatumState None = new(false, null, default, default);
}

/// <summary>
/// The data of a record being entered, compared on what is entered against what was read, never on the history of the typing
/// (visuel/saisie-album/contenu.md, données modifiées, decisions of the user, #355): a datum set back to what was read is no
/// longer modified. Once the record is reloaded after it was modified meanwhile (#329), the entry left behind offers each
/// datum the user had changed, under it alone where it differs from the record reloaded, until it is taken (by the button or by
/// hand) or rejected. The page holds the records: the tracker reads them each time, as the page replaces them.
/// </summary>
public sealed class EntryTracker<TEntry>(Func<TEntry?> read, Func<TEntry?> entry, IReadOnlyList<EntryDatum<TEntry>> data)
    where TEntry : class
{
    private readonly HashSet<string> _rejected = [];
    private (TEntry Read, TEntry Entry)? _abandoned;

    public bool IsModified(string key) =>
        read() is { } r && entry() is { } e && Datum(key) is var datum && datum.Value(r) != datum.Value(e);

    /// <summary>Each datum modified, with what was entered in it, in the order of the form.</summary>
    public IReadOnlyList<EntryChange> Changes() =>
        read() is { } r && entry() is { } e
            ? [.. data.Where(d => d.Value(r) != d.Value(e)).Select(d => new EntryChange(d.Label, d.Text(e)))]
            : [];

    /// <summary>
    /// Keeps the entry as it is, and the record as it was read, before the page reloads the record: the data the user changed
    /// are offered again on the record reloaded. A new abandon replaces the previous one, once something was entered since.
    /// </summary>
    public void Abandon()
    {
        if (read() is not { } r || entry() is not { } e)
            return;
        // Reloaded again with nothing entered since the last reading (e.g. « Recharger » clicked twice): what was entered before
        // is still to offer, rather than the record reloaded.
        if (_abandoned is not null && data.All(d => d.Value(r) == d.Value(e)))
            return;
        _abandoned = (r, e);
        _rejected.Clear();
    }

    /// <summary>The entry left behind at the reload, if any.</summary>
    public TEntry? Entered => _abandoned?.Entry;

    /// <summary>What the user had entered in the datum, still to take or reject; <c>null</c> for nothing.</summary>
    public string? Redo(string key) => IsPending(Datum(key)) ? Datum(key).Text(_abandoned!.Value.Entry) ?? "" : null;

    /// <summary>Whether a datum entered before the reload is still to take or reject.</summary>
    public bool HasRedo => data.Any(IsPending);

    public void Take(string key)
    {
        if (_abandoned is { } abandoned && entry() is { } e)
            Datum(key).Take(abandoned.Entry, e);
    }

    public void Reject(string key) => _rejected.Add(key);

    /// <summary>What the control of the datum presents, its buttons rendering the page anew.</summary>
    public EntryDatumState State(string key, IHandleEvent receiver) => new(
        IsModified(key), Redo(key),
        EventCallback.Factory.Create(receiver, () => Take(key)), EventCallback.Factory.Create(receiver, () => Reject(key)));

    private EntryDatum<TEntry> Datum(string key) =>
        data.FirstOrDefault(d => d.Key == key) ?? throw new ArgumentException($"The form has no datum « {key} ».", nameof(key));

    // Changed by the user before the reload, different from the record reloaded, and not entered again since.
    private bool IsPending(EntryDatum<TEntry> datum) =>
        _abandoned is { } abandoned && read() is { } r && entry() is { } e && !_rejected.Contains(datum.Key)
        && (datum.Shown is null || (datum.Shown(abandoned.Entry) && datum.Shown(e)))
        && datum.Value(abandoned.Entry) is var entered && entered != datum.Value(abandoned.Read)
        && entered != datum.Value(r) && entered != datum.Value(e);
}
