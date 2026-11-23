using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>
/// The setting of the application as its administration form edits it, with the dates of the record, which the form presents
/// (visuel/saisie-album/contenu.md, model of the entry forms).
/// </summary>
/// <param name="Version">Version of the setting, sent back with any modification.</param>
public sealed record ApplicationSettingsForm(
    AlbumLabelFormat AlbumLabelFormat,
    DisplayCulture DisplayCulture,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    uint Version);

/// <param name="Version">Version of the setting the form was read at.</param>
public sealed record UpdateApplicationSettingsRequest(AlbumLabelFormat AlbumLabelFormat, DisplayCulture DisplayCulture, uint Version);
