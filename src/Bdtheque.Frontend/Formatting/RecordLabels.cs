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
public sealed class RecordLabels(IStringLocalizer<RecordLabels> texts, IsbnFormatter isbns)
{
    /// <summary>
    /// The label of an album in the format of the settings, its series included, where the context does not make the series
    /// plain: the series stands in for a missing title, and the volume is then abbreviated as next to any title
    /// (fonctionnel/presentation/libelle-d-un-album.md, examples). Where the context makes it plain, see <see cref="InItsSeries"/>.
    /// </summary>
    public string Album(AlbumSummary album, AlbumLabelFormat format) => Album(LabelOf(album), format);

    /// <summary>
    /// The identification of an album where its series goes without saying: its name without the series, and the volume that
    /// goes with it, none when the name already says it (fonctionnel/presentation/libelle-d-un-album.md#série-reprise-selon-le-contexte).
    /// The one place this choice is made, so that no such context names the series, nor identifies an album by its volume twice.
    /// </summary>
    public AlbumInSeries InItsSeries(AlbumSummary album)
    {
        var label = LabelOf(album);
        return album.Title is { } title
            ? new AlbumInSeries(title, Volume(label, abbreviated: true))
            : new AlbumInSeries(Volume(label, abbreviated: false) ?? Untitled, null);
    }

    /// <summary>
    /// An album in the rest of its series, special case of <see cref="InItsSeries"/> (visuel/fiche-album/contenu.md): its name
    /// without the series, and its column of the volume, always given, abbreviated; an omnibus that is no special issue, with
    /// both its bounds, shows them there (« T. 1 à T. 4 »), and its name leaves them out (« Intégrale - 1 »). <c>Volume</c> is
    /// <c>null</c> for a regular album without number only.
    /// </summary>
    public AlbumInSeries InTheRestOfItsSeries(AlbumSummary album)
    {
        var label = LabelOf(album);
        if (album is not { Type: AlbumType.Omnibus, IsSpecialIssue: false, StartVolumeNumber: { } start, EndVolumeNumber: { } end })
            return new AlbumInSeries(album.Title ?? Volume(label, abbreviated: false) ?? Untitled, Volume(label, abbreviated: true));

        var bounds = label with { StartVolumeNumber = null, EndVolumeNumber = null };
        var range = string.Format(CultureInfo.CurrentCulture, texts["Range"], VolumeNumbered(start), VolumeNumbered(end));
        return new AlbumInSeries(album.Title ?? Volume(bounds, abbreviated: false)!, range);
    }

    /// <summary>The label of an edition: its publisher, then its collection, year and ISBN when known.</summary>
    public string Edition(EditionSummary edition) =>
        Edition(edition.PublisherName, edition.PublisherCollectionName, edition.PublicationYear, edition.Isbn);

    /// <summary>
    /// A label split before its ISBN, mention included, which always ends it: an ISBN is a single block, which the
    /// presentation keeps on one line (fonctionnel/presentation/libelle-d-une-edition.md). <c>Isbn</c> is <c>null</c> for a
    /// label without ISBN, whole in <c>Lead</c>.
    /// </summary>
    public (string Lead, string? Isbn) SplitIsbn(string label)
    {
        var start = label.LastIndexOf($" - {texts["Isbn"]}\u00A0", StringComparison.Ordinal);
        return start < 0 ? (label, null) : (label[..(start + 3)], label[(start + 3)..]);
    }

    /// <summary>
    /// The label of a purchase intent: the album it targets, followed by the edition when it targets one, as the change log
    /// names it (<see cref="Record"/>).
    /// </summary>
    public string PurchaseIntent(AlbumSummary album, EditionSummary? edition, AlbumLabelFormat format) =>
        edition is null ? Album(album, format) : WithItsAlbum(Album(album, format), Edition(edition));

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
        _ => Untitled,
    };

    // The fallback of any label whose components are all empty (fonctionnel/presentation/libelle-d-un-album.md#fallback).
    private string Untitled => texts["Untitled"];

    private static AlbumLabel LabelOf(AlbumSummary album) => new(album.Title, album.SeriesTitle, album.Type, album.IsSpecialIssue,
        album.VolumeNumber, album.StartVolumeNumber, album.EndVolumeNumber);

    private string Album(AlbumLabel album, AlbumLabelFormat format)
    {
        var series = album.SeriesTitle;
        var title = album.Title;
        var volume = Volume(album, abbreviated: title is not null || series is not null);

        if (title is null && series is null)
            return volume ?? Untitled;

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
            label += $" - {texts["Isbn"]}\u00A0{isbns.Format(isbn)}";
        return label;
    }

    private string EditionWithItsAlbum(EditionLabel edition, AlbumLabelFormat format) => WithItsAlbum(
        Album(edition.Album, format), Edition(edition.PublisherName, edition.PublisherCollectionName, edition.PublicationYear, edition.Isbn));

    private static string WithItsAlbum(string album, string edition) => $"{album} · {edition}";

    // Its main identifier: the pseudonym, else « First name Last name », or the last name alone
    // (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).
    private string Author(AuthorLabel author)
    {
        if (author.Pseudonym is { } pseudonym)
            return pseudonym;
        var name = string.Join(' ', new[] { author.FirstName, author.LastName }.OfType<string>());
        return name.Length > 0 ? name : Untitled;
    }

    // Never both null: the caller has already stood the volume in for a label without either.
    private static string TitleWithSeries(string? title, string? series) => (title, series) switch
    {
        (null, _) => series!,
        (_, null) => title,
        _ => $"{title} ({series})",
    };

    // A volume of a range, as next to a title: « T. 4 ».
    private string VolumeNumbered(int number) => $"{texts["Volume.Short"]} {number.ToString(CultureInfo.CurrentCulture)}";

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
