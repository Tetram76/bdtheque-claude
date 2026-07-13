using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// An entity that publishes editions (Éditeur).
/// </summary>
public sealed class Publisher : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Website { get; private set; }

    public ICollection<PublisherCollection> Collections { get; private set; } = [];

    // EF Core parameterless constructor
    private Publisher() { }

    public Publisher(string name)
    {
        SetName(name);
    }

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void SetWebsite(string? website) =>
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
}
