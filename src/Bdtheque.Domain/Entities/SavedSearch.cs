using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// An advanced search the administrator saved under a name, replayed in the consultation with the values
/// of its variables (modele-metier.md § Recherche enregistrée; fonctionnel.md § Recherches enregistrées).
/// </summary>
/// <remarks>
/// The criteria are the JSON the advanced search takes (its contract), which the API reads and checks
/// against what the search describes; the domain only sees a tree whose constants are strings in arrays
/// named <c>values</c>, those a variable can replace. A variable designates its constant by a JSON pointer
/// (RFC 6901) into the criteria, and that constant is its default value: changing the default value is
/// changing the constant.
/// </remarks>
public sealed class SavedSearch : EntityBase, IAggregateRoot
{
    private const string ValuesProperty = "values";

    // The variables are told apart by their name alone at replay: as the names of the searches themselves,
    // they ignore case and accents.
    private static readonly CompareInfo FrenchOrder = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;
    private const CompareOptions IgnoringCaseAndAccents = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    // Stored as entered, read back by the API: the texts keep their own characters.
    private static readonly JsonSerializerOptions WriteOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public string Name { get; private set; } = string.Empty;
    public EntityKind Entity { get; private set; }
    public string Criteria { get; private set; } = string.Empty;
    public SearchVariables Variables { get; private set; } = SearchVariables.Empty;

    // EF Core parameterless constructor
    private SavedSearch() { }

    public SavedSearch(string name, EntityKind entity, string criteria, IEnumerable<SearchVariable> variables)
    {
        SetName(name);
        SetSearch(entity, criteria, variables);
    }

    public void SetName(string name) =>
        Name = DomainText.Required(name, DomainRules.SavedSearchNameRequired, "A saved search must have a name.");

    /// <summary>
    /// Replaces what the search looks for: its variables designate constants of its criteria, so the
    /// three are written together.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Criteria that are not a JSON object, or a variable that does not designate a constant of them, or
    /// the constant of another variable: the client builds them from the criteria it sends.
    /// </exception>
    public void SetSearch(EntityKind entity, string criteria, IEnumerable<SearchVariable> variables)
    {
        EnumGuard.EnsureDefined(entity, nameof(entity));
        var tree = Parse(criteria);
        var checkedVariables = new List<SearchVariable>();
        foreach (var variable in variables)
        {
            ArgumentNullException.ThrowIfNull(variable);
            var name = DomainText.Required(variable.Name, DomainRules.SavedSearchVariableNameRequired, "A variable must have a name.");
            if (checkedVariables.Exists(v => FrenchOrder.Compare(v.Name, name, IgnoringCaseAndAccents) == 0))
                throw new DomainRuleViolationException(DomainRules.SavedSearchVariableNameAlreadyUsed, $"Two variables are named {name}.");
            if (checkedVariables.Exists(v => v.Location == variable.Location))
                throw new ArgumentException($"Two variables replace the constant at {variable.Location}.", nameof(variables));
            _ = ConstantAt(tree, variable.Location);
            checkedVariables.Add(variable with { Name = name });
        }

        Entity = entity;
        Criteria = criteria;
        Variables = new SearchVariables(checkedVariables);
    }

    /// <summary>The default value of one of the variables: the constant it replaces.</summary>
    public string DefaultValueOf(SearchVariable variable)
    {
        EnsureOwn(variable);
        return DefaultValueOf(Criteria, variable);
    }

    /// <summary>
    /// The default value of a variable of <paramref name="criteria"/>, for a reader of the values a saved
    /// search had (e.g. before a modification).
    /// </summary>
    public static string DefaultValueOf(string criteria, SearchVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        var (values, index) = ConstantAt(Parse(criteria), variable.Location);
        return values[index]!.GetValue<string>();
    }

    /// <summary>
    /// The criteria to replay: each variable given a value by its name replaced by it, the others keeping
    /// their default value.
    /// </summary>
    /// <exception cref="ArgumentException">A value for a variable the search does not have.</exception>
    public string CriteriaWith(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var tree = Parse(Criteria);
        foreach (var (name, value) in values)
        {
            var variable = Variables.SingleOrDefault(v => v.Name == name)
                           ?? throw new ArgumentException($"The search has no variable {name}.", nameof(values));
            var (array, index) = ConstantAt(tree, variable.Location);
            array[index] = JsonValue.Create(value);
        }

        return tree.ToJsonString(WriteOptions);
    }

    private void EnsureOwn(SearchVariable variable)
    {
        if (!Variables.Contains(variable))
            throw new ArgumentException($"The search has no variable {variable.Name} at {variable.Location}.", nameof(variable));
    }

    private static JsonObject Parse(string criteria)
    {
        try
        {
            return JsonNode.Parse(criteria) as JsonObject
                   ?? throw new ArgumentException("The criteria of a search are a JSON object.", nameof(criteria));
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The criteria of a search are a JSON object.", nameof(criteria), exception);
        }
    }

    // The constant a JSON pointer designates: a string of an array of values.
    private static (JsonArray Values, int Index) ConstantAt(JsonObject tree, string location)
    {
        if (string.IsNullOrEmpty(location) || location[0] != '/')
            throw new ArgumentException($"{location} is not a JSON pointer into the criteria.", nameof(location));

        var tokens = location[1..].Split('/').Select(t => t.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal)).ToArray();
        JsonNode? node = tree;
        JsonNode? parent = null;
        foreach (var token in tokens)
        {
            parent = node;
            node = node switch
            {
                JsonObject obj => obj[token],
                JsonArray array when IsIndex(token, out var i) && i < array.Count => array[i],
                _ => null,
            };
            if (node is null)
                throw new ArgumentException($"Nothing of the criteria is at {location}.", nameof(location));
        }

        if (tokens.Length < 2 || tokens[^2] != ValuesProperty || parent is not JsonArray values
            || node is not JsonValue constant || constant.GetValueKind() != JsonValueKind.String)
            throw new ArgumentException($"{location} does not designate a value of a comparison.", nameof(location));

        return (values, int.Parse(tokens[^1], CultureInfo.InvariantCulture));
    }

    private static bool IsIndex(string token, out int index)
    {
        index = 0;
        return token.Length > 0 && token.All(char.IsAsciiDigit) && (token == "0" || token[0] != '0')
               && int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }
}

/// <summary>A constant of the criteria of a saved search replaced by a variable, whose value is asked at replay.</summary>
/// <param name="Name">What the value asked is named after.</param>
/// <param name="Location">JSON pointer (RFC 6901) to the constant in the criteria.</param>
public sealed record SearchVariable(string Name, string Location);

/// <summary>The variables of a saved search, in their order, compared by value.</summary>
public sealed class SearchVariables : IReadOnlyList<SearchVariable>, IEquatable<SearchVariables>
{
    private readonly SearchVariable[] _variables;

    public SearchVariables(IEnumerable<SearchVariable> variables) => _variables = variables.ToArray();

    public static SearchVariables Empty { get; } = new([]);

    public int Count => _variables.Length;

    public SearchVariable this[int index] => _variables[index];

    public IEnumerator<SearchVariable> GetEnumerator() => ((IEnumerable<SearchVariable>)_variables).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(SearchVariables? other) => other is not null && _variables.SequenceEqual(other._variables);

    public override bool Equals(object? obj) => Equals(obj as SearchVariables);

    public override int GetHashCode() => _variables.Aggregate(0, (hash, variable) => HashCode.Combine(hash, variable));
}
