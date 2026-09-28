namespace Bdtheque.Domain.Common;

/// <summary>
/// Guards an enum-typed domain property against undefined values. Without this guard, a
/// value with no matching named member (e.g. from a future API layer binding an arbitrary
/// integer) would be persisted as-is under the project's enum-as-int convention (see
/// choix-implementation.md § Conventions de persistance), even though it corresponds to no
/// member any domain enum actually defines.
/// </summary>
public static class EnumGuard
{
    public static void EnsureDefined<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, value, $"{typeof(TEnum).Name} does not define the value {value}.");
    }
}
