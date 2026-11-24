namespace Bdtheque.Frontend.Components.Merge;

/// <summary>What the merge presents of a record above its values (visuel/fusion/fusion-de-deux-fiches.md).</summary>
/// <param name="Name">The name of the record, as its list names it.</param>
/// <param name="Links">What is linked to it (« crédité sur 2 séries · 24 albums »), if anything.</param>
/// <param name="ModifiedAt">Its last modification.</param>
public sealed record MergeRecordHead(string Name, string? Links, DateTimeOffset ModifiedAt);
