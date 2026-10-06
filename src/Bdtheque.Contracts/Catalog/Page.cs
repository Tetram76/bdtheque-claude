namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// One page of a list of the consultation, paginated by offset (choix-implementation.md §
/// Organisation de l'API).
/// </summary>
/// <param name="Number">Number of the page, from 1.</param>
/// <param name="Size">Maximum number of items per page.</param>
/// <param name="TotalCount">Number of items of the whole list.</param>
public sealed record Page<T>(IReadOnlyList<T> Items, int Number, int Size, int TotalCount);
