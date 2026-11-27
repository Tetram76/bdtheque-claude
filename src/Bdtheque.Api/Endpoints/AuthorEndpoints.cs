using System.Data;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Errors;
using Bdtheque.Api.Merge;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ContractEnums = Bdtheque.Contracts.Enums;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the authors (<c>/admin/authors</c>).</summary>
internal static class AuthorEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: an author is refused deletion while credited on an
    // album or a series. An author credited with several roles on one record blocks it once: the
    // impact counts records, not contributions.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Reference, EntityKind.Album,
            (context, id) => context.Contributions
                .Where(c => c.AuthorId == id && c.AlbumId != null)
                .Select(c => c.AlbumId!.Value)),
        new(LinkNature.Reference, EntityKind.Series,
            (context, id) => context.Contributions
                .Where(c => c.AuthorId == id && c.SeriesId != null)
                .Select(c => c.SeriesId!.Value)),
    ];

    public static void MapAuthors(this RouteGroupBuilder admin)
    {
        var authors = admin.MapGroup("/authors");
        authors.MapGet("/", ListForAdministrationAsync);
        authors.MapGet("/{id:guid}", GetAsync);
        authors.MapPost("/", CreateAsync);
        authors.MapPut("/{id:guid}", UpdateAsync);
        authors.MapDeletion<Author>(DeletionLinks);
        authors.MapGet("/identity", GetIdentity);
        authors.MapGet("/merge", GetMergeAsync);
        authors.MapPost("/merge", MergeAsync);
    }

    // The series and albums an author is credited on (visuel/fusion/fusion-de-deux-fiches.md).
    private static readonly (EntityKind Kind, Func<BdthequeDbContext, Guid, IQueryable<Guid>> Records)[] MergeLinks =
    [
        (EntityKind.Series, (context, id) => context.Contributions.Where(c => c.AuthorId == id && c.SeriesId != null).Select(c => c.SeriesId!.Value)),
        (EntityKind.Album, (context, id) => context.Contributions.Where(c => c.AuthorId == id && c.AlbumId != null).Select(c => c.AlbumId!.Value)),
    ];

    /// <summary>
    /// The identity the domain derives from names (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes): the merge of
    /// two authors presents it for the names chosen, without writing anything.
    /// </summary>
    private static AuthorIdentity GetIdentity(string? lastName = null, string? firstName = null, string? pseudonym = null)
    {
        var author = new Author(lastName, firstName, pseudonym);
        return new AuthorIdentity(author.DisplayName, author.SortKey, author.NavigationEntry);
    }

    /// <summary>Two authors to merge, and what they are credited on (fonctionnel/regles-metier/fusion-des-fiches.md).</summary>
    private static async Task<AuthorMergePreview> GetMergeAsync(Guid first, Guid second, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot: the counts are those of the two authors read.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        return new AuthorMergePreview(
            await MergeRecordAsync(context, first, cancellationToken),
            await MergeRecordAsync(context, second, cancellationToken),
            await RecordMerge.CountLinksAsync(context, first, second, MergeLinks, cancellationToken));
    }

    private static async Task<AuthorMergeRecord> MergeRecordAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken) =>
        await context.Authors.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AuthorMergeRecord(
                a.Id, a.LastName, a.FirstName, a.Pseudonym, a.Nationality, a.Biography, a.ModifiedAt,
                EF.Property<uint>(a, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Author), id);

    /// <summary>
    /// Merges two authors: the author that remains keeps each data chosen, and is credited on the series and albums of both, in
    /// the same roles, a role both held being credited once.
    /// </summary>
    private static Task<MergeResult> MergeAsync(AuthorMergeRequest request, BdthequeDbContext context, CancellationToken cancellationToken) =>
        RecordMerge.MergeAsync<Author>(
            context, request.First, request.Second,
            RecordMerge.KeepsFirst([request.LastName, request.FirstName, request.Pseudonym, request.Nationality, request.Biography]),
            pair =>
            {
                // Read from both before the kept author changes.
                var (lastName, firstName, pseudonym) =
                    (pair.Of(request.LastName).LastName, pair.Of(request.FirstName).FirstName, pair.Of(request.Pseudonym).Pseudonym);
                var (nationality, biography) = (pair.Of(request.Nationality).Nationality, pair.Of(request.Biography).Biography);
                pair.Kept.UpdateIdentity(lastName, firstName, pseudonym);
                pair.Kept.UpdateNationality(nationality);
                pair.Kept.UpdateBiography(biography);
            },
            async pair =>
            {
                // The contributions removed as duplicates, which a saved search may designate (contributions are searched).
                var removed = new List<(Guid Removed, Guid Kept)>();
                foreach (var series in await RecordMerge.LoadLinkedForWriteAsync<Series>(
                             context,
                             context.Contributions.Where(c => c.AuthorId == pair.Merged.Id && c.SeriesId != null).Select(c => c.SeriesId!.Value),
                             cancellationToken))
                {
                    await context.Entry(series).Collection(s => s.TemplateContributions).LoadAsync(cancellationToken);
                    removed.AddRange(series.TransferAuthor(pair.Merged, pair.Kept));
                }

                foreach (var album in await RecordMerge.LoadLinkedForWriteAsync<Album>(
                             context,
                             context.Contributions.Where(c => c.AuthorId == pair.Merged.Id && c.AlbumId != null).Select(c => c.AlbumId!.Value),
                             cancellationToken))
                    removed.AddRange(album.TransferAuthor(pair.Merged, pair.Kept));

                return [.. removed.Select(r => new MergeReplacement(EntityKind.Contribution, r.Removed, r.Kept))];
            },
            cancellationToken);

    public static void MapAuthorCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/authors", ListAsync);
        catalog.MapGet("/authors/{id:guid}", GetDetailAsync);
    }

    /// <summary>
    /// An author with the whole bibliography (fonctionnel/structure-de-l-application.md): a single list
    /// reconciling the series credited on — on the series itself, or on one of its albums — with the
    /// albums of each, and the albums without series, each with the roles held.
    /// </summary>
    /// <remarks>
    /// The series and the albums without series come together, by sort key, as typed entries: this is the
    /// order of the first level of the bibliography, where the albums without series stand alongside the
    /// series (fonctionnel/structure-de-l-application.md). Bounded, the bibliography is whole.
    /// </remarks>
    private static async Task<AuthorDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var author = await context.Authors.AsNoTracking()
                         .Where(a => a.Id == id)
                         .Select(a => new { a.LastName, a.FirstName, a.Pseudonym, a.Biography, a.Nationality, a.CreatedAt, a.ModifiedAt })
                         .SingleOrDefaultAsync(cancellationToken)
                     ?? throw new EntityNotFoundException(typeof(Author), id);

        // A single snapshot for the reads below, which are assembled into one another: an entry is
        // never without its series or album, whatever is written meanwhile.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var credited = context.Albums.AsNoTracking().Where(a => a.Contributions.Any(c => c.AuthorId == id));
        var creditedSeries = context.Series.AsNoTracking()
            .Where(s => s.TemplateContributions.Any(c => c.AuthorId == id) || credited.Any(a => a.SeriesId == s.Id));

        var bibliography = await CatalogWorks.ReadBibliographyAsync(
            context, creditedSeries, credited,
            CatalogExpressions.Expand((Series s) => new CatalogWorks.BibliographySeries(
                new SeriesListItem(s.Id, s.Title), s.TemplateContributions.Any(c => c.AuthorId == id), s.TemplateContributions.RolesOf(id))),
            CatalogExpressions.Expand((Album a) => new AlbumBibliographyItem(
                a.ToCard(), a.Contributions.RolesOf(id), a.FirstPublicationYear, a.FirstPublicationMonth)),
            cancellationToken);

        return new AuthorDetail(
            id, author.LastName, author.FirstName, author.Pseudonym, author.Biography, author.Nationality, bibliography, author.CreatedAt,
            author.ModifiedAt);
    }

    /// <summary>
    /// The list of the authors of the consultation, that of the administration without its actions: its search and navigation,
    /// in the same order, and the possession, its own filter, by which it limits itself to the collection
    /// (fonctionnel/regles-metier/perimetre-de-la-consultation.md). The choice of an author by their name reads it too.
    /// </summary>
    private static async Task<Page<AuthorCatalogListItem>> ListAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null,
        AlbumCollectionState? state = null, int page = 1, int pageSize = Paging.DefaultSize)
    {
        var rows = await RowsAsync(context, q, entry, state, page, pageSize, cancellationToken);
        return new Page<AuthorCatalogListItem>(
            [.. rows.Items.Select(r => new AuthorCatalogListItem(
                r.Id, r.LastName, r.FirstName, r.Pseudonym, r.Nationality, r.CreditedAlbumCount, r.CreditedAlbumsInCollectionCount, r.Roles))],
            rows.Number, rows.Size, rows.TotalCount);
    }

    /// <summary>
    /// The list of the authors of the administration (visuel/listes/liste-par-liste.md): the search and navigation of the list
    /// of the consultation, in the same order. Each row carries the identity and the nationality of the author, the albums it
    /// is credited on, the roles it holds on albums and series, its modification and its version, which its deletion sends
    /// back.
    /// </summary>
    /// <param name="resembling">An author whose merge proposes the authors that resemble them (<see cref="MergeSuggestions"/>):
    /// the list is then restricted to them, the closest first.</param>
    private static async Task<Page<AuthorAdminListItem>> ListForAdministrationAsync(
        BdthequeDbContext context, CancellationToken cancellationToken, string? q = null, string? entry = null, int page = 1,
        int pageSize = Paging.DefaultSize, Guid? resembling = null)
    {
        var ranked = resembling is { } subject ? await ResemblingAsync(context, subject, pageSize, cancellationToken) : null;
        var rows = await RowsAsync(context, q, entry, state: null, page, pageSize, cancellationToken, ranked);
        return new Page<AuthorAdminListItem>(
            MergeSuggestions.InRankOrder(rows.Items.Select(r => new AuthorAdminListItem(
                r.Id, r.LastName, r.FirstName, r.Pseudonym, r.Nationality, r.CreditedAlbumCount, r.Roles, r.ModifiedAt, r.Version)), ranked, a => a.Id),
            rows.Number, rows.Size, rows.TotalCount);
    }

    // Each identifier of an author, against each of the other's: the pseudonym of one may be the name of the other; a name
    // alone within the first and last name is one more, as « Gotlib » within « Marcel Gotlib » (fonctionnel/regles-metier/fusion-des-fiches.md).
    private static async Task<IReadOnlyList<Guid>> ResemblingAsync(
        BdthequeDbContext context, Guid subject, int count, CancellationToken cancellationToken)
    {
        var authors = await Search(context, text: null, entry: null, state: null)
            .Select(a => new { a.Id, a.LastName, a.FirstName, a.Pseudonym })
            .ToListAsync(cancellationToken);
        return MergeSuggestions.Rank(authors, subject, a => a.Id, (a, b) => Resemblance.Compare(
            [a.Pseudonym, a.LastName, $"{a.FirstName} {a.LastName}", $"{a.LastName} {a.FirstName}"],
            [b.Pseudonym, b.LastName, $"{b.FirstName} {b.LastName}", $"{b.LastName} {b.FirstName}"], inclusion: true), count);
    }

    // A page of the authors as both lists read them, each list keeping what it presents.
    private static async Task<Page<AuthorRow>> RowsAsync(
        BdthequeDbContext context, string? text, string? entry, AlbumCollectionState? state, int page, int pageSize,
        CancellationToken cancellationToken, IReadOnlyList<Guid>? only = null)
    {
        var rows = await Search(context, text, entry, state, only).ToPageAsync(page, pageSize, CatalogExpressions.Expand((Author a) => new
        {
            a.Id,
            a.LastName,
            a.FirstName,
            a.Pseudonym,
            a.Nationality,
            // An album credited with several roles of the author counts once.
            CreditedAlbumCount = context.Albums.Count(album => album.Contributions.Any(c => c.AuthorId == a.Id)),
            CreditedAlbumsInCollectionCount = context.Albums.Count(album =>
                album.Contributions.Any(c => c.AuthorId == a.Id) && album.Editions.Any(e => e.IsOwned())),
            Roles = context.Contributions.Where(c => c.AuthorId == a.Id).Select(c => c.Role).Distinct().OrderBy(r => r).ToList(),
            a.ModifiedAt,
            Version = EF.Property<uint>(a, BdthequeDbContext.VersionProperty),
        }), cancellationToken);

        return new Page<AuthorRow>(
            [.. rows.Items.Select(r => new AuthorRow(
                r.Id, r.LastName, r.FirstName, r.Pseudonym, r.Nationality, r.CreditedAlbumCount, r.CreditedAlbumsInCollectionCount,
                [.. r.Roles.Select(role => EnumMapping.Map<ContractEnums.ContributionRole>(role)!.Value)], r.ModifiedAt, r.Version))],
            rows.Number, rows.Size, rows.TotalCount);
    }

    private sealed record AuthorRow(
        Guid Id, string? LastName, string? FirstName, string? Pseudonym, string? Nationality, int CreditedAlbumCount,
        int CreditedAlbumsInCollectionCount, IReadOnlyList<ContractEnums.ContributionRole> Roles, DateTimeOffset ModifiedAt, uint Version);

    /// <summary>
    /// The authors found by any part of their name: last name, first name, pseudonym, or full name, as
    /// displayed ("First Last") or as sorted ("Last First",
    /// fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes), under the entry of the navigation, credited on at
    /// least one album in the possession chosen (fonctionnel/regles-metier/perimetre-de-la-consultation.md), by sort key.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An entry that is not one of the navigation: a technical error.</exception>
    private static IOrderedQueryable<Author> Search(
        BdthequeDbContext context, string? text, string? entry, AlbumCollectionState? state, IReadOnlyList<Guid>? only = null)
    {
        CatalogFilters.EnsureNavigationEntry(entry);
        var authors = context.Authors.AsNoTracking()
            .WhereContains(text, a => a.LastName, a => a.FirstName, a => a.Pseudonym, a => a.FirstName + " " + a.LastName, a => a.SortKey);
        if (only is not null)
            authors = authors.Where(a => only.Contains(a.Id));
        if (entry is not null)
            authors = authors.Where(a => a.NavigationEntry == entry);
        if (state is { } wanted)
            authors = authors.Where(CatalogExpressions.Expand((Author a) =>
                context.Albums.Any(album => album.Contributions.Any(c => c.AuthorId == a.Id) && album.CollectionState() == wanted)));
        return authors.OrderByEntry(a => a.NavigationEntry).ThenBy(a => a.SortKey).ThenBy(a => a.Id);
    }

    private static async Task<AuthorForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken) =>
        await context.Authors
            .Where(a => a.Id == id)
            .Select(a => new AuthorForm(
                a.Id, a.LastName, a.FirstName, a.Pseudonym, a.Biography, a.Nationality, a.CreatedAt, a.ModifiedAt,
                EF.Property<uint>(a, BdthequeDbContext.VersionProperty)))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Author), id);

    private static async Task<Created<AuthorForm>> CreateAsync(
        CreateAuthorRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        var author = fields.Create(
            "", () => new Author(request.LastName, request.FirstName, request.Pseudonym), EnteredOtherwise(request.Pseudonym));
        author.UpdateBiography(request.Biography);
        author.UpdateNationality(request.Nationality);
        context.Authors.Add(author);
        await context.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/authors/{author.Id}", ToForm(context, author));
    }

    private static async Task<AuthorForm> UpdateAsync(
        Guid id, UpdateAuthorRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var author = await context.LoadAggregateForWriteAsync<Author>(id, request.Version, cancellationToken: cancellationToken);
        fields.Place(author, renamed: EnteredOtherwise(request.Pseudonym));
        author.UpdateIdentity(request.LastName, request.FirstName, request.Pseudonym);
        author.UpdateBiography(request.Biography);
        author.UpdateNationality(request.Nationality);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, author);
    }

    // The display name, and the sort key drawn from it, come from the pseudonym if any, otherwise from the names
    // (fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes): a display name already taken, or too long, is
    // corrected there.
    internal static Dictionary<string, string> EnteredOtherwise(string? pseudonym)
    {
        var field = string.IsNullOrWhiteSpace(pseudonym) ? "/lastName" : "/pseudonym";
        return new() { [nameof(Author.DisplayName)] = field, [nameof(Author.SortKey)] = field };
    }

    private static AuthorForm ToForm(BdthequeDbContext context, Author author) =>
        new(author.Id, author.LastName, author.FirstName, author.Pseudonym, author.Biography, author.Nationality, author.CreatedAt,
            author.ModifiedAt, context.VersionOf(author));
}
