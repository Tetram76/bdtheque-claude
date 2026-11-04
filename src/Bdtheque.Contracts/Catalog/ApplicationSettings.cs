using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// The setting of the application (modele-metier/parametrage-de-l-application.md), which every presentation follows:
/// the label of the albums (fonctionnel/presentation/libelle-d-un-album.md) and the display culture.
/// </summary>
public sealed record ApplicationSettings(AlbumLabelFormat AlbumLabelFormat, DisplayCulture DisplayCulture);
