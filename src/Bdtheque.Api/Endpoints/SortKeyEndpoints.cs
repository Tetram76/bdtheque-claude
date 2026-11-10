using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The sort keys the domain computes, of a title (<c>/admin/sort-keys/title</c>) or of the names of an author
/// (<c>/admin/sort-keys/author</c>): an entry form shows them while the title or the names are typed, without computing them
/// itself (fonctionnel/presentation/tri-et-navigation-par-initiale.md).
/// </summary>
internal static class SortKeyEndpoints
{
    public static void MapSortKeys(this RouteGroupBuilder admin)
    {
        admin.MapGet("/sort-keys/title", (string? title) =>
            new TitleSortKey(string.IsNullOrWhiteSpace(title) ? null : TitleSortKeyCalculator.Compute(title)));
        admin.MapGet("/sort-keys/author", (string? lastName, string? firstName, string? pseudonym) =>
            AuthorIdentity.Of(lastName, firstName, pseudonym) is { } identity
                ? new AuthorSortKey(identity.DisplayName, identity.SortKey, identity.NavigationEntry)
                : new AuthorSortKey(null, null, null));
    }
}
