using System.Globalization;
using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// The collections a form of a series or of an edition chooses its collection among (visuel/saisie-serie/contenu.md): all those
/// of the publisher chosen, read with the form, and those the form added to it on the spot, created only with the form.
/// </summary>
public static class CollectionChoice
{
    /// <summary>
    /// The collections of <paramref name="publisher"/>, by name, those added on the spot among the others; none without publisher.
    /// A collection added to another publisher is not listed, but stays added: chosen again, its publisher lists it again.
    /// </summary>
    /// <param name="read">Those read, by name; none while they are read, or if they could not be.</param>
    public static IReadOnlyList<RecordOption> Options(
        IReadOnlyList<RecordOption>? read, IEnumerable<NewPublisherCollection> added, RecordOption? publisher) =>
        publisher is null
            ? []
            :
            [
                .. (read ?? []).Concat(added.Where(c => c.PublisherId == publisher.Id).Select(c => new RecordOption(c.Key, c.Name, null)))
                .OrderBy(c => c.Label, StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase)),
            ];
}
