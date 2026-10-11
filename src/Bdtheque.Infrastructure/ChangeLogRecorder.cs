using System.Linq.Expressions;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Adds to the save under way the change log entry of every write on a record (modele-metier.md §
/// Entrée du journal des modifications), from what the change tracker is about to write
/// (choix-implementation.md § Journal des modifications : mise en œuvre).
/// </summary>
internal sealed class ChangeLogRecorder(BdthequeDbContext context)
{
    private readonly ChangeLogLabels _labels = new(context);

    /// <summary>
    /// A change of an element a record carries without a record of its own (genre, universe,
    /// contribution, visual), read once its record is known to be modified.
    /// </summary>
    private sealed record CarriedChange(Guid OwnerId, Func<CancellationToken, ValueTask<ChangeLogChange?>> Read);

    public async Task RecordAsync(CancellationToken cancellationToken)
    {
        var records = context.ChangeTracker.Entries<EntityBase>()
            .Where(e => e.State is not EntityState.Detached && ChangeLogMappings.Records.ContainsKey(e.Metadata.ClrType))
            .ToList();
        foreach (var deleted in records.Where(e => e.State == EntityState.Deleted))
            await EnsureCompositionsTrackedAsync(deleted, cancellationToken);

        var carried = CarriedChanges().ToLookup(c => c.OwnerId);
        var entries = new List<ChangeLogEntry>();
        foreach (var record in records)
        {
            var mapping = ChangeLogMappings.Records[record.Metadata.ClrType];
            var entity = record.Entity;
            switch (record.State)
            {
                case EntityState.Added:
                    entries.Add(ChangeLogEntry.Creation(mapping.RecordType, entity.Id, await _labels.OfAsync(entity, cancellationToken)));
                    break;
                case EntityState.Deleted:
                    entries.Add(ChangeLogEntry.Deletion(mapping.RecordType, entity.Id, await _labels.OfAsync(entity, cancellationToken)));
                    break;
                default:
                    var changes = await ChangesAsync(record, mapping, carried[entity.Id], cancellationToken);
                    if (changes.Count > 0)
                    {
                        entries.Add(ChangeLogEntry.Modification(
                            mapping.RecordType, entity.Id, await _labels.OfAsync(entity, cancellationToken), changes));
                    }

                    break;
            }
        }

        context.AddRange(entries);
    }

    private async Task<List<ChangeLogChange>> ChangesAsync(
        EntityEntry<EntityBase> record, RecordMapping mapping, IEnumerable<CarriedChange> carried, CancellationToken cancellationToken)
    {
        var changes = new List<ChangeLogChange>();
        // A root is marked modified by every write on its aggregate, all its properties with it: what
        // changed is told by the values, not by the state.
        if (record.State == EntityState.Modified)
        {
            foreach (var attribute in mapping.Attributes)
            {
                var before = attribute.Properties.Select(p => record.OriginalValues[p]).ToArray();
                var after = attribute.Properties.Select(p => record.CurrentValues[p]).ToArray();
                if (before.SequenceEqual(after))
                    continue;

                changes.Add(new ChangeLogChange(
                    attribute.Attribute,
                    await attribute.Read(before, _labels, cancellationToken),
                    await attribute.Read(after, _labels, cancellationToken)));
            }
        }

        foreach (var change in carried)
        {
            if (await change.Read(cancellationToken) is { } read)
                changes.Add(read);
        }

        return changes;
    }

    // The changes of the elements carried by a record are only read if the record itself is modified:
    // created or deleted, it takes them along.
    private IEnumerable<CarriedChange> CarriedChanges()
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Deleted or EntityState.Modified))
                continue;

            var carried = entry.Entity switch
            {
                Contribution => ContributionChange(entry),
                EditionVisual => VisualChange(entry),
                _ when entry.Metadata.IsPropertyBag => AssociationChange(entry),
                _ => null,
            };
            if (carried is not null)
                yield return carried;
        }
    }

    // A contribution is added or removed, never modified: a removed one is deleted on save
    // (BdthequeDbContext), its owner then being known from its original values only.
    private CarriedChange? ContributionChange(EntityEntry entry)
    {
        if (entry.State == EntityState.Modified)
            return null;

        var values = entry.State == EntityState.Added ? entry.CurrentValues : entry.OriginalValues;
        var ownerId = (values.GetValue<Guid?>(nameof(Contribution.AlbumId)) ?? values.GetValue<Guid?>(nameof(Contribution.SeriesId)))!.Value;
        var authorId = values.GetValue<Guid>(nameof(Contribution.AuthorId));
        var role = values.GetValue<ContributionRole>(nameof(Contribution.Role));

        return new CarriedChange(ownerId, async cancellationToken =>
        {
            ChangeLogValue value = new ContributionValue(authorId, await _labels.AuthorAsync(authorId, cancellationToken), role);
            return Added(entry, ChangeLogAttribute.Contribution, value);
        });
    }

    private static CarriedChange VisualChange(EntityEntry entry)
    {
        var editionId = entry.OriginalValues.GetValue<Guid>(nameof(EditionVisual.EditionId));
        return new CarriedChange(editionId, _ =>
        {
            var before = VisualOf(entry, entry.OriginalValues);
            var after = VisualOf(entry, entry.CurrentValues);
            ChangeLogChange? change = entry.State switch
            {
                EntityState.Added => new ChangeLogChange(ChangeLogAttribute.Visual, null, after),
                EntityState.Deleted => new ChangeLogChange(ChangeLogAttribute.Visual, before, null),
                _ when before == after => null,
                _ => new ChangeLogChange(ChangeLogAttribute.Visual, before, after),
            };
            return ValueTask.FromResult(change);
        });
    }

    private static VisualValue VisualOf(EntityEntry entry, PropertyValues values) =>
        new(
            ((EditionVisual)entry.Entity).Id,
            values.GetValue<VisualType>(nameof(EditionVisual.Type)),
            values.GetValue<int>(nameof(EditionVisual.DisplayOrder)),
            values.GetValue<string>(nameof(EditionVisual.MediaReference)));

    // A row of a join table (genres and universes of albums and series): the record carrying the
    // association is the side the change log records modifications of, the other side the element.
    private CarriedChange? AssociationChange(EntityEntry entry)
    {
        if (entry.State == EntityState.Modified)
            return null;

        var foreignKeys = entry.Metadata.GetForeignKeys().ToList();
        var owner = foreignKeys.SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Album) || fk.PrincipalEntityType.ClrType == typeof(Series));
        var element = foreignKeys.SingleOrDefault(fk => fk != owner);
        if (owner is null || element is null)
            return null;

        var ownerId = (Guid)entry.Property(owner.Properties[0].Name).CurrentValue!;
        var elementId = (Guid)entry.Property(element.Properties[0].Name).CurrentValue!;
        var elementType = element.PrincipalEntityType.ClrType;
        return new CarriedChange(ownerId, async cancellationToken =>
        {
            var (attribute, label) = elementType == typeof(Genre)
                ? (ChangeLogAttribute.Genre, await _labels.OfAsync<Genre>(elementId, cancellationToken))
                : elementType == typeof(Universe)
                    ? (ChangeLogAttribute.Universe, await _labels.OfAsync<Universe>(elementId, cancellationToken))
                    : throw new InvalidOperationException($"No attribute of the change log for an association with {elementType.Name}.");
            return Added(entry, attribute, new ReferenceValue(elementId, label));
        });
    }

    private static ChangeLogChange Added(EntityEntry entry, ChangeLogAttribute attribute, ChangeLogValue value) =>
        entry.State == EntityState.Added ? new ChangeLogChange(attribute, null, value) : new ChangeLogChange(attribute, value, null);

    /// <summary>
    /// Refuses the deletion of a record whose compositions that are records themselves (e.g. the
    /// collections of a publisher) are not all deleted by this save: the database would delete them
    /// by cascade, out of sight of the change log.
    /// </summary>
    private async Task EnsureCompositionsTrackedAsync(EntityEntry<EntityBase> deleted, CancellationToken cancellationToken)
    {
        var compositions = context.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType == deleted.Metadata.ClrType
                         && fk.DeleteBehavior == DeleteBehavior.Cascade
                         && ChangeLogMappings.Records.ContainsKey(fk.DeclaringEntityType.ClrType));
        foreach (var composition in compositions)
        {
            var dependentType = composition.DeclaringEntityType.ClrType;
            var foreignKey = composition.Properties[0];
            var ids = await DependentIdsAsync(dependentType, foreignKey.Name, foreignKey.ClrType, deleted.Entity.Id, cancellationToken);
            var untracked = ids.Where(id => !context.ChangeTracker.Entries<EntityBase>()
                .Any(e => e.Entity.Id == id && e.State == EntityState.Deleted && e.Metadata.ClrType == dependentType)).ToList();
            if (untracked.Count > 0)
                throw new InvalidOperationException(
                    $"Deleting {deleted.Metadata.ClrType.Name} {deleted.Entity.Id} would delete by cascade {untracked.Count} {dependentType.Name} " +
                    "out of sight of the change log: load them with it.");
        }
    }

    private Task<List<Guid>> DependentIdsAsync(Type dependentType, string foreignKey, Type foreignKeyType, Guid principalId, CancellationToken cancellationToken) =>
        (Task<List<Guid>>)typeof(ChangeLogRecorder)
            .GetMethod(nameof(DependentIdsOfAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(dependentType)
            .Invoke(this, [foreignKey, foreignKeyType, principalId, cancellationToken])!;

    private Task<List<Guid>> DependentIdsOfAsync<TDependent>(
        string foreignKey, Type foreignKeyType, Guid principalId, CancellationToken cancellationToken)
        where TDependent : EntityBase
    {
        var dependent = Expression.Parameter(typeof(TDependent));
        var linked = Expression.Lambda<Func<TDependent, bool>>(
            Expression.Equal(
                Expression.Call(typeof(EF), nameof(EF.Property), [foreignKeyType], dependent, Expression.Constant(foreignKey)),
                Expression.Constant(principalId, foreignKeyType)),
            dependent);
        return context.Set<TDependent>().AsNoTracking().IgnoreAutoIncludes().Where(linked).Select(d => d.Id).ToListAsync(cancellationToken);
    }
}
