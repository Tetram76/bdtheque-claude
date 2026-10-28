using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Catalog;

/// <summary>
/// The setting of the application (modele-metier.md § Paramétrage de l'application), which every presentation follows:
/// the label of the albums (fonctionnel/presentation.md § Libellé d'un album) and the display culture.
/// </summary>
public sealed record ApplicationSettings(AlbumLabelFormat AlbumLabelFormat, DisplayCulture DisplayCulture);
