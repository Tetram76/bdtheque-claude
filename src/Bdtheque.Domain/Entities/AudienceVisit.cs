using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// Visit of the web interface (Visite de l'interface web, modele-metier.md): the usages of one user during one
/// browsing session, with nothing that identifies the user or the device. Never modified once recorded; its
/// beginning is its creation date, the date of its first usage, recorded with it.
/// </summary>
/// <remarks>
/// Not a record: neither in the change log nor versioned (choix-implementation/mesure-audience.md § Mesure
/// d'audience : mise en œuvre).
/// </remarks>
public sealed class AudienceVisit : EntityBase
{
    /// <summary>A visit of the administrator (authenticated session), even on the public pages.</summary>
    public bool IsAdministrator { get; private set; }

    public DeviceType DeviceType { get; private set; }
    public RenderingEngine RenderingEngine { get; private set; }
    public string? EngineVersion { get; private set; }

    // EF Core parameterless constructor
    private AudienceVisit() { }

    /// <summary>
    /// Opens a visit with its first usage: a visit always has one at least. Saving the usage saves its visit.
    /// </summary>
    public static AudienceUsage Open(
        bool isAdministrator, DeviceType deviceType, RenderingEngine renderingEngine, string? engineVersion, FunctionUse firstUse)
    {
        EnumGuard.EnsureDefined(deviceType, nameof(deviceType));
        EnumGuard.EnsureDefined(renderingEngine, nameof(renderingEngine));

        var visit = new AudienceVisit
        {
            IsAdministrator = isAdministrator,
            DeviceType = deviceType,
            RenderingEngine = renderingEngine,
            EngineVersion = DomainText.NullIfBlank(engineVersion),
        };
        return new AudienceUsage(visit, firstUse);
    }
}
