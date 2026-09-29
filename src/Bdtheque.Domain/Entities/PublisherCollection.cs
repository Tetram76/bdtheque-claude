using Bdtheque.Domain.Common;
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

    // Only reachable through Publisher.AddCollection, the single place that registers it with
    // its publisher.
    internal PublisherCollection(string name, Publisher publisher)
    {
        SetName(name);
        Publisher = publisher;
        PublisherId = publisher.Id;
    }

    public void SetName(string name) =>
        Name = DomainText.Required(name, DomainRules.PublisherCollectionNameRequired, "A publisher collection must have a name.");
}
