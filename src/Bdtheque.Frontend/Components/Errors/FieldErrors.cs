using Bdtheque.Frontend.Api;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Components.Errors;

/// <summary>
/// The fields a refused saving designates, which an entry form borders, with the message under them, the first one getting
/// the focus (visuel/saisie-album/contenu.md): the error carries them itself, as JSON Pointers into the content the form sent
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), so that a form only says which field
/// of its content each of its controls enters, and never deduces it from the rule.
/// </summary>
/// <param name="problem">The error of the last saving, if any.</param>
/// <param name="texts">The texts under the fields, by rule code.</param>
/// <param name="rules">The texts of the rules, which the error above the form gives.</param>
/// <param name="subject">The subject of the form, which a text may name (e.g. the name of the publisher as typed).</param>
public sealed class FieldErrors(
    ApiProblem? problem, IStringLocalizer<FieldMessages> texts, IStringLocalizer<RuleMessages> rules, string subject)
{
    /// <summary>
    /// What to correct in the control that enters <paramref name="fields"/> of the content (e.g. <c>/title</c>, or a pair of
    /// fields entered together, an amount and its currency); none if the error designates none of them.
    /// </summary>
    /// <remarks>
    /// A field whose value the one to correct clashes with (<see cref="IsRelated"/>) gets a text of its own, which does not
    /// present it as the one to correct.
    /// </remarks>
    public string? For(params string[] fields)
    {
        if (problem is not { Category: ProblemCategory.Business, RuleCode: { } rule })
            return null;

        if (problem.Fields.Intersect(fields).Any())
        {
            // A rule without a short text still says under its field what to correct, by its text above the form.
            var text = texts[rule, subject];
            return text.ResourceNotFound ? rules[rule].Value : text.Value;
        }

        if (problem.RelatedFields.Intersect(fields).Any())
        {
            var related = texts[$"{rule}.Related", subject];
            return related.ResourceNotFound ? texts["Related"].Value : related.Value;
        }

        return null;
    }

    /// <summary>
    /// Whether the control entering <paramref name="fields"/> holds a value the field to correct clashes with, rather than
    /// the value to correct: presented with the error, it does not get the focus.
    /// </summary>
    public bool IsRelated(params string[] fields) =>
        problem is { Category: ProblemCategory.Business } && !problem.Fields.Intersect(fields).Any()
        && problem.RelatedFields.Intersect(fields).Any();
}
