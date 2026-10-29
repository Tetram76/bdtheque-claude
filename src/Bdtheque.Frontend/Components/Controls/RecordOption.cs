namespace Bdtheque.Frontend.Components.Controls;

/// <summary>A record a <see cref="RecordPicker"/> suggests, or has chosen.</summary>
/// <param name="Label">Its name, as the lists name it.</param>
/// <param name="Detail">What tells it apart from a namesake (e.g. the publisher of a collection, the parent of a universe).</param>
public sealed record RecordOption(Guid Id, string Label, string? Detail);
