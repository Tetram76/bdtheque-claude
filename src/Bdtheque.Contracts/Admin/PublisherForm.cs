namespace Bdtheque.Contracts.Admin;

/// <summary>A publisher as its administration form edits it.</summary>
/// <param name="Version">Version of the publisher, sent back with any modification or deletion.</param>
public sealed record PublisherForm(Guid Id, string Name, string? Website, uint Version);

public sealed record CreatePublisherRequest(string Name, string? Website);

/// <param name="Version">Version of the publisher the form was read at.</param>
public sealed record UpdatePublisherRequest(string Name, string? Website, uint Version);
