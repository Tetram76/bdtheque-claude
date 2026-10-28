namespace Bdtheque.Domain.Enums;

/// <summary>
/// Format of the label of an album, a setting of the application (fonctionnel/presentation.md § Libellé d'un album).
/// </summary>
public enum AlbumLabelFormat
{
    /// <summary><c>Album (Série - Tome)</c>, the default.</summary>
    TitleSeriesVolume = 1,

    /// <summary><c>Tome - Album (Série)</c>.</summary>
    VolumeTitleSeries = 2,
}
