using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A value of an attribute changed by a modification, as recorded in the record (modele-metier.md §
/// Entrée du journal des modifications): never a text formatted for display, so that it is presented
/// in the display culture in force when the change log is read (fonctionnel/presentation.md § Langue et culture
/// d'affichage).
/// </summary>
public abstract record ChangeLogValue;

public sealed record TextValue(string Value) : ChangeLogValue;

public sealed record NumberValue(int Value) : ChangeLogValue;

public sealed record FlagValue(bool Value) : ChangeLogValue;

public sealed record DateValue(DateOnly Value) : ChangeLogValue;

/// <summary>A date known at year, or year and month, granularity.</summary>
public sealed record PartialDateValue(int Year, int? Month) : ChangeLogValue;

/// <summary>An amount in its own currency (ISO 4217 code, or the old franc's reserved code).</summary>
public sealed record AmountValue(decimal Amount, string Currency) : ChangeLogValue;

/// <summary>
/// A member of an enumeration, by its persisted value: the enumeration is the one of the attribute
/// changed (e.g. <see cref="BindingType"/> for <see cref="ChangeLogAttribute.Binding"/>).
/// </summary>
public sealed record EnumValue(int Value) : ChangeLogValue;

/// <summary>Another record (e.g. the series of an album), with what its label was built from.</summary>
public sealed record ReferenceValue(Guid Id, RecordLabel Label) : ChangeLogValue;

/// <summary>A role credited to an author on an album or a series.</summary>
public sealed record ContributionValue(Guid AuthorId, AuthorLabel Author, ContributionRole Role) : ChangeLogValue;

/// <summary>
/// A visual of an edition, with its rank among the visuals of its type; without the reference of its
/// media, a technical datum the change log does not record (modele-metier.md § Visuel d'édition).
/// </summary>
public sealed record VisualValue(Guid Id, VisualType Type, int DisplayOrder) : ChangeLogValue;

/// <summary>The criteria of a saved search, in the JSON form the advanced search takes.</summary>
public sealed record SearchCriteriaValue(string Criteria) : ChangeLogValue;

/// <summary>The variables of a saved search, in their order, compared by value.</summary>
public sealed record SearchVariablesValue(IReadOnlyList<SearchVariableValue> Variables) : ChangeLogValue
{
    public bool Equals(SearchVariablesValue? other) => other is not null && Variables.SequenceEqual(other.Variables);

    public override int GetHashCode() => Variables.Aggregate(0, (hash, variable) => HashCode.Combine(hash, variable));
}

/// <summary>A variable of a saved search, with its default value: the constant of the criteria it replaces.</summary>
public sealed record SearchVariableValue(string Name, string Location, string DefaultValue);

/// <summary>
/// One attribute changed by a modification, with its value before and after it: an element added
/// (genre, contribution, visual…) has no value before, an element removed none after.
/// </summary>
public sealed record ChangeLogChange
{
    public ChangeLogChange(ChangeLogAttribute attribute, ChangeLogValue? before, ChangeLogValue? after)
    {
        if (before is null && after is null)
            throw new ArgumentException("A change has a value before it, after it, or both.", nameof(after));

        Attribute = attribute;
        Before = before;
        After = after;
    }

    public ChangeLogAttribute Attribute { get; }
    public ChangeLogValue? Before { get; }
    public ChangeLogValue? After { get; }
}
