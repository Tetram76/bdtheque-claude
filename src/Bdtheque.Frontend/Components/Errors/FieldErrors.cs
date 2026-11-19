using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
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
    public string? For(params string[] fields) => Text(field => fields.Contains(field), subject);

    /// <summary>
    /// What to correct in the control that enters <paramref name="fields"/> of a line of a list entered with the form (a
    /// collection of a publisher), the text citing that line by <paramref name="name"/>, as typed, rather than by the subject
    /// of the form: when two lines are bordered, the name cited says which one to correct (decision of the user, #464).
    /// </summary>
    public string? ForNamed(string name, params string[] fields) => Text(field => fields.Contains(field), name);

    /// <summary>
    /// What to correct in the record added on the spot under <paramref name="key"/> (an author, a genre, a universe, a series, a
    /// publisher, a publisher collection), which the last saving sent with its content (<paramref name="sent"/>), where the error designates it: any of its fields, the
    /// text citing that record, as its own list or panel does (visuel/saisie-album/contenu.md); none if the error designates
    /// none of them, or if that saving did not send it.
    /// </summary>
    public string? ForAdded(NewRecords? sent, Guid key) => Added(sent, key) is var (path, name)
        ? Text(field => field.StartsWith($"{path}/", StringComparison.Ordinal), name)
        : null;

    // Where the content enters the records added on the spot (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
    private static (string Path, string Name)? Added(NewRecords? sent, Guid key)
    {
        if (sent is null)
            return null;
        IEnumerable<(Guid Key, string Path, string Name)> places =
        [
            .. sent.Authors.Select((a, i) => (a.Key, $"/newRecords/authors/{i}", RecordSearch.AuthorName(a.LastName, a.FirstName, a.Pseudonym))),
            .. sent.Genres.Select((g, i) => (g.Key, $"/newRecords/genres/{i}", g.Label)),
            .. sent.Universes.Select((u, i) => (u.Key, $"/newRecords/universes/{i}", u.Name)),
            .. sent.Series.Select((s, i) => (s.Key, $"/newRecords/series/{i}", s.Title)),
            .. sent.Publishers.Select((p, i) => (p.Key, $"/newRecords/publishers/{i}", p.Name)),
            .. sent.PublisherCollections.Select((c, i) => (c.Key, $"/newRecords/publisherCollections/{i}", c.Name)),
        ];
        return places.Where(place => place.Key == key).Select(place => ((string, string)?)(place.Path, place.Name)).FirstOrDefault();
    }

    private string? Text(Func<string, bool> designates, string named)
    {
        if (problem is not { Category: ProblemCategory.Business, RuleCode: { } rule })
            return null;

        if (problem.Fields.Any(designates))
        {
            // A rule without a short text still says under its field what to correct, by its text above the form.
            var text = texts[rule, named];
            return text.ResourceNotFound ? rules[rule].Value : text.Value;
        }

        if (problem.RelatedFields.Any(designates))
        {
            var related = texts[$"{rule}.Related", named];
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
