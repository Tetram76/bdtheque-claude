namespace Bdtheque.Frontend.Entries;

/// <summary>
/// A field the user changed since the record was read, with what they entered, to enter it again once the record is reloaded
/// (choix-implementation/socle/frontend.md, modifiée entre-temps).
/// </summary>
/// <param name="Value">What they entered; <c>null</c> or blank for a field they emptied.</param>
public sealed record EntryChange(string Field, string? Value)
{
    /// <summary>The records cited by a field, by their names.</summary>
    public static string Names(IEnumerable<Components.Controls.RecordOption> records) => string.Join(", ", records.Select(r => r.Label));
}
