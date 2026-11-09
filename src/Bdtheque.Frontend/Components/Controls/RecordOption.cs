namespace Bdtheque.Frontend.Components.Controls;

/// <summary>A record a <see cref="RecordPicker"/> suggests, or has chosen.</summary>
/// <param name="Label">Its name, as the lists name it.</param>
/// <param name="Detail">What tells it apart from a namesake (e.g. the publisher of a collection, the parent of a universe).</param>
/// <param name="Parent">
/// The record it belongs to, when two filters are linked (<see cref="Lists.LinkedFilters"/>): the publisher of a collection,
/// the series of an album; <c>null</c> for an album without series, or a record of another kind.
/// </param>
public sealed record RecordOption(Guid Id, string Label, string? Detail, RecordOption? Parent = null);
