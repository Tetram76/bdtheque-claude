using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// Monthly audience counter (Compteur d'audience mensuel, modele-metier.md): the indicators of a function for
/// the visits begun in a month, in one compartment (administrator or visitor, device type, rendering engine and
/// its version), without any visit or path. Established once, from the visits of its month, then never
/// modified, and kept without limit.
/// </summary>
public sealed class AudienceCounter : EntityBase
{
    private readonly List<AudienceCounterObject> _objects = [];

    public string Function { get; private set; } = null!;

    /// <summary>First day of the month the counted visits began in.</summary>
    public DateOnly Month { get; private set; }

    public bool IsAdministrator { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public RenderingEngine RenderingEngine { get; private set; }
    public string? EngineVersion { get; private set; }

    public int UsageCount { get; private set; }
    public int VisitCount { get; private set; }
    public int RepeatedUsageCount { get; private set; }
    public DateTimeOffset LastUsage { get; private set; }

    /// <summary>
    /// For a function presenting a record or bearing parameters, each one used, with its number of usages: the
    /// number of different ones is not additive across counters, their union is.
    /// </summary>
    public IReadOnlyCollection<AudienceCounterObject> Objects => _objects.AsReadOnly();

    // EF Core parameterless constructor
    private AudienceCounter() { }

    public AudienceCounter(
        string function, DateOnly month, bool isAdministrator, DeviceType deviceType, RenderingEngine renderingEngine,
        string? engineVersion, int usageCount, int visitCount, int repeatedUsageCount, DateTimeOffset lastUsage,
        IEnumerable<AudienceCounterObject> objects)
    {
        if (month.Day != 1)
            throw new ArgumentException("A month is stored as its first day.", nameof(month));
        EnumGuard.EnsureDefined(deviceType, nameof(deviceType));
        EnumGuard.EnsureDefined(renderingEngine, nameof(renderingEngine));
        // A counter only exists for a function used during the month, each usage within a visit; the first usage
        // of a visit repeats nothing.
        if (usageCount < 1 || visitCount < 1 || visitCount > usageCount)
            throw new ArgumentException("A counter counts at least one usage, within at most as many visits.", nameof(visitCount));
        if (repeatedUsageCount < 0 || repeatedUsageCount > usageCount - visitCount)
            throw new ArgumentException("The first usage of a visit repeats nothing.", nameof(repeatedUsageCount));

        Function = DomainText.NullIfBlank(function) ?? throw new ArgumentException("A counter is of a function.", nameof(function));
        Month = month;
        IsAdministrator = isAdministrator;
        DeviceType = deviceType;
        RenderingEngine = renderingEngine;
        EngineVersion = DomainText.NullIfBlank(engineVersion);
        UsageCount = usageCount;
        VisitCount = visitCount;
        RepeatedUsageCount = repeatedUsageCount;
        LastUsage = lastUsage;
        _objects.AddRange(objects);
    }
}

/// <summary>A record presented or a set of parameters used by the usages a counter counts, with their number.</summary>
public sealed class AudienceCounterObject
{
    // Assigned here like any entity's (choix-implementation/persistance.md § Identifiants), never by the database.
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public RecordType? RecordType { get; private set; }
    public Guid? RecordId { get; private set; }
    public string? Parameters { get; private set; }
    public int UsageCount { get; private set; }

    public PresentedRecord? Record => RecordType is { } type ? new PresentedRecord(type, RecordId!.Value) : null;

    // EF Core parameterless constructor
    private AudienceCounterObject() { }

    public AudienceCounterObject(PresentedRecord? record, string? parameters, int usageCount)
    {
        if ((record is null) == (parameters is null))
            throw new ArgumentException("An object is a record presented or the parameters of an action.", nameof(parameters));
        ArgumentOutOfRangeException.ThrowIfLessThan(usageCount, 1);

        RecordType = record?.Type;
        RecordId = record?.Id;
        Parameters = parameters;
        UsageCount = usageCount;
    }
}
