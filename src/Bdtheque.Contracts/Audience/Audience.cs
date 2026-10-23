using Bdtheque.Contracts.Enums;

namespace Bdtheque.Contracts.Audience;

// The audience measurement of the web interface (choix-implementation/mesure-audience.md § Mesure d'audience :
// mise en œuvre): recorded by the frontend under /audience, read under /admin/audience.

/// <summary>A record a screen presents.</summary>
public sealed record PresentedRecord(RecordType Type, Guid Id);

/// <summary>
/// A function used — screen displayed or action triggered —, with either the record its screen presents or the
/// parameters of its action, never both. The parameters are compared as they are written.
/// </summary>
public sealed record FunctionUse(string Function, PresentedRecord? Record, string? Parameters);

/// <summary>Opens a visit with its first usage.</summary>
public sealed record OpenVisitRequest(
    bool IsAdministrator, DeviceType DeviceType, RenderingEngine RenderingEngine, string? EngineVersion, FunctionUse FirstUse);

public sealed record OpenedVisit(Guid VisitId);

/// <param name="Begin">Date of its first usage.</param>
public sealed record AudienceVisitItem(
    Guid Id, DateTimeOffset Begin, bool IsAdministrator, DeviceType DeviceType, RenderingEngine RenderingEngine, string? EngineVersion,
    int UsageCount);

/// <param name="IsRepeat">It repeats the usage immediately preceding it in the visit.</param>
public sealed record AudienceUsageItem(Guid Id, DateTimeOffset Date, string Function, PresentedRecord? Record, string? Parameters, bool IsRepeat);

/// <summary>A visit and its path: its usages, in the order of their dates.</summary>
public sealed record AudienceVisitDetail(AudienceVisitItem Visit, IReadOnlyList<AudienceUsageItem> Usages);

/// <summary>The indicators of a function over the period and compartment read.</summary>
/// <param name="DistinctObjectCount">Different records presented, or different parameters used.</param>
/// <param name="Months">Its frequency: the usages and visits of each month it was used in, in their order.</param>
public sealed record FunctionIndicators(
    string Function, int UsageCount, int VisitCount, int RepeatedUsageCount, int DistinctObjectCount, DateTimeOffset LastUsage,
    IReadOnlyList<MonthlyUsage> Months);

/// <param name="Month">First day of the month the visits began in.</param>
public sealed record MonthlyUsage(DateOnly Month, int UsageCount, int VisitCount);
