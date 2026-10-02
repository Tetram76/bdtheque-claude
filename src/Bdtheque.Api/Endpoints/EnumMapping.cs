namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Converts an enumeration between its contract and its domain declaration. The two are separate
/// types with the same names and values, a parity pinned by <c>ContractEnumTests</c>: the conversion
/// goes through the value, and an out-of-range value is left for the domain setters to refuse.
/// </summary>
internal static class EnumMapping
{
    /// <summary>Converts a (possibly absent) enumeration value to its counterpart <typeparamref name="TTo"/>.</summary>
    public static TTo? Map<TTo>(object? value) where TTo : struct, Enum =>
        value is null ? null : (TTo)Enum.ToObject(typeof(TTo), value);
}
