namespace Bdtheque.Frontend.Components.Merge;

/// <summary>What the merge presents of a record above its values (visuel/fusion/fusion-de-deux-fiches.md).</summary>
/// <param name="Name">The name of the record, as its list names it.</param>
/// <param name="Links">What is linked to it (« crédité sur 2 séries · 24 albums »), if anything.</param>
/// <param name="ModifiedAt">
/// Its last modification, with what is linked to it; none above the values of a block that only names the record (« Collections
/// homonymes »), the block of its data having already said them.
/// </param>
public sealed record MergeRecordHead(string Name, string? Links, DateTimeOffset? ModifiedAt);
