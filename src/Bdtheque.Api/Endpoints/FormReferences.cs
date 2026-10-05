using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>The records an administration form references, shared by the resources whose forms list them.</summary>
internal static class FormReferences
{
    /// <summary>
    /// Loads the records with the given identifiers, each of which must still exist: a record deleted
    /// since the form was read is a functional error.
    /// </summary>
    /// <exception cref="EntityNotFoundException">One of the records no longer exists.</exception>
    public static async Task<List<TEntity>> LoadAsync<TEntity>(
        DbSet<TEntity> set, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        where TEntity : EntityBase
    {
        var distinctIds = ids.Distinct().ToList();
        var entities = await set.Where(e => distinctIds.Contains(e.Id)).ToListAsync(cancellationToken);
        var missing = distinctIds.Except(entities.Select(e => e.Id)).ToList();
        return missing.Count == 0 ? entities : throw new EntityNotFoundException(typeof(TEntity), missing[0]);
    }

    /// <summary>Replaces the current associations (genres, universes…) by the wanted ones.</summary>
    public static void ReplaceAssociations<TEntity>(
        IReadOnlyCollection<TEntity> current, IReadOnlyList<TEntity> wanted, Action<TEntity> add, Action<TEntity> remove)
        where TEntity : EntityBase
    {
        foreach (var removed in current.Where(c => wanted.All(w => w.Id != c.Id)).ToList())
            remove(removed);
        foreach (var entity in wanted)
            add(entity);
    }

    /// <summary>Loads the authors of the contributions of a form, each of which must still exist.</summary>
    /// <exception cref="EntityNotFoundException">One of the authors no longer exists.</exception>
    public static async Task<List<(Author Author, DomainEnums.ContributionRole Role)>> LoadContributionsAsync(
        BdthequeDbContext context, IReadOnlyList<ContributionContent> contributions, CancellationToken cancellationToken)
    {
        var authors = await LoadAsync(context.Authors, contributions.Select(c => c.AuthorId).ToList(), cancellationToken);
        return contributions
            .Select(c => (authors.Single(a => a.Id == c.AuthorId), EnumMapping.Map<DomainEnums.ContributionRole>(c.Role)!.Value))
            .ToList();
    }

    /// <summary>The contributions as a form lists them, in a stable order.</summary>
    public static List<ContributionContent> ToContents(IEnumerable<Contribution> contributions) =>
        contributions
            .OrderBy(c => c.Role).ThenBy(c => c.AuthorId)
            .Select(c => new ContributionContent(c.AuthorId, EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value))
            .ToList();
}
