using System.Text.Json;
using Bdtheque.Domain.Common;

namespace Bdtheque.Api.Errors;

/// <summary>
/// Where, in the content a write request sent, each record it writes is entered: what lets a business error designate
/// the field of the value in cause, a JSON Pointer (RFC 6901) into that content (e.g. <c>/collections/1/name</c>), whatever
/// raised it — the domain applying the content, or the database refusing it at the saving
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md). Scoped to the request: the endpoint that
/// applies a content places its records as it goes, and the exception handler reads them.
/// </summary>
internal sealed class ContentFields
{
    private readonly Dictionary<object, Location> _places = new(ReferenceEqualityComparer.Instance);

    /// <summary>Places a record of the content at <paramref name="path"/> (<c>""</c> for the record the content describes).</summary>
    /// <param name="renamed">
    /// The fields of the content named otherwise than the properties of the record they set, by property, each a pointer
    /// relative to <paramref name="path"/> (e.g. the sort key of an album, entered as <c>/manualSortKey</c>); any other
    /// property is entered under its own name, as the content serializes it.
    /// </param>
    public void Place(object record, string path = "", IReadOnlyDictionary<string, string>? renamed = null) =>
        _places[record] = new Location(path, renamed);

    /// <summary>
    /// Creates the record entered at <paramref name="path"/>, and places it: a rule its creation breaks designates its
    /// field as well, although the record never comes into being.
    /// </summary>
    public TRecord Create<TRecord>(string path, Func<TRecord> create, IReadOnlyDictionary<string, string>? renamed = null)
        where TRecord : class
    {
        try
        {
            var record = create();
            Place(record, path, renamed);
            return record;
        }
        catch (DomainRuleViolationException violation) when (violation.Record is TRecord record && !_places.ContainsKey(record))
        {
            Place(record, path, renamed);
            throw;
        }
    }

    /// <summary>The field of the content that entered <paramref name="attribute"/> of <paramref name="record"/>; none if it entered none.</summary>
    public string? FieldOf(object record, string attribute) =>
        _places.TryGetValue(record, out var location) ? location.FieldOf(attribute) : null;

    private sealed record Location(string Path, IReadOnlyDictionary<string, string>? Renamed)
    {
        public string FieldOf(string attribute) =>
            Renamed is not null && Renamed.TryGetValue(attribute, out var renamed)
                ? Path + renamed
                : $"{Path}/{JsonNamingPolicy.CamelCase.ConvertName(attribute)}";
    }
}
