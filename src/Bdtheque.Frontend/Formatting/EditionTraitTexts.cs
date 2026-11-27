using Bdtheque.Contracts.Catalog;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// The summary of an edition targeted by a purchase intent, under its label, the same wherever the intent is presented
/// (fonctionnel/regles-metier/intention-d-acquisition.md, résumé de l'édition visée).
/// </summary>
public sealed class EditionTraitTexts(IStringLocalizer<EditionTraitTexts> texts, ValueLabels values, DisplayFormatter format)
{
    /// <summary>
    /// Its category, format, binding, orientation, reading direction and pages, in that order, each when set, then « Noir et
    /// blanc » for an edition not in colour: in colour, the usual case, it would be read under almost every intent and teach
    /// nothing. <c>null</c> when nothing is to be said.
    /// </summary>
    public string? Summary(EditionTraits traits)
    {
        string?[] parts =
        [
            traits.Category is { } category ? values.Of(category) : null,
            traits.Format is { } editionFormat ? values.Of(editionFormat) : null,
            traits.Binding is { } binding ? values.Of(binding) : null,
            traits.Orientation is { } orientation ? values.Of(orientation) : null,
            traits.ReadingDirection is { } direction ? values.Of(direction) : null,
            traits.PageCount is { } pages ? texts["Pages", format.Number(pages)].Value : null,
            traits.IsColor ? null : texts["BlackAndWhite"].Value,
        ];
        var known = parts.OfType<string>().ToList();
        return known.Count == 0 ? null : string.Join(" · ", known);
    }
}
