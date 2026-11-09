namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The sort key the domain computes from a title, which an entry form shows in its automatic mode while the title is
/// typed (fonctionnel/presentation/tri-et-navigation-par-initiale.md#calcul-et-stockage-de-la-clé-de-tri).
/// </summary>
/// <param name="SortKey"><c>null</c> for a blank title, which has none.</param>
public sealed record TitleSortKey(string? SortKey);
