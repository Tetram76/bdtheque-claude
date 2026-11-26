using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A content category that can be applied to albums and series (Genre).
/// </summary>
public sealed class Genre : EntityBase, IAggregateRoot
{
    public string Label { get; private set; } = null!;

    // Stored rather than computed at read time, so that the database can filter the list of the genres on it and index it
    // (choix-implementation/api/navigation-par-initiale-mise-en-oeuvre.md). Derived from the label, as sorted.
    public string NavigationEntry { get; private set; } = null!;

    // EF Core parameterless constructor
    private Genre() { }

    public Genre(string label)
    {
        SetLabel(label);
    }

    public void SetLabel(string label)
    {
        Label = DomainText.Required(label, DomainRules.GenreLabelRequired, "A genre must have a label.", this, nameof(Label));
        NavigationEntry = NavigationEntryCalculator.Compute(Label);
    }
}
