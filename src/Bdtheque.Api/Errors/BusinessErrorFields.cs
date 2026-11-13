using System.Globalization;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
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
    public sealed record Designation(IReadOnlyList<string> Fields, IReadOnlyList<string> Related);

    private sealed record Value(object Record, string Attribute, bool Related = false);

    public static Designation Of(Exception exception, ContentFields fields, BdthequeDbContext context)
    {
        var designated = (exception switch
        {
            DomainRuleViolationException { Record: { } record, Attribute: { } attribute } => [new Value(record, attribute)],
            DbUpdateException { InnerException: PostgresException error } => Refused(error, fields, context),
            PostgresException error => Refused(error, fields, context),
            _ => [],
        }).ToList();
        return new(FieldsOf(designated.Where(value => !value.Related), fields), FieldsOf(designated.Where(value => value.Related), fields));
    }

    private static string[] FieldsOf(IEnumerable<Value> values, ContentFields fields) =>
        [.. values.Select(value => fields.FieldOf(value.Record, value.Attribute)).OfType<string>().Distinct().Order(StringComparer.Ordinal)];

    private static IEnumerable<Value> Refused(PostgresException error, ContentFields fields, BdthequeDbContext context) => error switch
    {
        { SqlState: PostgresErrorCodes.StringDataRightTruncation } => TooLong(context),
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: { } constraint } => AlreadyTaken(context, fields, constraint),
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

    // The records of the index's type the request writes: the only one, or those whose values clash among themselves (the
    // collections of a publisher, saved together, that end up with the same name). Of these, the values to correct are those
    // the request wrote (a collection renamed or added), the others only related (the collection left as it was, already
    // named so), unless it wrote none of them. The values the user enters are the properties of the index other than its
    // foreign keys (the name of a collection, not its publisher).
    private static IEnumerable<Value> AlreadyTaken(BdthequeDbContext context, ContentFields fields, string constraint)
    {
        var index = context.Model.GetEntityTypes().SelectMany(type => type.GetDeclaredIndexes())
            .FirstOrDefault(candidate => candidate.GetDatabaseName() == constraint);
        if (index is null)
            return [];

        var records = context.ChangeTracker.Entries()
            .Where(entry => entry.Metadata == index.DeclaringEntityType && entry.State != EntityState.Deleted)
            .ToList();
        var clashing = records.Count == 1
            ? records
            : [.. records.GroupBy(entry => KeyOf(entry, index), StringComparer.Ordinal).Where(group => group.Count() > 1).SelectMany(group => group)];
        var anyWritten = clashing.Any(entry => fields.Wrote(entry.Entity));
        var entered = index.Properties.Where(property => !property.IsForeignKey()).ToList();
        return clashing.SelectMany(entry => entered.Select(property =>
            new Value(entry.Entity, property.Name, Related: anyWritten && !fields.Wrote(entry.Entity))));
    }

    // The values of the index, compared as entered: the collations of the values the forms enter together are deterministic,
    // and consider two strings equal only if they are the same bytes (documentation of PostgreSQL, « Collation Support »).
    private static string KeyOf(EntityEntry entry, IIndex index) =>
        string.Join('\u001f', index.Properties.Select(property =>
            Convert.ToString(entry.Property(property.Name).CurrentValue, CultureInfo.InvariantCulture)));
}
