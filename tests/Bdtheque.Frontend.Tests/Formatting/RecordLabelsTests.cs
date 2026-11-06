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
        new ServiceCollection().AddLogging().AddLocalization().AddSingleton<RecordLabels>()
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
    [InlineData("Lucky Luke", 1978, "978-2-205-01234-5", "Dargaud (Lucky Luke) [1978] - ISBN 978-2-205-01234-5")]
    [InlineData(null, 1978, "978-2-205-01234-5", "Dargaud [1978] - ISBN 978-2-205-01234-5")]
    public void Edition_DropsWhatIsMissing(string? collection, int? year, string? isbn, string expected) =>
        Assert.Equal(expected, _labels.Edition(
            new EditionSummary(Guid.NewGuid(), Guid.NewGuid(), "Dargaud", collection is null ? null : Guid.NewGuid(), collection, year, isbn)));

    private static AlbumSummary Regular(string? title, string? series, int? volume) =>
        new(Guid.NewGuid(), title, series is null ? null : Guid.NewGuid(), series, AlbumType.Regular, false, volume, null, null, true);
}
