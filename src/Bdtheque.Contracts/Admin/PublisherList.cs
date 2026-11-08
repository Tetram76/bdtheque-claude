namespace Bdtheque.Contracts.Admin;

/// <summary>A publisher as a row of the list of the administration presents it (visuel/listes/liste-par-liste.md).</summary>
/// <param name="Website">Where the publisher leads, having no record of its own (fonctionnel/structure-de-l-application.md).</param>
/// <param name="Collections">The names of its collections, in their order: their number is the column of the list.</param>
/// <param name="EditionCount">
/// Its editions in the collection, an edition targeted by a purchase intent being none
/// (fonctionnel/regles-metier/intention-d-achat.md).
/// </param>
/// <param name="Version">Version of the publisher, sent back with its deletion.</param>
public sealed record PublisherAdminListItem(
    Guid Id,
    string Name,
    string? Website,
    IReadOnlyList<string> Collections,
    int EditionCount,
    DateTimeOffset ModifiedAt,
    uint Version);
