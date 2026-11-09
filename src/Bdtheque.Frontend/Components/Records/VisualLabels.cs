using System.Globalization;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Components.Records;

/// <summary>
/// The names of a visual of an edition, shared by its thumbnail and the viewer (visuel/fiche-album/contenu.md): its type,
/// a plate being numbered by its rank among the plates of the edition, in full or short. The texts are those of the
/// component that asks (keys <c>Plate</c> and <c>Short.{type}</c>).
/// </summary>
internal static class VisualLabels
{
    /// <summary>The type of the visual at <paramref name="index"/> in full (« Planche 2 », « 4e de couverture »).</summary>
    public static string TypeOf(EditionDetail edition, int index, ValueLabels values, IStringLocalizer texts) =>
        edition.Visuals[index].Type == VisualType.Plate ? texts["Plate", PlateNumber(edition, index)] : values.Of(edition.Visuals[index].Type);

    /// <summary>The type of the visual at <paramref name="index"/> in short, under its thumbnail (« Couv. », « 2 », « 4e »).</summary>
    public static string ShortOf(EditionDetail edition, int index, IStringLocalizer texts) =>
        edition.Visuals[index].Type == VisualType.Plate
            ? PlateNumber(edition, index).ToString(CultureInfo.CurrentCulture)
            : texts[$"Short.{edition.Visuals[index].Type}"];

    private static int PlateNumber(EditionDetail edition, int index) =>
        edition.Visuals.Take(index + 1).Count(v => v.Type == VisualType.Plate);
}
