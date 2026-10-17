using System.Linq.Expressions;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities.Common;

namespace Bdtheque.Api.Search;

/// <summary>An entity the advanced search returns, with the data its criteria compare and the links they follow.</summary>
internal sealed class SearchEntityDefinition(
    EntityKind kind, Type clrType, IReadOnlyList<SearchField> fields, IReadOnlyList<SearchLinkDefinition> links)
{
    private readonly Dictionary<string, SearchField> _fields = fields.ToDictionary(f => f.Key);
    private readonly Dictionary<string, SearchLinkDefinition> _links = links.ToDictionary(l => l.Key);

    public EntityKind Kind => kind;

    public Type ClrType => clrType;

    /// <exception cref="ArgumentException">Not a data of the entity: a technical error, the client building the criterion from the schema.</exception>
    public SearchField Field(string key) =>
        _fields.GetValueOrDefault(key) ?? throw new ArgumentException($"{kind} has no data '{key}'.", nameof(key));

    /// <exception cref="ArgumentException">Not a link of the entity: a technical error, the client building the criterion from the schema.</exception>
    public SearchLinkDefinition Link(string key) =>
        _links.GetValueOrDefault(key) ?? throw new ArgumentException($"{kind} has no link '{key}'.", nameof(key));

    public SearchEntityDescription Describe() => new(kind, fields.Select(f => f.Describe()).ToList(), links.Select(l => l.Describe()).ToList());
}

/// <summary>
/// Declares an entity of the search: the identity and the dates of creation and of modification of the
/// record, common to every entity (modele-metier.md § Attributs communs à toutes les entités), then its own
/// data and links.
/// </summary>
internal sealed class SearchEntityBuilder<T>(EntityKind kind) where T : EntityBase
{
    private readonly List<SearchField> _fields =
    [
        new RecordField("record", Of(r => r.Id), isOptional: false, kind),
        new InstantField("createdAt", Of(r => r.CreatedAt), isOptional: false),
        new InstantField("modifiedAt", Of(r => r.ModifiedAt), isOptional: false),
    ];

    private readonly List<SearchLinkDefinition> _links = [];

    public SearchEntityBuilder<T> Text(string key, Expression<Func<T, string?>> selector, bool isOptional = true, Func<string, string>? normalize = null) =>
        With(new TextField(key, selector, isOptional, normalize));

    public SearchEntityBuilder<T> Number(string key, Expression<Func<T, int?>> selector, bool isOptional = true) =>
        With(new NumberField(key, selector, isOptional));

    public SearchEntityBuilder<T> Boolean(string key, Expression<Func<T, bool?>> selector, bool isOptional = false) =>
        With(new BooleanField(key, selector, isOptional));

    public SearchEntityBuilder<T> Enumeration<TDomain, TContract>(string key, Expression<Func<T, TDomain?>> selector, bool isOptional = true)
        where TDomain : struct, Enum
        where TContract : struct, Enum =>
        With(new EnumerationField<TDomain, TContract>(key, selector, isOptional));

    public SearchEntityBuilder<T> Day(string key, Expression<Func<T, DateOnly?>> selector) => With(new DayField(key, selector, isOptional: true));

    public SearchEntityBuilder<T> Instant(string key, Expression<Func<T, DateTimeOffset?>> selector) =>
        With(new InstantField(key, selector, isOptional: true));

    /// <param name="month">The month of <paramref name="year"/>; <c>null</c> for a date known to the year only.</param>
    public SearchEntityBuilder<T> PartialDate(string key, Expression<Func<T, int?>> year, Expression<Func<T, int?>>? month = null) =>
        With(new PartialDateField(key, year, month));

    public SearchEntityBuilder<T> Amount(string key, Expression<Func<T, decimal?>> selector) => With(new AmountField(key, selector));

    /// <summary>A link to one record, through a navigation.</summary>
    public SearchEntityBuilder<T> One<TTarget>(string key, EntityKind target, Expression<Func<T, TTarget?>> related) where TTarget : class =>
        With(new SearchLinkDefinition(key, target, IsMultiple: false, _ => related));

    /// <summary>A link to at most one record, without navigation to it.</summary>
    public SearchEntityBuilder<T> ZeroOrOne<TTarget>(string key, EntityKind target, Expression<Func<T, IEnumerable<TTarget>>> related) =>
        With(new SearchLinkDefinition(key, target, IsMultiple: false, _ => related));

    /// <summary>A link to several records, through a navigation.</summary>
    public SearchEntityBuilder<T> Many<TTarget>(string key, EntityKind target, Expression<Func<T, IEnumerable<TTarget>>> related) =>
        With(new SearchLinkDefinition(key, target, IsMultiple: true, _ => related));

    /// <summary>A link to several records, queried in the scope of the search (without navigation to them).</summary>
    public SearchEntityBuilder<T> Queried<TTarget>(string key, EntityKind target, Func<SearchScope, Expression<Func<T, IEnumerable<TTarget>>>> related) =>
        With(new SearchLinkDefinition(key, target, IsMultiple: true, related));

    /// <summary>A link to several records through associations with genres or universes (<see cref="SearchAttachments"/>).</summary>
    public SearchEntityBuilder<T> Attached(string key, EntityKind target, Func<SearchScope, LambdaExpression> related) =>
        With(new SearchLinkDefinition(key, target, IsMultiple: true, related, FollowsUniverseHierarchy: target == EntityKind.Universe || kind == EntityKind.Universe));

    public SearchEntityDefinition Build() => new(kind, typeof(T), _fields, _links);

    private SearchEntityBuilder<T> With(SearchField field)
    {
        _fields.Add(field);
        return this;
    }

    private SearchEntityBuilder<T> With(SearchLinkDefinition link)
    {
        _links.Add(link);
        return this;
    }

    private static LambdaExpression Of<TValue>(Expression<Func<T, TValue>> selector) => selector;
}
