namespace Bdtheque.Domain.Common;

/// <summary>
/// Guards an enum-typed domain property against undefined values. Without this guard, a
/// numeric value with no matching named member (e.g. from a future API layer binding an
/// out-of-range integer) would bypass the project's enum-as-string persistence convention:
/// <c>Enum.ToString()</c> falls back to the raw number when no name matches, so EF's string
/// converter would persist it as e.g. "42" instead of a stable member name (see
/// choix-implementation.md § Conventions de persistance).
/// </summary>
public static class EnumGuard
{
    public static void EnsureDefined<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, value, $"{typeof(TEnum).Name} does not define the value {value}.");
    }
}
