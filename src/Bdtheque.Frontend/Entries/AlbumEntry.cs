using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

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

    /// <summary>
    /// The series whose contributions the album last received, whatever series was chosen since: those of the album as read,
    /// then those of the form (fonctionnel/regles-metier/initialisation-des-contributions-depuis-la-serie.md).
    /// </summary>
    public Guid? AuthorsSeriesId { get; set; }

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

    /// <summary>Its authors, genres and universes, those added on the spot included.</summary>
    public CitedRecords Cited { get; set; } = new();

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
            AuthorsSeriesId = content.SeriesId,
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
            Cited = CitedRecords.Of(content.Contributions, references.Authors, references.Genres, references.Universes),
        };
        entry.ShowSeries(series);
        return entry;
    }

    /// <summary>What the series gives the album to display; nothing without a series.</summary>
    public void ShowSeries(SeriesDetail? series)
    {
        SeriesGenres = series is null ? [] : [.. series.Genres.Select(g => g.Label)];
        SeriesUniverses = series is null ? [] : [.. series.Universes.Select(u => CitedRecords.Universe(u).Label)];
    }

    /// <summary>
    /// The content to save: the whole form, the range of volumes only for an omnibus, whose fields alone show it, and the
    /// records added on the spot that it still cites, to create with the album.
    /// </summary>
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
        Cited.GenreIds(),
        Cited.UniverseIds(),
        Cited.Contributions(),
        Cited.Added());

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
}
