using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A content category that can be applied to albums and series (Genre).
/// </summary>
public sealed class Genre : EntityBase
{
    public string Label { get; private set; } = string.Empty;

    // EF Core parameterless constructor
    private Genre() { }

    public Genre(string label)
    {
        SetLabel(label);
    }

    public void SetLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label.Trim();
    }
}
