using System.Globalization;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Bdtheque.Api.Errors;

/// <summary>
/// The fields of the content of the request that a business error designates, the first one being the field the user
/// corrects first (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md): the value a rule of the
/// domain designates, or the values the database refused at the saving — a text longer than its column, or a value
/// already taken — found among the records the request writes, as only the model, not the database, names them.
/// </summary>
internal static class BusinessErrorFields
{
    /// <param name="Fields">The fields of the values to correct.</param>
    /// <param name="Related">
    /// The fields of the values those to correct clash with, entered in the same content and left as they were (the other
    /// collection already named so): presented with the error, not to correct.
    /// </param>
    /// <param name="Records">The records of the values to correct, whose situation a rule may depend on.</param>
    public sealed record Designation(IReadOnlyList<string> Fields, IReadOnlyList<string> Related, IReadOnlyList<object> Records);

    private sealed record Value(object Record, string Attribute, bool Related = false);

    public static async Task<Designation> OfAsync(
        Exception exception, ContentFields fields, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var designated = (exception switch
        {
            DomainRuleViolationException { Record: { } record, Attribute: { } attribute } => [new Value(record, attribute)],
            DbUpdateException { InnerException: PostgresException error } => await RefusedAsync(error, fields, context, cancellationToken),
            PostgresException error => await RefusedAsync(error, fields, context, cancellationToken),
            _ => [],
        }).ToList();
        return new(
            FieldsOf(designated.Where(value => !value.Related), fields), FieldsOf(designated.Where(value => value.Related), fields),
            [.. designated.Where(value => !value.Related).Select(value => value.Record).Distinct(ReferenceEqualityComparer.Instance)]);
    }

    private static string[] FieldsOf(IEnumerable<Value> values, ContentFields fields) =>
        [.. values.Select(value => fields.FieldOf(value.Record, value.Attribute)).OfType<string>().Distinct().Order(StringComparer.Ordinal)];

    private static async Task<IEnumerable<Value>> RefusedAsync(
        PostgresException error, ContentFields fields, BdthequeDbContext context, CancellationToken cancellationToken) => error switch
    {
        { SqlState: PostgresErrorCodes.StringDataRightTruncation } => TooLong(context),
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: { } constraint } =>
            await AlreadyTakenAsync(context, fields, constraint, cancellationToken),
        _ => [],
    };

    // PostgreSQL names neither the column nor the row of a value too long for its column: the values the save writes are
    // measured against the lengths the model declares for the schema, in characters, as PostgreSQL counts them.
    private static IEnumerable<Value> TooLong(BdthequeDbContext context) =>
        from entry in context.ChangeTracker.Entries()
        where entry.State is EntityState.Added or EntityState.Modified
        from property in entry.Properties
        where property.Metadata.GetMaxLength() is { } maxLength
              && property.CurrentValue is string value
              && value.EnumerateRunes().Count() > maxLength
        select new Value(entry.Entity, property.Metadata.Name);

    // The records of the index's type whose values clash among themselves (the collections of a publisher, saved together,
    // that end up with the same name), or, if none does, those the request wrote whose values a row it did not read holds
    // already (a genre added on the spot, beside the authors an album credits, read with it). Of these, the values to correct
    // are those the request wrote (a collection renamed or added), the others only related (the collection left as it was,
    // already named so), unless it wrote none of them. The values the user enters are the properties of the index other than
    // its foreign keys (the name of a collection, not its publisher).
    private static async Task<IEnumerable<Value>> AlreadyTakenAsync(
        BdthequeDbContext context, ContentFields fields, string constraint, CancellationToken cancellationToken)
    {
        var index = context.Model.GetEntityTypes().SelectMany(type => type.GetDeclaredIndexes())
            .FirstOrDefault(candidate => candidate.GetDatabaseName() == constraint);
        if (index is null)
            return [];

        var records = context.ChangeTracker.Entries()
            .Where(entry => entry.Metadata == index.DeclaringEntityType && entry.State != EntityState.Deleted)
            .ToList();
        var insensitive = CaseAndAccentInsensitive(context, index);
        List<EntityEntry> clashing = [.. records.GroupBy(entry => KeyOf(entry, index, insensitive), StringComparer.Ordinal)
            .Where(group => group.Count() > 1).SelectMany(group => group)];
        if (clashing.Count == 0)
            clashing = await HeldByAnotherRowAsync(context, index, [.. records.Where(entry => fields.Wrote(entry.Entity))], cancellationToken);
        var anyWritten = clashing.Any(entry => fields.Wrote(entry.Entity));
        var entered = index.Properties.Where(property => !property.IsForeignKey()).ToList();
        return clashing.SelectMany(entry => entered.Select(property =>
            new Value(entry.Entity, property.Name, Related: anyWritten && !fields.Wrote(entry.Entity))));
    }

    // Of the records the request wrote, those whose values of the index another row holds. A single one is the record refused;
    // among several (two genres added on the spot), the database tells which: the request having failed, its transaction is
    // rolled back when this runs, and the database answers as it was before it. The column compares the values as its
    // collation does (case and accents ignored for a name), the parameter taking the collation of the column.
    private static async Task<List<EntityEntry>> HeldByAnotherRowAsync(
        BdthequeDbContext context, IIndex index, List<EntityEntry> written, CancellationToken cancellationToken)
    {
        if (written.Count <= 1)
            return written;

        var type = index.DeclaringEntityType;
        var table = StoreObjectIdentifier.Table(type.GetTableName()!, type.GetSchema());
        var sql = context.GetService<ISqlGenerationHelper>();
        var key = type.FindPrimaryKey()!.Properties.Single();
        var held = new List<EntityEntry>();
        foreach (var entry in written)
        {
            var values = new List<object>();
            var conditions = new List<string>();
            foreach (var property in index.Properties)
            {
                var column = sql.DelimitIdentifier(property.GetColumnName(table)!);
                if (ProviderValue(entry, property) is { } value)
                {
                    values.Add(value);
                    conditions.Add($"{column} = {{{values.Count - 1}}}");
                }
                else
                    conditions.Add($"{column} IS NULL");
            }

            values.Add(ProviderValue(entry, key)!);
            conditions.Add($"{sql.DelimitIdentifier(key.GetColumnName(table)!)} <> {{{values.Count - 1}}}");
            var query = $"SELECT EXISTS (SELECT 1 FROM {sql.DelimitIdentifier(table.Name, table.Schema)} WHERE {string.Join(" AND ", conditions)}) AS \"Value\"";
            if (await context.Database.SqlQueryRaw<bool>(query, [.. values]).SingleAsync(cancellationToken))
                held.Add(entry);
        }

        return held;
    }

    private static object? ProviderValue(EntityEntry entry, IReadOnlyProperty property)
    {
        var value = entry.Property(property.Name).CurrentValue;
        return value is null ? null : property.GetTypeMapping().Converter?.ConvertToProvider(value) ?? value;
    }

    // The values of the index, compared as their column compares them: a deterministic collation considers two strings equal
    // only if they are the same bytes (documentation of PostgreSQL, « Collation Support »); the case- and accent-insensitive
    // one, carried by the names unique regardless of them, compares their base letters only, as the primary strength of the
    // French collation does, which a sort key ignoring case and accents reproduces.
    private static string KeyOf(EntityEntry entry, IIndex index, HashSet<string> insensitive) =>
        string.Join('\u001f', index.Properties.Select(property => entry.Property(property.Name).CurrentValue switch
        {
            string text when insensitive.Contains(property.Name) =>
                Convert.ToBase64String(FrenchComparison.GetSortKey(text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace).KeyData),
            var value => Convert.ToString(value, CultureInfo.InvariantCulture),
        }));

    // The properties of the index under the case- and accent-insensitive collation, read on the design-time model: the
    // runtime model keeps no collation (EF Core, « The requested configuration is not stored in the read-optimized model »).
    private static HashSet<string> CaseAndAccentInsensitive(BdthequeDbContext context, IIndex index)
    {
        var designed = context.GetService<IDesignTimeModel>().Model.FindEntityType(index.DeclaringEntityType.Name);
        return [.. index.Properties.Select(property => property.Name)
            .Where(name => designed?.FindProperty(name)?.GetCollation() == BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation)];
    }

    private static readonly CompareInfo FrenchComparison = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;
}
