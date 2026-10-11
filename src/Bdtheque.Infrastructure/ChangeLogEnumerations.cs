using Bdtheque.Domain.Enums;

namespace Bdtheque.Infrastructure;

/// <summary>
/// The enumeration of each attribute of the change log whose values are members of one (e.g.
/// <see cref="BindingType"/> for <see cref="ChangeLogAttribute.Binding"/>), for a reader to name the
/// members the change log records by their value.
/// </summary>
public static class ChangeLogEnumerations
{
    private static readonly IReadOnlyDictionary<ChangeLogAttribute, Type> Types = ChangeLogMappings.Records.Values
        .SelectMany(r => r.Attributes)
        .Where(a => a.EnumType is not null)
        .GroupBy(a => a.Attribute)
        .ToDictionary(g => g.Key, g => g.Select(a => a.EnumType!).Distinct().Single());

    /// <summary>The domain enumeration of <paramref name="attribute"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The values of the attribute are not members of an enumeration.</exception>
    public static Type Of(ChangeLogAttribute attribute) =>
        Types.TryGetValue(attribute, out var type)
            ? type
            : throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "The values of this attribute are not members of an enumeration.");
}
