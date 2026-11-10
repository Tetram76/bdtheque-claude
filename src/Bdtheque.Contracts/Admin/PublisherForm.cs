namespace Bdtheque.Contracts.Admin;

/// <summary>
/// A publisher as its administration form edits it (visuel/saisie-editeur/contenu.md), with its collections, which are entered
/// and saved with it, and the dates of the record, which the form presents.
/// </summary>
/// <param name="Collections">Its collections, by name, each with what references it.</param>
/// <param name="Version">
/// Version of the publisher, the aggregate root of its collections, sent back with any modification or deletion.
/// </param>
public sealed record PublisherForm(
    Guid Id,
    string Name,
    string? Website,
    IReadOnlyList<PublisherCollectionUse> Collections,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    uint Version);

/// <summary>
/// A collection of a publisher as the form of its publisher presents it: what references it, editions and series templates,
/// which forbids its removal (fonctionnel/regles-metier/suppression-des-entites.md).
/// </summary>
/// <param name="EditionCount">The editions of the collection, whether owned or targeted by a purchase intent.</param>
/// <param name="SeriesCount">The series whose new editions are proposed the collection.</param>
public sealed record PublisherCollectionUse(Guid Id, string Name, int EditionCount, int SeriesCount);

/// <summary>
/// Everything the user edits on a publisher, sent to create it, and sent whole to modify it: its collections are replaced
/// by those listed, a collection of the publisher absent from the list being removed.
/// </summary>
public sealed record PublisherContent(string Name, string? Website, IReadOnlyList<PublisherCollectionContent> Collections);

/// <summary>A collection as the form of its publisher sends it.</summary>
/// <param name="Id">The collection; <c>null</c> for a collection added by the form, created with the publisher's save.</param>
public sealed record PublisherCollectionContent(Guid? Id, string Name);

/// <param name="Version">Version of the publisher the form was read at.</param>
public sealed record UpdatePublisherRequest(PublisherContent Content, uint Version);
