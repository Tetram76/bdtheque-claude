using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>
/// Labels of the albums and editions (fonctionnel/presentation/libelle-d-un-album.md,
/// fonctionnel/presentation/libelle-d-une-edition.md), with the series of the album, as a context of several series shows them.
/// </summary>
public sealed class RecordLabelsTests
{
    private readonly RecordLabels _labels =
        new ServiceCollection().AddLogging().AddLocalization().AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>()
            .BuildServiceProvider().GetRequiredService<RecordLabels>();

    [Theory]
    [InlineData("Le Lotus bleu", "Tintin", 5, "Le Lotus bleu (Tintin - T. 5)")]
    [InlineData(null, "Tintin", 5, "Tintin - T. 5")]
    [InlineData("Le Lotus bleu", null, 5, "Le Lotus bleu (T. 5)")]
    [InlineData("Le Lotus bleu", "Tintin", null, "Le Lotus bleu (Tintin)")]
    [InlineData(null, "Tintin", null, "Tintin")]
    [InlineData("Le Lotus bleu", null, null, "Le Lotus bleu")]
    [InlineData(null, null, null, "<Sans titre>")]
    public void Album_InTheDefaultFormat_DropsWhatIsMissing(string? title, string? series, int? volume, string expected) =>
        Assert.Equal(expected, _labels.Album(Regular(title, series, volume), AlbumLabelFormat.TitleSeriesVolume));

    [Theory]
    [InlineData("Le Lotus bleu", "Tintin", 5, "T. 5 - Le Lotus bleu (Tintin)")]
    [InlineData(null, "Tintin", 5, "T. 5 - Tintin")]
    [InlineData("Le Lotus bleu", null, 5, "T. 5 - Le Lotus bleu")]
    [InlineData("Le Lotus bleu", "Tintin", null, "Le Lotus bleu (Tintin)")]
    [InlineData(null, "Tintin", null, "Tintin")]
    [InlineData("Le Lotus bleu", null, null, "Le Lotus bleu")]
    public void Album_VolumeFirst_DropsWhatIsMissing(string? title, string? series, int? volume, string expected) =>
        Assert.Equal(expected, _labels.Album(Regular(title, series, volume), AlbumLabelFormat.VolumeTitleSeries));

    [Theory]
    [InlineData(AlbumType.Regular, true, 2, null, null, "Titre (Série - HS 2)")]
    [InlineData(AlbumType.Regular, true, null, null, null, "Titre (Série - HS)")]
    [InlineData(AlbumType.Omnibus, false, null, 1, 6, "Titre (Série - INT. [1 à 6])")]
    [InlineData(AlbumType.Omnibus, false, 2, 4, 6, "Titre (Série - INT. - 2 [4 à 6])")]
    [InlineData(AlbumType.Omnibus, false, 2, 4, null, "Titre (Série - INT. - 2)")]
    [InlineData(AlbumType.Omnibus, true, 1, null, null, "Titre (Série - INT.HS - 1)")]
    public void Album_ShowsItsVolumeAsItsTypeWants(AlbumType type, bool special, int? volume, int? start, int? end, string expected) =>
        Assert.Equal(expected, _labels.Album(
            new AlbumSummary(Guid.NewGuid(), "Titre", Guid.NewGuid(), "Série", type, special, volume, start, end, true),
            AlbumLabelFormat.TitleSeriesVolume));

    // Without the series, an album without its own title keeps only its volume, written in full.
    [Theory]
    [InlineData(AlbumType.Regular, false, "Tome 5")]
    [InlineData(AlbumType.Regular, true, "Hors-série 5")]
    [InlineData(AlbumType.Omnibus, false, "Intégrale - 5")]
    [InlineData(AlbumType.Omnibus, true, "Intégrale hors-série - 5")]
    public void Album_WithoutSeriesNorTitle_WritesItsVolumeInFull(AlbumType type, bool special, string expected) =>
        Assert.Equal(expected, _labels.Album(
            new AlbumSummary(Guid.NewGuid(), null, Guid.NewGuid(), "Série", type, special, 5, null, null, true),
            AlbumLabelFormat.TitleSeriesVolume, withSeries: false));

    [Theory]
    [InlineData(null, null, null, "Dargaud")]
    [InlineData("Lucky Luke", 1978, null, "Dargaud (Lucky Luke) [1978]")]
    [InlineData("Lucky Luke", 1978, "978-2-205-01234-5", "Dargaud (Lucky Luke) [1978] - ISBN\u00A0978-2-205-01234-5")]
    [InlineData(null, 1978, "978-2-205-01234-5", "Dargaud [1978] - ISBN\u00A0978-2-205-01234-5")]
    // The ISBN never apart from its number, which a no-break space keeps on its line.
    public void Edition_DropsWhatIsMissing(string? collection, int? year, string? isbn, string expected) =>
        Assert.Equal(expected, _labels.Edition(
            new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Dargaud", collection is null ? null : Guid.NewGuid(), collection, year, isbn)));

    [Fact]
    public void Edition_FormatsItsIsbn_ByTheRangesOfTheAgency()
    {
        var services = new ServiceCollection().AddLogging().AddLocalization().AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>()
            .BuildServiceProvider();
        services.GetRequiredService<IsbnFormatter>().Ranges = IsbnRangesTests.Ranges();

        Assert.Equal("Glénat [2004] - ISBN\u00A0978-2-7234-4567-1", services.GetRequiredService<RecordLabels>().Edition(
            new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Glénat", null, null, 2004, "9782723445671")));
    }

    // An ISBN is a single block (fonctionnel/presentation/libelle-d-une-edition.md): the label tells it apart, mention
    // included, for the presentation that keeps it on one line; the text of the label is left as it is.
    [Theory]
    [InlineData("Dargaud (Lucky Luke) [1978] - ISBN\u00A0978-2-205-01234-5", "Dargaud (Lucky Luke) [1978] - ", "ISBN\u00A0978-2-205-01234-5")]
    [InlineData("Blacksad - T. 7 · Dargaud - ISBN\u00A0978-2-205-01234-5", "Blacksad - T. 7 · Dargaud - ", "ISBN\u00A0978-2-205-01234-5")]
    [InlineData("Dargaud (Lucky Luke) [1978]", "Dargaud (Lucky Luke) [1978]", null)]
    [InlineData("ISBN Pratique (Tome 1)", "ISBN Pratique (Tome 1)", null)]
    public void SplitIsbn_SetsTheIsbnOfALabelApart(string label, string lead, string? isbn) =>
        Assert.Equal((lead, isbn), _labels.SplitIsbn(label));

    // The label of a record of the change log, from what it was built from at the time of the write
    // (modele-metier/entree-du-journal-des-modifications.md): an album and an edition as above, an author by its main
    // identifier (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes).
    public static TheoryData<RecordLabel, string> RecordsOfTheChangeLog => new()
    {
        { new AlbumLabel("Le Lotus bleu", "Tintin", AlbumType.Regular, false, 5, null, null), "Le Lotus bleu (Tintin - T. 5)" },
        { new EditionLabel(new AlbumLabel(null, "Blacksad", AlbumType.Regular, false, 7, null, null), "Dargaud", null, 2023, null),
            "Blacksad - T. 7 · Dargaud [2023]" },
        { new PurchaseIntentLabel(new AlbumLabel(null, "Les Vieux Fourneaux", AlbumType.Regular, false, 8, null, null), null),
            "Les Vieux Fourneaux - T. 8" },
        { new PurchaseIntentLabel(null, new EditionLabel(new AlbumLabel("Undertaker", null, AlbumType.Regular, false, null, null, null),
            "Dargaud", null, null, null)), "Undertaker · Dargaud" },
        { new AuthorLabel("Van Hamme", "Jean", null), "Jean Van Hamme" },
        { new AuthorLabel("Remi", "Georges", "Hergé"), "Hergé" },
        { new AuthorLabel("Franquin", null, null), "Franquin" },
        { new PublisherCollectionLabel("Repérages", "Dupuis"), "Repérages (Dupuis)" },
        { new NameLabel("Western"), "Western" },
        { new ApplicationSettingsLabel(), "Paramétrage de l'application" },
    };

    [Theory]
    [MemberData(nameof(RecordsOfTheChangeLog))]
    public void Record_OfTheChangeLog_IsLabelledAsItsTypeWants(RecordLabel label, string expected) =>
        Assert.Equal(expected, _labels.Record(label, AlbumLabelFormat.TitleSeriesVolume));

    [Theory]
    [InlineData(AlbumType.Regular, false, 5, null, null, "T. 5")]
    [InlineData(AlbumType.Regular, true, 2, null, null, "HS 2")]
    [InlineData(AlbumType.Omnibus, false, null, 1, 6, "INT. [1 à 6]")]
    public void Volume_Alone_IsTheOneNextToATitle(AlbumType type, bool special, int? volume, int? start, int? end, string expected) =>
        Assert.Equal(expected, _labels.Volume(
            new AlbumSummary(Guid.NewGuid(), "Titre", Guid.NewGuid(), "Série", type, special, volume, start, end, true)));

    [Fact]
    public void Volume_OfARegularAlbumWithoutNumber_IsNone() =>
        Assert.Null(_labels.Volume(Regular("Titre", "Série", null)));

    [Theory]
    [InlineData("Gotlieb", "Marcel", "Gotlib", "Gotlib")]
    [InlineData("Franquin", "André", null, "André Franquin")]
    [InlineData("Franquin", null, null, "Franquin")]
    public void Author_IsNamedByItsMainIdentifier(string? lastName, string? firstName, string? pseudonym, string expected) =>
        Assert.Equal(expected, _labels.Author(new AuthorListItem(Guid.NewGuid(), lastName, firstName, pseudonym)));

    private static AlbumSummary Regular(string? title, string? series, int? volume) =>
        new(Guid.NewGuid(), title, series is null ? null : Guid.NewGuid(), series, AlbumType.Regular, false, volume, null, null, true);
}
