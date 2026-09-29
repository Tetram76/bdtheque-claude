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

    public ICollection<PublisherCollection> Collections { get; private set; } = [];

    // EF Core parameterless constructor
    private Publisher() { }

    public Publisher(string name)
    {
        SetName(name);
    }

    public void SetName(string name) =>
        Name = DomainText.Required(name, DomainRules.PublisherNameRequired, "A publisher must have a name.");

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
