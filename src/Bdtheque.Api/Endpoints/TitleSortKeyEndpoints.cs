using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The sort key of a title (<c>/admin/sort-keys/title</c>), computed by the domain alone: an entry form shows it in its
/// automatic mode while the title is typed, without computing it itself
/// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#calcul-et-stockage-de-la-clé-de-tri).
/// </summary>
internal static class TitleSortKeyEndpoints
{
    public static void MapTitleSortKeys(this RouteGroupBuilder admin) =>
        admin.MapGet("/sort-keys/title", (string? title) =>
            new TitleSortKey(string.IsNullOrWhiteSpace(title) ? null : TitleSortKeyCalculator.Compute(title)));
}
