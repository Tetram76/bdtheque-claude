using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Components.Merge;

/// <summary>
/// The texts every merge shares (visuel/fusion/fusion-de-deux-fiches.md), written once, so that they cannot diverge from one type
/// of records to another: what is left to choose, and the confirmation, in the future, the confirmation preceding what it says.
/// </summary>
public sealed class MergeTexts(IStringLocalizer<MergeTexts> localizer)
{
    /// <summary>« 2 données restent à choisir », « Tout est choisi ».</summary>
    public string PendingText(int pending) => pending switch
    {
        0 => localizer["Pending.None"].Value,
        1 => localizer["Pending.One"].Value,
        _ => localizer["Pending.Other", pending].Value,
    };

    /// <summary>« 2 à choisir », on a smartphone.</summary>
    public string PendingShortText(int pending) => pending == 0 ? localizer["Pending.None"].Value : localizer["Pending.Short", pending].Value;

    /// <summary>« La fusion est définitive. », which opens the confirmation as it opens that of a deletion.</summary>
    public string Definitive => localizer["Definitive"].Value;

    /// <summary>« Les deux fiches n'en feront plus qu'une : Gotlib. »</summary>
    public string OneRecord(string name) => localizer["OneRecord", name].Value;

    /// <summary>« Fusionner définitivement ».</summary>
    public string ConfirmLabel => localizer["Confirm"].Value;

    /// <summary>
    /// What the merged record will be linked to, in the order of the entity types (« les 2 séries et les 26 albums des deux
    /// fiches », « les 3 séries, les 2 collections et l'édition des deux fiches »), none when nothing is linked to either record.
    /// </summary>
    public string? Linked(IEnumerable<MergeLinkCount> links)
    {
        var parts = links.Where(l => l.Total > 0)
            .Select(l => l.Total == 1 ? localizer[$"Linked.One.{l.Kind}"].Value : localizer[$"Linked.Other.{l.Kind}", l.Total].Value)
            .ToList();
        return parts.Count == 0 ? null : localizer["Linked", Enumerate(parts)].Value;
    }

    /// <summary>« a », « a et b », « a, b et c ».</summary>
    public string Enumerate(IReadOnlyList<string> parts) => parts.Count switch
    {
        0 => "",
        1 => parts[0],
        _ => localizer["Last", string.Join(localizer["Comma"].Value, parts.Take(parts.Count - 1)), parts[^1]].Value,
    };

    /// <summary>
    /// The elements of a list the merge discards, which the confirmation says it will delete (visuel/fusion/fusion-de-deux-fiches.md):
    /// a line per type, in the order of the entity types, and per record that held them, the first one first
    /// (« 1 contribution : Uderzo (Dessin), de Astérix », « 2 genres : Humour et Aventure, de Asterix »).
    /// </summary>
    /// <param name="discarded">Each element discarded: its type (a genre, a universe or a contribution), the record that held it, its name.</param>
    /// <param name="recordName">The name of a merged record.</param>
    public IEnumerable<string> Discarded(IEnumerable<(EntityKind Kind, MergeSide Holder, string Name)> discarded, Func<MergeSide, string> recordName) =>
        discarded
            .GroupBy(d => (d.Kind, d.Holder))
            .OrderBy(g => EntityKindOrder.Of(g.Key.Kind)).ThenBy(g => g.Key.Holder)
            .Select(g => DiscardedOfARecord(g.Key.Kind, [.. g.Select(d => d.Name)], recordName(g.Key.Holder)));

    private string DiscardedOfARecord(EntityKind kind, IReadOnlyList<string> names, string record)
    {
        var listed = Enumerate(names);
        return names.Count == 1
            ? localizer[$"Discarded.One.{kind}", listed, record].Value
            : localizer[$"Discarded.Other.{kind}", names.Count, listed, record].Value;
    }

    /// <summary>Whether a type of records is named by the texts of the merge (a test pins every type a merge links).</summary>
    public bool Names(EntityKind kind) => !localizer[$"Linked.One.{kind}"].ResourceNotFound;
}
