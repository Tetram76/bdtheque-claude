using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Format of the label of an album, a setting of the application (fonctionnel/presentation/libelle-d-un-album.md).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AlbumLabelFormat>))]
public enum AlbumLabelFormat
{
    /// <summary><c>Album (Série - Tome)</c>, the default.</summary>
    TitleSeriesVolume = 1,

    /// <summary><c>Tome - Album (Série)</c>.</summary>
    VolumeTitleSeries = 2,
}
