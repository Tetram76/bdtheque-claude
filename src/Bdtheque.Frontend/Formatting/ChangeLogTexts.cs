using System.Globalization;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// The texts of the entries of the change log, built when they are presented, in the display culture in force
/// (modele-metier/entree-du-journal-des-modifications.md, visuel/journal/journal-des-modifications.md): the type of a record,
/// an operation, an attribute and its values. An entry keeps data only, so that it always reads the same in a same culture and
/// under a same setting; the texts are those of <c>ChangeLogTexts.resx</c>.
/// </summary>
public sealed class ChangeLogTexts(IStringLocalizer<ChangeLogTexts> texts, ValueLabels values, RecordLabels labels, DisplayFormatter format)
{
    public string Of(RecordType type) => texts[$"RecordType.{type}"];

    public string Of(ChangeLogOperation operation) => texts[$"Operation.{operation}"];

    public string Of(ChangeLogAttribute attribute) => texts[$"Attribute.{attribute}"];

    /// <summary>
    /// Whether an attribute is an element the record carries, added or withdrawn as a whole (a genre, a universe, a contribution,
    /// a visual): a change without value before is then an addition, without value after a withdrawal
    /// (modele-metier/entree-du-journal-des-modifications.md); of any other attribute, a value missing is a data absent.
    /// </summary>
    public static bool IsElement(ChangeLogAttribute attribute) =>
        attribute is ChangeLogAttribute.Genre or ChangeLogAttribute.Universe or ChangeLogAttribute.Contribution or ChangeLogAttribute.Visual;

    /// <summary>
    /// The type of a record of an entry, which the line under its label gives: an edition followed by its album, the label of
    /// an edition naming it alone (visuel/journal/journal-des-modifications.md, Entrées).
    /// </summary>
    public string TypeOf(RecordLabel label, RecordType type, AlbumLabelFormat format) => label is EditionLabel edition
        ? texts["EditionOf", labels.Album(edition.Album, format)]
        : Of(type);

    /// <summary>The label of the record of an entry, an edition by itself, its album being on the line of its type.</summary>
    public string LabelOf(RecordLabel label, AlbumLabelFormat format) =>
        label is EditionLabel edition ? labels.Edition(edition) : labels.Record(label, format);

    /// <summary>A value of an attribute, as the record holds it; another record by its label.</summary>
    public string Value(ChangeLogValue value, AlbumLabelFormat albumFormat) => value switch
    {
        TextValue text => text.Value,
        NumberValue number => format.Number(number.Value),
        FlagValue flag => texts[flag.Value ? "Yes" : "No"],
        DateValue date => format.Date(date.Value),
        PartialDateValue date => format.PartialDate(date.Year, date.Month),
        AmountValue amount => format.Amount(amount.Amount, amount.Currency),
        EnumValue member => values.Of(member.Type, member.Member),
        ReferenceValue reference => labels.Record(reference.Label, albumFormat),
        ContributionValue contribution => texts["Contribution", labels.Record(contribution.Author, albumFormat), values.Of(contribution.Role)],
        VisualValue visual => Visual(visual),
        SearchCriteriaValue criteria => Criteria(criteria.Criteria),
        SearchVariablesValue variables => variables.Variables.Count == 0
            ? texts["Variables.None"]
            : string.Join(", ", variables.Variables.Select(v => v.Name)),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value.GetType().Name, "Unknown kind of change log value."),
    };

    // A plate numbered by its rank among the plates, as an edition names it; another type numbered only beyond the first, its
    // rank starting at 0.
    private string Visual(VisualValue visual) =>
        visual.Type == VisualType.Plate || visual.DisplayOrder > 0
            ? texts["Visual.Numbered", values.Of(visual.Type), (visual.DisplayOrder + 1).ToString(CultureInfo.CurrentCulture)]
            : values.Of(visual.Type);

    // The criteria of a saved search, by their number: their tree reads in the screen of the advanced search, not on a line.
    private string Criteria(SearchCondition criteria)
    {
        var count = CountCriteria(criteria);
        return count == 1 ? texts["Criteria.One"] : texts["Criteria.Other", format.Number(count)];
    }

    private static int CountCriteria(SearchCondition condition) => condition switch
    {
        SearchGroup group => group.Conditions.Sum(CountCriteria),
        SearchNot not => CountCriteria(not.Condition),
        SearchLink link => link.Condition is null ? 1 : CountCriteria(link.Condition),
        _ => 1,
    };

    /// <summary>The text of a secret renewed, recorded without any value (a value of an access token).</summary>
    public string Secret => texts["Secret"];

    /// <summary>What follows a value withdrawn.</summary>
    public string Removed => texts["Removed"];
}
