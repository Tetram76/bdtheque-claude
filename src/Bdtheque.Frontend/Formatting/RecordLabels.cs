using System.Globalization;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// Labels of the albums and editions, built when they are presented, from their components
/// (fonctionnel/presentation/libelle-d-un-album.md, fonctionnel/presentation/libelle-d-une-edition.md). The words of the
/// volume and the ISBN mention are texts of the display culture (<c>RecordLabels.resx</c>).
/// </summary>
public sealed class RecordLabels(IStringLocalizer<RecordLabels> texts)
{
    /// <summary>
    /// The label of an album in the format of the settings. <paramref name="withSeries"/> leaves the series out where the
    /// context makes it plain; with it, the series stands in for a missing title, and the volume is then abbreviated as
    /// next to any title (fonctionnel/presentation/libelle-d-un-album.md, examples).
    /// </summary>
    public string Album(AlbumSummary album, AlbumLabelFormat format, bool withSeries = true)
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

    /// <summary>The label of an edition: its publisher, then its collection, year and ISBN when known.</summary>
    public string Edition(EditionSummary edition)
    {
        var label = edition.PublisherName;
        if (edition.PublisherCollectionName is { } collection)
            label += $" ({collection})";
        if (edition.PublicationYear is { } year)
            label += string.Create(CultureInfo.InvariantCulture, $" [{year}]");
        if (edition.Isbn is { } isbn)
            label += $" - {texts["Isbn"]} {isbn}";
        return label;
    }

    // Never both null: the caller has already stood the volume in for a label without either.
    private static string TitleWithSeries(string? title, string? series) => (title, series) switch
    {
        (null, _) => series!,
        (_, null) => title,
        _ => $"{title} ({series})",
    };

    // The volume of the album, as its type wants it; null for a regular album without number.
    private string? Volume(AlbumSummary album, bool abbreviated)
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
