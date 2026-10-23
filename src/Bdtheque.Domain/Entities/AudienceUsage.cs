using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// Usage of a function of the web interface during a visit (Utilisation d'une fonction, modele-metier.md): a
/// screen displayed or an action triggered. Never modified once recorded; its date is its creation date.
/// </summary>
public sealed class AudienceUsage : EntityBase
{
    public Guid VisitId { get; private set; }

    /// <summary>Set on the first usage only, which saves its visit with it.</summary>
    public AudienceVisit? Visit { get; private set; }

    public string Function { get; private set; } = null!;

    // Kept after the record is deleted: no foreign key ties the usage to the record, as for the change log.
    public RecordType? PresentedRecordType { get; private set; }
    public Guid? PresentedRecordId { get; private set; }

    public string? Parameters { get; private set; }

    /// <summary>
    /// The usage immediately preceding it in its visit was of the same function, on the same record and with the
    /// same parameters (modele-metier.md § Compteur d'audience mensuel). Set when it is recorded, the preceding
    /// usage being known then, and never changed.
    /// </summary>
    public bool IsRepeat { get; private set; }

    public PresentedRecord? PresentedRecord =>
        PresentedRecordType is { } type ? new PresentedRecord(type, PresentedRecordId!.Value) : null;

    public FunctionUse Use => new(Function, PresentedRecord, Parameters);

    // EF Core parameterless constructor
    private AudienceUsage() { }

    internal AudienceUsage(AudienceVisit visit, FunctionUse use) : this(visit.Id, use, isRepeat: false) => Visit = visit;

    private AudienceUsage(Guid visitId, FunctionUse use, bool isRepeat)
    {
        ArgumentNullException.ThrowIfNull(use);
        VisitId = visitId;
        Function = use.Function;
        PresentedRecordType = use.Record?.Type;
        PresentedRecordId = use.Record?.Id;
        Parameters = use.Parameters;
        IsRepeat = isRepeat;
    }

    /// <summary>
    /// Records the next usage of the visit, this one being the latest recorded: whether it repeats this one is
    /// settled now, for good.
    /// </summary>
    public AudienceUsage Follow(FunctionUse use)
    {
        ArgumentNullException.ThrowIfNull(use);
        return new AudienceUsage(VisitId, use, isRepeat: use == Use);
    }
}

/// <summary>A record a screen presents (Fiche présentée, modele-metier.md § Utilisation d'une fonction).</summary>
public sealed record PresentedRecord
{
    public RecordType Type { get; }
    public Guid Id { get; }

    public PresentedRecord(RecordType type, Guid id)
    {
        EnumGuard.EnsureDefined(type, nameof(type));
        Type = type;
        Id = id;
    }
}

/// <summary>
/// What a usage bears: its function, and either the record its screen presents or the parameters of its action,
/// never both — a record an action writes is among its parameters. The parameters are a text the frontend
/// builds, compared as it is.
/// </summary>
/// <remarks>Built by the frontend, never entered: an inconsistent use is a technical error.</remarks>
public sealed record FunctionUse
{
    public string Function { get; }
    public PresentedRecord? Record { get; }
    public string? Parameters { get; }

    public FunctionUse(string function, PresentedRecord? record, string? parameters)
    {
        Function = DomainText.NullIfBlank(function) ?? throw new ArgumentException("A usage is of a function.", nameof(function));
        Parameters = string.IsNullOrWhiteSpace(parameters) ? null : parameters;
        if (record is not null && Parameters is not null)
            throw new ArgumentException("A usage presents a record or bears parameters, never both.", nameof(parameters));
        Record = record;
    }
}
