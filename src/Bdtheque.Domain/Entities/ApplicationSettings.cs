using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// The setting of the application (modele-metier.md § Paramétrage de l'application): a single record, always present,
/// which is modified but neither created nor deleted.
/// </summary>
/// <remarks>
/// The application never creates it: the database is provisioned with it, under <see cref="SingletonId"/> and with
/// the defaults below, when <c>api</c> starts (choix-implementation/domaine.md § Paramétrage de l'application : mise
/// en œuvre).
/// </remarks>
public sealed class ApplicationSettings : EntityBase, IAggregateRoot
{
    /// <summary>Identifier of the single setting, the only one the database accepts.</summary>
    public static readonly Guid SingletonId = new("0199c0de-5e77-7000-8000-000000000001");

    public const AlbumLabelFormat DefaultAlbumLabelFormat = AlbumLabelFormat.TitleSeriesVolume;

    public const DisplayCulture DefaultDisplayCulture = DisplayCulture.FrenchFrance;

    public AlbumLabelFormat AlbumLabelFormat { get; private set; } = DefaultAlbumLabelFormat;

    public DisplayCulture DisplayCulture { get; private set; } = DefaultDisplayCulture;

    // EF Core materializes it; the domain tests build one.
    internal ApplicationSettings() : base(SingletonId) { }

    public void Set(AlbumLabelFormat albumLabelFormat, DisplayCulture displayCulture)
    {
        EnumGuard.EnsureDefined(albumLabelFormat, nameof(albumLabelFormat));
        EnumGuard.EnsureDefined(displayCulture, nameof(displayCulture));
        AlbumLabelFormat = albumLabelFormat;
        DisplayCulture = displayCulture;
    }
}
