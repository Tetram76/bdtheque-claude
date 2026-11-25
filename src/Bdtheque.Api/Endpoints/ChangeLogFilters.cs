using Bdtheque.Api.Search;
using Bdtheque.Contracts.Search;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;
using Model = Bdtheque.Domain.Entities;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search and the filters of the whole change log (visuel/journal/journal-des-modifications.md), bound as a whole from the
/// query of its request (<c>[AsParameters]</c>), as the filters of the lists: the search is its parameter <c>q</c>, each other
/// criterion that of its name. They combine.
/// </summary>
/// <param name="Text">The search on the label of the records.</param>
/// <param name="RecordType">The type of the records.</param>
/// <param name="Operation">The operation.</param>
/// <param name="From">The first day of the period, included, in the time zone of the browser.</param>
/// <param name="To">The last day of the period, included.</param>
/// <param name="Record">A single record, whose history the change log then is.</param>
/// <param name="TimeZone">
/// The time zone of the browser, in which the days of the period are counted, as an advanced search compares a date and time
/// by its day; universal time without one.
/// </param>
internal sealed record ChangeLogFilters(
    [FromQuery(Name = "q")] string? Text,
    ContractEnums.RecordType? RecordType,
    ContractEnums.ChangeLogOperation? Operation,
    DateOnly? From,
    DateOnly? To,
    Guid? Record,
    [FromQuery(Name = AdvancedSearchQuery.TimeZoneParameter)] string? TimeZone)
{
    // The separators an ISBN is searched without, as one string for the SQL function translate (IsbnSearch).
    private static readonly string IsbnSeparators = string.Concat(IsbnSearch.Separators);

    // Each text of the label, decoded (->>), with whether it is an ISBN: a constant, every value searched being a parameter
    // ({0} the pattern of the text, {1} that of an ISBN, {2} the separators of an ISBN), any other brace being read as one.
    private const string SearchSql =
        $$"""
        SELECT * FROM "ChangeLogEntries" AS entry
        WHERE EXISTS (
            SELECT 1
            FROM jsonb_path_query(entry."Label", 'strict $.** ? (@.type() == "object")') AS label(part)
            CROSS JOIN LATERAL (
                SELECT label.part ->> key, key = 'isbn' FROM jsonb_each(label.part)
                WHERE key <> 'kind' AND jsonb_typeof(value) = 'string'
                UNION ALL
                SELECT concat_ws(' ', label.part ->> 'firstName', label.part ->> 'lastName'), false
                WHERE label.part ->> 'kind' = 'author'
                UNION ALL
                SELECT concat_ws(' ', label.part ->> 'lastName', label.part ->> 'firstName'), false
                WHERE label.part ->> 'kind' = 'author'
            ) AS searched(text, isbn)
            WHERE searched.text COLLATE "{{BdthequeDbContext.CaseAndAccentInsensitiveFrenchCollation}}" LIKE {0} ESCAPE '\'
                OR (searched.isbn AND translate(searched.text, {2}, '') LIKE {1} ESCAPE '\'))
        """;

    /// <summary>The entries of the change log that the search and the filters keep.</summary>
    public IQueryable<Model.ChangeLogEntry> Apply(BdthequeDbContext context)
    {
        var zone = SearchTimeZones.Resolve(TimeZone);
        var entries = Searched(context).AsNoTracking();
        if (RecordType is { } type)
        {
            var domainType = EnumMapping.Map<DomainEnums.RecordType>(type)!.Value;
            entries = entries.Where(e => e.RecordType == domainType);
        }

        if (Operation is { } kind)
        {
            var domainOperation = EnumMapping.Map<DomainEnums.ChangeLogOperation>(kind)!.Value;
            entries = entries.Where(e => e.Operation == domainOperation);
        }

        if (From is { } first)
        {
            var start = InstantField.StartOf(first, zone);
            entries = entries.Where(e => e.CreatedAt >= start);
        }

        if (To is { } last)
        {
            var end = InstantField.StartOf(last.AddDays(1), zone);
            entries = entries.Where(e => e.CreatedAt < end);
        }

        if (Record is { } id)
            entries = entries.Where(e => e.RecordId == id);
        return entries;
    }

    /// <summary>
    /// The entries whose label contains the text searched, ignoring case and accents, by what identifies each record in the
    /// lists (choix-implementation/api/recherche.md); all of them if it is blank. The label is stored in <c>jsonb</c> by its
    /// components, each text compared on its own, decoded: every text of it, those of the labels it holds too (the album of an
    /// edition or of a purchase intent), never the kinds that tell them apart, nor its numbers and flags; the full name of an
    /// author in its two orders; an ISBN without its separators, on both sides.
    /// </summary>
    private IQueryable<Model.ChangeLogEntry> Searched(BdthequeDbContext context)
    {
        if (string.IsNullOrWhiteSpace(Text))
            return context.ChangeLog;

        var text = Text.Trim();
        var pattern = $"%{CatalogFilters.EscapeLike(text)}%";
        // Without a character of its own, the ISBN searched is the text itself: a separator alone finds no ISBN by its absence.
        var isbn = IsbnSearch.Normalize(text);
        var isbnPattern = isbn.Length == 0 ? pattern : $"%{CatalogFilters.EscapeLike(isbn)}%";
        // EF Core translates no JSON path over a converted column: the condition is written in SQL, and the rest of the query
        // composed over it.
        return context.ChangeLog.FromSqlRaw(SearchSql, pattern, isbnPattern, IsbnSeparators);
    }
}
