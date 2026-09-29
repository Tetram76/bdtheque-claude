using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// An entity that publishes editions (Éditeur).
/// </summary>
public sealed class Publisher : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Website { get; private set; }

    // Read-only from outside: a collection is only ever created through AddCollection, so it
    // can never be moved to another publisher (which would silently break the editions and
    // series templates that rely on it belonging to its publisher).
    private readonly List<PublisherCollection> _collections = [];
    public IReadOnlyCollection<PublisherCollection> Collections => _collections;

    // EF Core parameterless constructor
    private Publisher() { }

    public Publisher(string name)
    {
        SetName(name);
    }

    public void SetName(string name) =>
        Name = DomainText.Required(name, DomainRules.PublisherNameRequired, "A publisher must have a name.");

    public PublisherCollection AddCollection(string name)
    {
        var collection = new PublisherCollection(name, this);
        _collections.Add(collection);
        return collection;
    }

    public void SetWebsite(string? website)
    {
        var trimmed = DomainText.NullIfBlank(website);
        if (trimmed is not null
            && (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new DomainRuleViolationException(
                DomainRules.PublisherWebsiteInvalid, $"'{trimmed}' is not a valid absolute web URL (http or https required).");

        Website = trimmed;
    }
}
