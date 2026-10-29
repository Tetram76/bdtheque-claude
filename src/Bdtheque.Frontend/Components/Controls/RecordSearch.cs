using Bdtheque.Contracts.Catalog;
using Bdtheque.Frontend.Api;

namespace Bdtheque.Frontend.Components.Controls;

/// <summary>
/// The suggestions of a <see cref="RecordPicker"/>: the first records of the list of the consultation of their kind that
/// contain the text typed, as the search of the lists finds them (choix-implementation/api.md § Recherche), in their order.
/// </summary>
public sealed class RecordSearch(ApiClient api)
{
    /// <summary>The number of suggestions: enough to choose, few enough to read at a glance.</summary>
    public const int SuggestionCount = 10;

    public async Task<IReadOnlyList<RecordOption>> FindAsync(RecordKind kind, string text, CancellationToken cancellationToken)
    {
        var query = $"q={Uri.EscapeDataString(text.Trim())}&pageSize={SuggestionCount}";
        return kind switch
        {
            RecordKind.Series => await FindAsync<SeriesListItem>("series", query, s => new(s.Id, s.Title, null), cancellationToken),
            RecordKind.Author => await FindAsync<AuthorListItem>(
                "authors", query, a => new(a.Id, AuthorName(a.LastName, a.FirstName, a.Pseudonym), null), cancellationToken),
            RecordKind.Publisher => await FindAsync<PublisherListItem>("publishers", query, p => new(p.Id, p.Name, null), cancellationToken),
            RecordKind.PublisherCollection => await FindAsync<PublisherCollectionListItem>(
                "publisher-collections", query, c => new(c.Id, c.Name, c.PublisherName), cancellationToken),
            RecordKind.Genre => await FindAsync<GenreListItem>("genres", query, g => new(g.Id, g.Label, null), cancellationToken),
            RecordKind.Universe => await FindAsync<UniverseListItem>("universes", query, u => new(u.Id, u.Name, u.ParentName), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    /// <summary>
    /// The main identifier of an author: the pseudonym if any, otherwise « Prénom Nom », or the last name alone
    /// (fonctionnel/presentation.md § Artistes).
    /// </summary>
    public static string AuthorName(string? lastName, string? firstName, string? pseudonym) =>
        !string.IsNullOrWhiteSpace(pseudonym)
            ? pseudonym
            : string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private async Task<IReadOnlyList<RecordOption>> FindAsync<T>(
        string list, string query, Func<T, RecordOption> option, CancellationToken cancellationToken) =>
        [.. (await api.GetAsync<Page<T>>($"/catalog/{list}?{query}", cancellationToken)).Items.Select(option)];
}
