using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// The setting of the application as its entry form holds it while it is entered (modele-metier/parametrage-de-l-application.md):
/// a single record, which is modified but neither created nor deleted. Persisted from the prerendering into the interactive
/// circuit, which does not read it again. It applies no rule of the setting: the domain is their only source
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
/// </summary>
public sealed class SettingsEntry
{
    /// <summary>The version of the setting the form was read at, sent back with its modification.</summary>
    public uint Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public AlbumLabelFormat AlbumLabelFormat { get; set; }

    public DisplayCulture DisplayCulture { get; set; }

    /// <summary>The setting as the API gives it.</summary>
    public static SettingsEntry Of(ApplicationSettingsForm form) => new()
    {
        Version = form.Version,
        CreatedAt = form.CreatedAt,
        ModifiedAt = form.ModifiedAt,
        AlbumLabelFormat = form.AlbumLabelFormat,
        DisplayCulture = form.DisplayCulture,
    };

    /// <summary>The modification to save: the whole form, under the version it was read at.</summary>
    public UpdateApplicationSettingsRequest ToRequest() => new(AlbumLabelFormat, DisplayCulture, Version);

    /// <summary>What tells two states of the form apart: what it would save, its version left out.</summary>
    public string Fingerprint() => JsonSerializer.Serialize(ToRequest() with { Version = 0 });

    public SettingsEntry Copy() => (SettingsEntry)MemberwiseClone();
}
