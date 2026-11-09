using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// The values of the enumerations of the contracts as the model names them (modele-metier/), in the display culture
/// (<c>ValueLabels.resx</c>, under <c>{enumeration}.{value}</c>): one place for every page that presents or enters them.
/// </summary>
public sealed class ValueLabels(IStringLocalizer<ValueLabels> texts)
{
    public string Of<TEnum>(TEnum value) where TEnum : struct, Enum => texts[Key(value)];

    /// <summary>Whether a value has no label: what the tests check of every value of the enumerations presented.</summary>
    public bool ResourceNotFound(Enum value) => texts[Key(value)].ResourceNotFound;

    private static string Key(Enum value) => $"{value.GetType().Name}.{value}";
}
