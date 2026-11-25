using System.Globalization;
using Bdtheque.Api.Search;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Search;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract = Bdtheque.Contracts.Admin;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;
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

    // From the latest entry to the oldest; the entries of a same save share their date, and follow the order of their
    // time-ordered identifiers (visuel/journal/journal-des-modifications.md). Its criteria combine: the search on the label of
    // the records, their type, the operation, the period (two days, both included, in the time zone of the browser, as an
    // advanced search compares a date and time by its day) and a single record.
    private static async Task<Contract.ChangeLogList> ListAsync(
        BdthequeDbContext context,
        CancellationToken cancellationToken,
        string? q = null,
        ContractEnums.RecordType? recordType = null,
        ContractEnums.ChangeLogOperation? operation = null,
        DateOnly? from = null,
        DateOnly? to = null,
        Guid? record = null,
        int page = 1,
        int pageSize = Paging.DefaultSize,
        [FromQuery(Name = AdvancedSearchQuery.TimeZoneParameter)] string? timeZone = null)
    {
        var zone = SearchTimeZones.Resolve(timeZone);
        var entries = Searched(context, q).AsNoTracking();
        if (recordType is { } type)
        {
            var domainType = EnumMapping.Map<DomainEnums.RecordType>(type)!.Value;
            entries = entries.Where(e => e.RecordType == domainType);
        }

        if (operation is { } kind)
        {
            var domainOperation = EnumMapping.Map<DomainEnums.ChangeLogOperation>(kind)!.Value;
            entries = entries.Where(e => e.Operation == domainOperation);
        }

        if (from is { } first)
        {
            var start = InstantField.StartOf(first, zone);
            entries = entries.Where(e => e.CreatedAt >= start);
        }

        if (to is { } last)
        {
            var end = InstantField.StartOf(last.AddDays(1), zone);
            entries = entries.Where(e => e.CreatedAt < end);
        }
        if (record is { } id)
            entries = entries.Where(e => e.RecordId == id);

        var found = await entries
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToPageAsync(page, pageSize, e => e, cancellationToken);
        var parents = await ReadParentsAsync(context, found.Items, cancellationToken);
        var deleted = await ReadDeletedAsync(context, found.Items, cancellationToken);
        var wholeCount = await context.ChangeLog.CountAsync(cancellationToken);
        var filtered = record is { } recordId ? await ReadRecordAsync(context, recordId, cancellationToken) : null;

        return new Contract.ChangeLogList(
            new Page<Contract.ChangeLogEntryItem>(
                found.Items.Select(e => ToItem(e, parents, !deleted.Contains(e.RecordId))).ToList(), found.Number, found.Size, found.TotalCount),
            wholeCount,
            filtered);
    }

    /// <summary>
    /// The entries whose label contains <paramref name="text"/>, ignoring case and accents, as the lists search
    /// (choix-implementation/api/recherche.md); all of them if it is blank. The label is stored in <c>jsonb</c> by its
    /// components: every text of it is looked into, those of the labels it holds too (the album of an edition or of a purchase
    /// intent), never the kinds that tell them apart, nor its numbers and flags.
    /// </summary>
    private static IQueryable<Model.ChangeLogEntry> Searched(BdthequeDbContext context, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return context.ChangeLog;

        var pattern = $"%{CatalogFilters.EscapeLike(text.Trim())}%";
        // EF Core translates no JSON path over a converted column: the condition is written in SQL, a parameter carrying the
        // pattern, and the rest of the query composed over it.
        return context.ChangeLog.FromSqlRaw(
            $$"""
            SELECT * FROM "ChangeLogEntries"
            WHERE jsonb_path_query_array("Label", '{{LabelTextsPath}}')::text COLLATE "{{BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation}}" LIKE {0} ESCAPE '\'
            """,
            pattern);
    }

    // Every string of the label, in any of its objects, the kind of each excepted.
    private const string LabelTextsPath =
        "strict $.** ? (@.type() == \"object\").keyvalue() ? (@.key != \"kind\" && @.value.type() == \"string\").value";

    /// <summary>The records of the entries deleted since: an identifier is never given again, so any deletion is the last.</summary>
    private static async Task<HashSet<Guid>> ReadDeletedAsync(
        BdthequeDbContext context, IReadOnlyCollection<Model.ChangeLogEntry> entries, CancellationToken cancellationToken)
    {
        var ids = entries.Select(e => e.RecordId).Distinct().ToList();
        return (await context.ChangeLog
            .Where(e => ids.Contains(e.RecordId) && e.Operation == DomainEnums.ChangeLogOperation.Deletion)
            .Select(e => e.RecordId)
            .ToListAsync(cancellationToken)).ToHashSet();
    }

    // The record of a filter, by its latest entry.
    private static async Task<Contract.ChangeLogRecord?> ReadRecordAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken)
    {
        var latest = await context.ChangeLog.AsNoTracking()
            .Where(e => e.RecordId == id)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return latest is null
            ? null
            : new Contract.ChangeLogRecord(EnumMapping.Map<ContractEnums.RecordType>(latest.RecordType)!.Value, ToLabel(latest.Label));
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
        var latest = records.Select(r => entries.First(e => e.RecordType == r.RecordType && e.RecordId == r.RecordId)).ToList();
        var parents = await ReadParentsAsync(context, latest, cancellationToken);

        return latest.Select(e => ToItem(e, parents, recordExists: true)).ToList();
    }

    /// <summary>
    /// The parent of each record of the entries whose page is addressed under it, by the identifier of the record: the album
    /// of an edition or of a purchase intent, the publisher of a collection. Read rather than written in the entry, which
    /// holds what the record was, not where it is: a record deleted since has none, and has no page to lead to.
    /// </summary>
    private static async Task<IReadOnlyDictionary<Guid, Guid>> ReadParentsAsync(
        BdthequeDbContext context, IReadOnlyCollection<Model.ChangeLogEntry> entries, CancellationToken cancellationToken)
    {
        var editions = IdsOf(entries, DomainEnums.RecordType.Edition);
        var intents = IdsOf(entries, DomainEnums.RecordType.PurchaseIntent);
        var collections = IdsOf(entries, DomainEnums.RecordType.PublisherCollection);
        var parents = new List<KeyValuePair<Guid, Guid>>();
        if (editions.Count > 0)
        {
            parents.AddRange(await context.Editions.AsNoTracking()
                .Where(e => editions.Contains(e.Id))
                .Select(e => new KeyValuePair<Guid, Guid>(e.Id, e.AlbumId))
                .ToListAsync(cancellationToken));
        }

        if (intents.Count > 0)
        {
            parents.AddRange(await context.PurchaseIntents.AsNoTracking()
                .Where(i => intents.Contains(i.Id))
                .Select(i => new KeyValuePair<Guid, Guid>(i.Id, i.AlbumId))
                .ToListAsync(cancellationToken));
        }

        if (collections.Count > 0)
        {
            parents.AddRange(await context.PublisherCollections.AsNoTracking()
                .Where(c => collections.Contains(c.Id))
                .Select(c => new KeyValuePair<Guid, Guid>(c.Id, c.PublisherId))
                .ToListAsync(cancellationToken));
        }

        // Identifiers are unique across every table (Guid v7), so one dictionary holds the three kinds.
        return new Dictionary<Guid, Guid>(parents);
    }

    private static List<Guid> IdsOf(IEnumerable<Model.ChangeLogEntry> entries, DomainEnums.RecordType type) =>
        entries.Where(e => e.RecordType == type).Select(e => e.RecordId).Distinct().ToList();

    private static Contract.ChangeLogEntryItem ToItem(
        Model.ChangeLogEntry entry, IReadOnlyDictionary<Guid, Guid> parents, bool recordExists) =>
        new(
            entry.Id,
            entry.CreatedAt,
            EnumMapping.Map<ContractEnums.RecordType>(entry.RecordType)!.Value,
            entry.RecordId,
            parents.TryGetValue(entry.RecordId, out var parent) ? parent : null,
            recordExists,
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
