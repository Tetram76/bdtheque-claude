namespace Bdtheque.Frontend.Components.Controls;

/// <summary>
/// What a <see cref="Field"/> gives the control it labels, to be set on it: its identifier, the texts that describe it,
/// and whether it is in error.
/// </summary>
/// <param name="Id">The identifier of the control, which its label designates.</param>
/// <param name="DescribedBy">The identifiers of its error and help, for <c>aria-describedby</c>; <c>null</c> without any.</param>
/// <param name="Invalid"><c>"true"</c> for <c>aria-invalid</c> when the field is in error; <c>null</c> otherwise, which leaves the attribute out.</param>
public sealed record FieldContext(string Id, string? DescribedBy, string? Invalid);
