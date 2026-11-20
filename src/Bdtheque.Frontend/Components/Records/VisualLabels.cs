using System.Globalization;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Components.Records;

/// <summary>
/// The names of a visual of an edition, written once for every page that names one: its type, a plate alone being numbered, by
/// its rank among the plates of the edition, in full (the viewer and the thumbnails of the entry form) or short (the thumbnails of
/// the album record, too small for the full name; visuel/fiche-album/contenu.md, visuel/saisie-edition/contenu.md, #469). The
/// texts are those of the component that asks (keys <c>Plate</c> and <c>Short.{type}</c>).
/// </summary>
internal static class VisualLabels
{
    /// <summary>The type of the visual at <paramref name="index"/> of the edition in full (« Planche 2 », « 4e de couverture »).</summary>
    public static string TypeOf(EditionDetail edition, int index, ValueLabels values, IStringLocalizer texts) =>
        TypeOf(TypesOf(edition), index, values, texts);

    /// <summary>The type of the visual at <paramref name="index"/> among <paramref name="types"/>, those of the visuals of an
    /// edition in their order, in full.</summary>
    public static string TypeOf(IReadOnlyList<VisualType> types, int index, ValueLabels values, IStringLocalizer texts) =>
        types[index] == VisualType.Plate ? texts["Plate", PlateNumber(types, index)] : values.Of(types[index]);

    /// <summary>The type of the visual at <paramref name="index"/> in short, under its thumbnail (« Couv. », « 2 », « 4e »).</summary>
    public static string ShortOf(EditionDetail edition, int index, IStringLocalizer texts)
    {
        var types = TypesOf(edition);
        return types[index] == VisualType.Plate
            ? PlateNumber(types, index).ToString(CultureInfo.CurrentCulture)
            : texts[$"Short.{types[index]}"];
    }

    private static VisualType[] TypesOf(EditionDetail edition) => [.. edition.Visuals.Select(v => v.Type)];

    private static int PlateNumber(IReadOnlyList<VisualType> types, int index) =>
        types.Take(index + 1).Count(t => t == VisualType.Plate);
}
