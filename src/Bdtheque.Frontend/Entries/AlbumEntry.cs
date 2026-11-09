using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>An author credited with a role on an album being entered.</summary>
public sealed record Credit(ContributionRole Role, RecordOption Author);

/// <summary>
/// An album as its entry form holds it while it is entered (visuel/saisie-album/contenu.md): the fields of its content, the
/// records it references named for the form to present them, and what it displays through its series. Persisted from the
/// prerendering into the interactive circuit, which does not read it again. It applies no rule of the album: the domain is
/// their only source (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), and reports what to
/// correct when the album is saved.
/// </summary>
public sealed class AlbumEntry
{
    /// <summary>The album; <c>null</c> for a new one, not saved yet.</summary>
    public Guid? Id { get; set; }

    /// <summary>The version of the album the form was read at, sent back with its modification.</summary>
    public uint Version { get; set; }

    /// <summary>The sort key the album was read with, shown in the automatic mode until its title changes.</summary>
    public string? StoredSortKey { get; set; }

    /// <summary>Where the album stands with regard to the collection; <c>null</c> for a new album, not saved yet.</summary>
    public AlbumCollectionState? CollectionState { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? Title { get; set; }

    /// <summary>The sort key entered by hand; <c>null</c> for the automatic mode.</summary>
    public string? ManualSortKey { get; set; }

    public RecordOption? Series { get; set; }

    public bool IsOmnibus { get; set; }

    public bool IsSpecialIssue { get; set; }

    public int? VolumeNumber { get; set; }

    public int? StartVolumeNumber { get; set; }

    public int? EndVolumeNumber { get; set; }

    public int? FirstPublicationYear { get; set; }

    public int? FirstPublicationMonth { get; set; }

    public string? Summary { get; set; }

    public string? PersonalNotes { get; set; }

    public AlbumRating? Rating { get; set; }

    public List<RecordOption> Genres { get; set; } = [];

    /// <summary>Each named with its ancestors (« Univers Hergé › Moulinsart »).</summary>
    public List<RecordOption> Universes { get; set; } = [];

    public List<Credit> Credits { get; set; } = [];

    /// <summary>The genres the album displays through its series, which are modified on the form of the series.</summary>
    public List<string> SeriesGenres { get; set; } = [];

    /// <summary>The universes the album displays through its series, each named with its ancestors.</summary>
    public List<string> SeriesUniverses { get; set; } = [];

    /// <summary>A new album, empty.</summary>
    public static AlbumEntry New() => new();

    /// <summary>The album as the API gives it, with what its series gives it to display, if any.</summary>
    public static AlbumEntry Of(AlbumForm form, SeriesDetail? series)
    {
        var content = form.Content;
        var references = form.References;
        var authors = references.Authors.ToDictionary(a => a.Id);
        var entry = new AlbumEntry
        {
            Id = form.Id,
            Version = form.Version,
            StoredSortKey = form.SortKey,
            CollectionState = form.CollectionState,
            CreatedAt = form.CreatedAt,
            ModifiedAt = form.ModifiedAt,
            Title = content.Title,
            ManualSortKey = content.ManualSortKey,
            Series = references.Series is { } named ? new RecordOption(named.Id, named.Title, null) : null,
            IsOmnibus = content.Type == AlbumType.Omnibus,
            IsSpecialIssue = content.IsSpecialIssue,
            VolumeNumber = content.VolumeNumber,
            StartVolumeNumber = content.StartVolumeNumber,
            EndVolumeNumber = content.EndVolumeNumber,
            FirstPublicationYear = content.FirstPublicationYear,
            FirstPublicationMonth = content.FirstPublicationMonth,
            Summary = content.Summary,
            PersonalNotes = content.PersonalNotes,
            Rating = content.Rating,
            Genres = [.. references.Genres.Select(g => new RecordOption(g.Id, g.Label, null))],
            Universes = [.. references.Universes.Select(Universe)],
            Credits = [.. content.Contributions.Select(c => new Credit(c.Role, Author(authors[c.AuthorId])))],
        };
        entry.ShowSeries(series);
        return entry;
    }

    /// <summary>What the series gives the album to display; nothing without a series.</summary>
    public void ShowSeries(SeriesDetail? series)
    {
        SeriesGenres = series is null ? [] : [.. series.Genres.Select(g => g.Label)];
        SeriesUniverses = series is null ? [] : [.. series.Universes.Select(u => Universe(u).Label)];
    }

    /// <summary>The content to save: the whole form, the range of volumes only for an omnibus, whose fields alone show it.</summary>
    public AlbumContent ToContent() => new(
        Title,
        ManualSortKey,
        Series?.Id,
        IsOmnibus ? AlbumType.Omnibus : AlbumType.Regular,
        IsSpecialIssue,
        VolumeNumber,
        IsOmnibus ? StartVolumeNumber : null,
        IsOmnibus ? EndVolumeNumber : null,
        FirstPublicationYear,
        FirstPublicationMonth,
        Summary,
        PersonalNotes,
        Rating,
        [.. Genres.Select(g => g.Id)],
        [.. Universes.Select(u => u.Id)],
        [.. Credits.Select(c => new ContributionContent(c.Author.Id, c.Role))]);

    /// <summary>
    /// What tells two states of the form apart: the content it would save, the order of its associations aside, which the
    /// album does not keep.
    /// </summary>
    public string Fingerprint()
    {
        var content = ToContent();
        return JsonSerializer.Serialize(content with
        {
            GenreIds = [.. content.GenreIds.Order()],
            UniverseIds = [.. content.UniverseIds.Order()],
            Contributions = [.. content.Contributions.OrderBy(c => c.Role).ThenBy(c => c.AuthorId)],
        });
    }

    public AlbumEntry Copy() => JsonSerializer.Deserialize<AlbumEntry>(JsonSerializer.Serialize(this))!;

    public IReadOnlyList<RecordOption> AuthorsOf(ContributionRole role) => [.. Credits.Where(c => c.Role == role).Select(c => c.Author)];

    /// <summary>Credits the author with the role, once.</summary>
    public void Credit(ContributionRole role, RecordOption author)
    {
        if (!Credits.Any(c => c.Role == role && c.Author.Id == author.Id))
            Credits.Add(new Credit(role, author));
    }

    /// <summary>Credits the album with the authors a series gives it, in place of its own.</summary>
    public void CreditOnly(IEnumerable<SeriesAlbumContribution> contributions) =>
        Credits = [.. contributions.Select(c => new Credit(c.Role, Author(c.Author)))];

    public static RecordOption Author(AuthorListItem author) =>
        new(author.Id, RecordSearch.AuthorName(author.LastName, author.FirstName, author.Pseudonym), null);

    /// <summary>A universe named with its ancestors, from the top of the hierarchy (visuel/saisie-album/contenu.md).</summary>
    public static RecordOption Universe(UniverseInHierarchy universe) =>
        new(universe.Universe.Id, string.Join(" › ", universe.Ancestors.Append(universe.Universe).Select(u => u.Name)), null);
}
