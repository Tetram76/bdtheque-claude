using System.Globalization;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// Labels of the records, built when they are presented, from their components
/// (fonctionnel/presentation/libelle-d-un-album.md, fonctionnel/presentation/libelle-d-une-edition.md), whether a list
/// gives them or the change log kept them from the time of a write (modele-metier/entree-du-journal-des-modifications.md).
/// The words of the volume and the ISBN mention are texts of the display culture (<c>RecordLabels.resx</c>).
/// </summary>
public sealed class RecordLabels(IStringLocalizer<RecordLabels> texts)
{
    /// <summary>
    /// The label of an album in the format of the settings. <paramref name="withSeries"/> leaves the series out where the
    /// context makes it plain; with it, the series stands in for a missing title, and the volume is then abbreviated as
    /// next to any title (fonctionnel/presentation/libelle-d-un-album.md, examples).
    /// </summary>
    public string Album(AlbumSummary album, AlbumLabelFormat format, bool withSeries = true) =>
        Album(new AlbumLabel(album.Title, album.SeriesTitle, album.Type, album.IsSpecialIssue, album.VolumeNumber,
            album.StartVolumeNumber, album.EndVolumeNumber), format, withSeries);

    /// <summary>The label of an edition: its publisher, then its collection, year and ISBN when known.</summary>
    public string Edition(EditionSummary edition) =>
        Edition(edition.PublisherName, edition.PublisherCollectionName, edition.PublicationYear, edition.Isbn);

    /// <summary>
    /// The volume of an album alone, as next to a title (fonctionnel/presentation/libelle-d-un-album.md); <c>null</c> for a
    /// regular album without number.
    /// </summary>
    public string? Volume(AlbumSummary album) =>
        Volume(new AlbumLabel(album.Title, album.SeriesTitle, album.Type, album.IsSpecialIssue, album.VolumeNumber,
            album.StartVolumeNumber, album.EndVolumeNumber), abbreviated: true);

    /// <summary>The name of an author: its main identifier.</summary>
    public string Author(AuthorListItem author) => Author(new AuthorLabel(author.LastName, author.FirstName, author.Pseudonym));

    /// <summary>
    /// The label of a record of the change log, with its series for an album, the log mixing every series: an edition is
    /// hardly told apart without its album, which comes first.
    /// </summary>
    public string Record(RecordLabel label, AlbumLabelFormat format) => label switch
    {
        AlbumLabel album => Album(album, format),
        EditionLabel edition => EditionWithItsAlbum(edition, format),
        PurchaseIntentLabel { Edition: { } edition } => EditionWithItsAlbum(edition, format),
        PurchaseIntentLabel { Album: { } album } => Album(album, format),
        AuthorLabel author => Author(author),
        PublisherCollectionLabel collection => $"{collection.Name} ({collection.PublisherName})",
        NameLabel name => name.Name,
        ApplicationSettingsLabel => texts["ApplicationSettings"],
        _ => texts["Untitled"],
    };

    private string Album(AlbumLabel album, AlbumLabelFormat format, bool withSeries = true)
    {
        var series = withSeries ? album.SeriesTitle : null;
        var title = album.Title;
        var volume = Volume(album, abbreviated: title is not null || series is not null);

        if (title is null && series is null)
            return volume ?? texts["Untitled"];

        return format switch
        {
            AlbumLabelFormat.VolumeTitleSeries => (title, series, volume) switch
            {
                (_, _, null) => TitleWithSeries(title, series),
                (null, _, _) => $"{volume} - {series}",
                (_, null, _) => $"{volume} - {title}",
                _ => $"{volume} - {title} ({series})",
            },
            _ => (title, series, volume) switch
            {
                (_, _, null) => TitleWithSeries(title, series),
                (null, _, _) => $"{series} - {volume}",
                (_, null, _) => $"{title} ({volume})",
                _ => $"{title} ({series} - {volume})",
            },
        };
    }

    private string Edition(string publisher, string? collectionName, int? publicationYear, string? isbn)
    {
        var label = publisher;
        if (collectionName is { } collection)
            label += $" ({collection})";
        if (publicationYear is { } year)
            label += string.Create(CultureInfo.InvariantCulture, $" [{year}]");
        if (isbn is not null)
            label += $" - {texts["Isbn"]}\u00A0{isbn}";
        return label;
    }

    private string EditionWithItsAlbum(EditionLabel edition, AlbumLabelFormat format) =>
        $"{Album(edition.Album, format)} · {Edition(edition.PublisherName, edition.PublisherCollectionName, edition.PublicationYear, edition.Isbn)}";

    // Its main identifier: the pseudonym, else « First name Last name », or the last name alone
    // (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).
    private string Author(AuthorLabel author)
    {
        if (author.Pseudonym is { } pseudonym)
            return pseudonym;
        var name = string.Join(' ', new[] { author.FirstName, author.LastName }.OfType<string>());
        return name.Length > 0 ? name : texts["Untitled"];
    }

    // Never both null: the caller has already stood the volume in for a label without either.
    private static string TitleWithSeries(string? title, string? series) => (title, series) switch
    {
        (null, _) => series!,
        (_, null) => title,
        _ => $"{title} ({series})",
    };

    // The volume of the album, as its type wants it; null for a regular album without number.
    private string? Volume(AlbumLabel album, bool abbreviated)
    {
        var form = abbreviated ? "Short" : "Full";
        if (album.Type != AlbumType.Omnibus)
        {
            var number = album.VolumeNumber is { } n ? n.ToString(CultureInfo.CurrentCulture) : null;
            if (!album.IsSpecialIssue)
                return number is null ? null : $"{texts[$"Volume.{form}"]} {number}";
            return number is null ? texts[$"SpecialIssue.{form}"] : $"{texts[$"SpecialIssue.{form}"]} {number}";
        }

        var omnibus = texts[album.IsSpecialIssue ? $"SpecialOmnibus.{form}" : $"Omnibus.{form}"].Value;
        if (album.VolumeNumber is { } volume)
            omnibus += $" - {volume.ToString(CultureInfo.CurrentCulture)}";
        if (album.StartVolumeNumber is { } start && album.EndVolumeNumber is { } end)
            omnibus += $" [{string.Format(CultureInfo.CurrentCulture, texts["Range"], start, end)}]";
        return omnibus;
    }
}
