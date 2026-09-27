using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A named sub-group within a publisher, used to classify editions (Collection éditeur).
/// A <see cref="PublisherCollection"/> cannot exist without its <see cref="Publisher"/>.
/// </summary>
public sealed class PublisherCollection : EntityBase
{
    public string Name { get; private set; } = string.Empty;

    public Guid PublisherId { get; private set; }
    public Publisher Publisher { get; private set; } = null!;

    // EF Core parameterless constructor
    private PublisherCollection() { }

    public PublisherCollection(string name, Publisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        SetName(name);
        Publisher = publisher;
        PublisherId = publisher.Id;
    }

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }
}
