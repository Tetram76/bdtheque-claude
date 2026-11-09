using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Formatting;

namespace Bdtheque.Frontend.Components.Controls;

/// <summary>
/// The suggestions of a <see cref="RecordPicker"/>: the first records of the list of the consultation of their kind that
/// contain the text typed, as the search of the lists finds them (choix-implementation/api/recherche.md), in their order.
/// </summary>
public sealed class RecordSearch(ApiClient api, RecordLabels labels)
{
    /// <summary>The number of suggestions: enough to choose, few enough to read at a glance.</summary>
    public const int SuggestionCount = 10;

    /// <param name="within">
    /// The record the records searched belong to, when two filters are linked (<see cref="Lists.LinkedFilters"/>): the
    /// publisher of the collections, the series of the albums; ignored for the other kinds.
    /// </param>
    public async Task<IReadOnlyList<RecordOption>> FindAsync(RecordKind kind, string text, CancellationToken cancellationToken, Guid? within = null)
    {
        var query = $"q={Uri.EscapeDataString(text.Trim())}&pageSize={SuggestionCount}";
        string Within(string name) => within is { } id ? $"{query}&{name}={id}" : query;
        return kind switch
        {
            RecordKind.Series => await FindAsync<SeriesListItem>("series", query, s => new(s.Id, s.Title, null), cancellationToken),
            RecordKind.Author => await FindAsync<AuthorListItem>(
                "authors", query, a => new(a.Id, AuthorName(a.LastName, a.FirstName, a.Pseudonym), null), cancellationToken),
            RecordKind.Publisher => await FindAsync<PublisherListItem>("publishers", query, p => new(p.Id, p.Name, null), cancellationToken),
            RecordKind.PublisherCollection => await FindAsync<PublisherCollectionListItem>(
                "publisher-collections", Within("publisherId"),
                CollectionOption, cancellationToken),
            RecordKind.Genre => await FindAsync<GenreListItem>("genres", query, g => new(g.Id, g.Label, null), cancellationToken),
            RecordKind.Universe => await FindAsync<UniverseListItem>("universes", query, u => new(u.Id, u.Name, u.ParentName), cancellationToken),
            RecordKind.Album => await FindAlbumsAsync(Within("seriesId"), cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    /// <summary>A collection as a record option: told apart by its publisher, the record it belongs to.</summary>
    public static RecordOption CollectionOption(PublisherCollectionListItem collection) =>
        new(collection.Id, collection.Name, collection.PublisherName, new RecordOption(collection.PublisherId, collection.PublisherName, null));

    /// <summary>
    /// An album as a record option: its label in the format of the settings, its series beside it, as the lists name it
    /// (fonctionnel/presentation/libelle-d-un-album.md), and its series as the record it belongs to, none for an album
    /// without series.
    /// </summary>
    public RecordOption AlbumOption(AlbumSummary album, AlbumLabelFormat format) =>
        new(album.Id, labels.Album(album, format), null,
            album is { SeriesId: { } seriesId, SeriesTitle: { } seriesTitle } ? new RecordOption(seriesId, seriesTitle, null) : null);

    // The settings are read with each search, which a pause of the typing already spaces out, so that a format changed
    // meanwhile applies.
    private async Task<IReadOnlyList<RecordOption>> FindAlbumsAsync(string query, CancellationToken cancellationToken)
    {
        var format = (await api.GetAsync<ApplicationSettings>("/catalog/settings", cancellationToken)).AlbumLabelFormat;
        return await FindAsync<AlbumSummary>("albums", query, a => AlbumOption(a, format), cancellationToken);
    }

    /// <summary>
    /// The main identifier of an author: the pseudonym if any, otherwise « Prénom Nom », or the last name alone
    /// (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).
    /// </summary>
    public static string AuthorName(string? lastName, string? firstName, string? pseudonym) =>
        !string.IsNullOrWhiteSpace(pseudonym)
            ? pseudonym
            : string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private async Task<IReadOnlyList<RecordOption>> FindAsync<T>(
        string list, string query, Func<T, RecordOption> option, CancellationToken cancellationToken) =>
        [.. (await api.GetAsync<Page<T>>($"/catalog/{list}?{query}", cancellationToken)).Items.Select(option)];
}
