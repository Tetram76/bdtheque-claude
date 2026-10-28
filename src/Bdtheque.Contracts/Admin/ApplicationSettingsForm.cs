using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Admin;

/// <summary>The setting of the application as its administration form edits it.</summary>
/// <param name="Version">Version of the setting, sent back with any modification.</param>
public sealed record ApplicationSettingsForm(AlbumLabelFormat AlbumLabelFormat, DisplayCulture DisplayCulture, uint Version);

/// <param name="Version">Version of the setting the form was read at.</param>
public sealed record UpdateApplicationSettingsRequest(AlbumLabelFormat AlbumLabelFormat, DisplayCulture DisplayCulture, uint Version);
