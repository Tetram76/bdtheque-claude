namespace Bdtheque.Contracts.Admin;

/// <summary>A publisher collection as its administration form edits it.</summary>
/// <param name="PublisherVersion">
/// Version of the publisher the collection belongs to — the aggregate root, whose version guards
/// every write on its collections — sent back with any modification or deletion.
/// </param>
public sealed record PublisherCollectionForm(Guid Id, Guid PublisherId, string Name, uint PublisherVersion);

/// <param name="PublisherVersion">Version of the publisher the form was read at.</param>
public sealed record CreatePublisherCollectionRequest(string Name, uint PublisherVersion);

/// <param name="PublisherVersion">Version of the publisher the form was read at.</param>
public sealed record UpdatePublisherCollectionRequest(string Name, uint PublisherVersion);
