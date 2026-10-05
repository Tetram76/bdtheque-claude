using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>An author credited with a role, on an album or on the template of a series.</summary>
public sealed record ContributionContent(Guid AuthorId, ContributionRole Role);
