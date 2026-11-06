using System.Globalization;
using Bdtheque.Api.Search;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Search;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Contract = Bdtheque.Contracts.Admin;
using ContractEnums = Bdtheque.Contracts.Enums;
using Model = Bdtheque.Domain.Entities;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// Reading of the change log (<c>/admin/change-log</c>), consulted in the administration only
/// (modele-metier/entree-du-journal-des-modifications.md). The entries are data, mapped as they
/// were recorded: the same entry always reads the same.
/// </summary>
internal static class ChangeLogEndpoints
{
    public const int MaxLatestRecordCount = 20;

    public static void MapChangeLog(this RouteGroupBuilder admin)
    {
        admin.MapGet("/change-log", ListAsync);
        admin.MapGet("/change-log/latest-records", LatestRecordsAsync);
    }

    // From the latest entry to the oldest; the entries of a same save share their date, and follow
    // the order of their time-ordered identifiers.
    private static async Task<Page<Contract.ChangeLogEntryItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, int page = 1, int pageSize = Paging.DefaultSize)
    {
        var entries = await context.ChangeLog.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToPageAsync(page, pageSize, e => e, cancellationToken);

        return new Page<Contract.ChangeLogEntryItem>(entries.Items.Select(ToItem).ToList(), entries.Number, entries.Size, entries.TotalCount);
    }

    /// <summary>
    /// The latest records created or modified (visuel/administration/contenu.md, Dernières modifications): each record
    /// once, by its latest entry, from the latest to the oldest; a record whose latest entry is its deletion is no record
    /// any more, and is left out.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A count outside 1 to <see cref="MaxLatestRecordCount"/>: a technical error, the frontend asking for it, never the user.
    /// </exception>
    private static async Task<IReadOnlyList<Contract.ChangeLogEntryItem>> LatestRecordsAsync(
        int count, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxLatestRecordCount);

        // The records first, then their entries: EF Core translates the first entry of a group as the last operator of a
        // query only, never filtered nor ordered afterwards (EF Core documentation, Complex query operators). A record
        // whose latest entry is its deletion is one that has a deletion at all, an identifier never being given again.
        var records = await context.ChangeLog
            .GroupBy(e => new { e.RecordType, e.RecordId })
            .Where(record => !record.Any(e => e.Operation == Bdtheque.Domain.Enums.ChangeLogOperation.Deletion))
            .OrderByDescending(record => record.Max(e => e.CreatedAt))
            .ThenBy(record => record.Key.RecordId)
            .Take(count)
            .Select(record => record.Key)
            .ToListAsync(cancellationToken);

        var ids = records.Select(r => r.RecordId).ToList();
        var entries = await context.ChangeLog.AsNoTracking()
            .Where(e => ids.Contains(e.RecordId))
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

        return records
            .Select(r => ToItem(entries.First(e => e.RecordType == r.RecordType && e.RecordId == r.RecordId)))
            .ToList();
    }

    private static Contract.ChangeLogEntryItem ToItem(Model.ChangeLogEntry entry) =>
        new(
            entry.Id,
            entry.CreatedAt,
            EnumMapping.Map<ContractEnums.RecordType>(entry.RecordType)!.Value,
            entry.RecordId,
            ToLabel(entry.Label),
            EnumMapping.Map<ContractEnums.ChangeLogOperation>(entry.Operation)!.Value,
            entry.Changes.Select(ToChange).ToList());

    private static Contract.ChangeLogChange ToChange(Model.ChangeLogChange change) =>
        new(
            EnumMapping.Map<ContractEnums.ChangeLogAttribute>(change.Attribute)!.Value,
            ToValue(change.Attribute, change.Before),
            ToValue(change.Attribute, change.After));

    private static Contract.ChangeLogValue? ToValue(Bdtheque.Domain.Enums.ChangeLogAttribute attribute, Model.ChangeLogValue? value) =>
        value switch
        {
            null => null,
            Model.TextValue text => new Contract.TextValue(text.Value),
            Model.NumberValue number => new Contract.NumberValue(number.Value),
            Model.FlagValue flag => new Contract.FlagValue(flag.Value),
            Model.DateValue date => new Contract.DateValue(date.Value),
            Model.PartialDateValue date => new Contract.PartialDateValue(date.Year, date.Month),
            Model.AmountValue amount => new Contract.AmountValue(amount.Amount, amount.Currency),
            Model.EnumValue member => ToEnumValue(attribute, member.Value),
            Model.ReferenceValue reference => new Contract.ReferenceValue(reference.Id, ToLabel(reference.Label)),
            Model.ContributionValue contribution => new Contract.ContributionValue(
                contribution.AuthorId, ToAuthor(contribution.Author), EnumMapping.Map<ContractEnums.ContributionRole>(contribution.Role)!.Value),
            Model.VisualValue visual => new Contract.VisualValue(
                visual.Id, EnumMapping.Map<ContractEnums.VisualType>(visual.Type)!.Value, visual.DisplayOrder),
            Model.SearchCriteriaValue criteria => new Contract.SearchCriteriaValue(SearchCriteriaJson.Deserialize(criteria.Criteria)),
            Model.SearchVariablesValue variables => new Contract.SearchVariablesValue(
                variables.Variables.Select(v => new SavedSearchVariable(v.Name, v.Location, v.DefaultValue)).ToList()),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value.GetType().Name, "Unknown kind of change log value."),
        };

    // Named after the contract enumeration of the same name as the domain one (ContractEnumTests). A
    // value no member bears any more — a member retired since the entry was written — keeps its number.
    private static Contract.EnumValue ToEnumValue(Bdtheque.Domain.Enums.ChangeLogAttribute attribute, int value)
    {
        var domainType = ChangeLogEnumerations.Of(attribute);
        var contractType = typeof(ContractEnums.AlbumType).Assembly.GetType($"{typeof(ContractEnums.AlbumType).Namespace}.{domainType.Name}", throwOnError: true)!;
        return new Contract.EnumValue(
            contractType.Name, Enum.GetName(contractType, Enum.ToObject(contractType, value)) ?? value.ToString(CultureInfo.InvariantCulture));
    }

    private static Contract.RecordLabel ToLabel(Model.RecordLabel label) =>
        label switch
        {
            Model.AlbumLabel album => ToAlbum(album),
            Model.EditionLabel edition => ToEdition(edition),
            Model.PurchaseIntentLabel intent => new Contract.PurchaseIntentLabel(
                intent.Album is null ? null : ToAlbum(intent.Album), intent.Edition is null ? null : ToEdition(intent.Edition)),
            Model.AuthorLabel author => ToAuthor(author),
            Model.PublisherCollectionLabel collection => new Contract.PublisherCollectionLabel(collection.Name, collection.PublisherName),
            Model.NameLabel name => new Contract.NameLabel(name.Name),
            Model.ApplicationSettingsLabel => new Contract.ApplicationSettingsLabel(),
            _ => throw new ArgumentOutOfRangeException(nameof(label), label.GetType().Name, "Unknown kind of record label."),
        };

    private static Contract.AlbumLabel ToAlbum(Model.AlbumLabel album) =>
        new(
            album.Title,
            album.SeriesTitle,
            EnumMapping.Map<ContractEnums.AlbumType>(album.Type)!.Value,
            album.IsSpecialIssue,
            album.VolumeNumber,
            album.StartVolumeNumber,
            album.EndVolumeNumber);

    private static Contract.EditionLabel ToEdition(Model.EditionLabel edition) =>
        new(ToAlbum(edition.Album), edition.PublisherName, edition.PublisherCollectionName, edition.PublicationYear, edition.Isbn);

    private static Contract.AuthorLabel ToAuthor(Model.AuthorLabel author) => new(author.LastName, author.FirstName, author.Pseudonym);
}
