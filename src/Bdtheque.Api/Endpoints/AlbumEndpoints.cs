using System.Data;
using System.Linq.Expressions;
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
using DomainEnums = Bdtheque.Domain.Enums;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Endpoints;

/// <summary>Administration of the albums (<c>/admin/albums</c>).</summary>
internal static class AlbumEndpoints
{
    // fonctionnel/regles-metier/suppression-des-entites.md: an album takes along its editions (with their
    // visuals and purchase intents), its contributions and its own purchase intent. Its genres and
    // universes are associations that merely disappear, without any record being concerned.
    private static readonly DeletionLink[] DeletionLinks =
    [
        new(LinkNature.Composition, EntityKind.Edition,
            (context, id) => context.Editions.Where(e => e.AlbumId == id).Select(e => e.Id)),
        new(LinkNature.Composition, EntityKind.EditionVisual,
            (context, id) => context.EditionVisuals.Where(v => v.Edition.AlbumId == id).Select(v => v.Id)),
        new(LinkNature.Composition, EntityKind.Contribution,
            (context, id) => context.Contributions.Where(c => c.AlbumId == id).Select(c => c.Id)),
        // Every intent carries the identifier of its album, that of an intent on an edition included.
        new(LinkNature.Composition, EntityKind.PurchaseIntent,
            (context, id) => context.PurchaseIntents.Where(p => p.AlbumId == id).Select(p => p.Id)),
    ];

    public static void MapAlbums(this RouteGroupBuilder admin)
    {
        var albums = admin.MapGroup("/albums");
        albums.MapGet("/", ListForAdministrationAsync);
        albums.MapGet("/{id:guid}", GetAsync);
        albums.MapGet("/{id:guid}/editions-and-purchase-intents", GetEditionsPageAsync);
        albums.MapPost("/", CreateAsync);
        albums.MapPut("/{id:guid}", UpdateAsync);
        albums.MapDeletion<Album>(DeletionLinks);
        albums.MapGet("/merge", GetMergeAsync);
        albums.MapPost("/merge", MergeAsync);
    }

    /// <summary>
    /// Two albums to merge (fonctionnel/regles-metier/fusion-des-fiches.md), their editions and purchase intents, which pass to
    /// the album that remains, and the intents on the whole album the merge withdraws.
    /// </summary>
    private static async Task<AlbumMergePreview> GetMergeAsync(Guid first, Guid second, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot: the counts are those of the two albums read.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives for this request only.
        var albums = new List<Album>(2);
        foreach (var id in new[] { first, second })
        {
            albums.Add(await WithAssociations(context.Albums).SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(typeof(Album), id));
        }

        var (firstAlbum, secondAlbum) = (albums[0], albums[1]);
        var withdrawn = Album.PurchaseIntentsWithdrawnByMerge(firstAlbum, secondAlbum);
        return new AlbumMergePreview(
            await MergeRecordAsync(context, firstAlbum, cancellationToken),
            await MergeRecordAsync(context, secondAlbum, cancellationToken),
            MergeLinks(firstAlbum, secondAlbum, withdrawn),
            [
                .. firstAlbum.PurchaseIntents.Any(withdrawn.Contains) ? [MergeSide.First] : Array.Empty<MergeSide>(),
                .. secondAlbum.PurchaseIntents.Any(withdrawn.Contains) ? [MergeSide.Second] : Array.Empty<MergeSide>(),
            ]);
    }

    // What passes to the album that remains, in the order of the entity types: its owned editions, and its purchase intents, an
    // edition targeted counted as its intent (fonctionnel/regles-metier/suppression-des-entites.md); an intent on the whole album
    // both albums hold is the same wish, common to both, and one the merge withdraws does not pass (visuel/fusion/fusion-de-deux-fiches.md).
    private static List<MergeLinkCount> MergeLinks(Album first, Album second, IReadOnlyList<PurchaseIntent> withdrawn)
    {
        static int Owned(Album album) => album.Editions.Count(e => e.AcquisitionMode is not null);
        int Intents(Album album) => album.PurchaseIntents.Count(p => !withdrawn.Contains(p));
        var commonIntents = first.PurchaseIntents.Concat(second.PurchaseIntents).Count(p => p.EditionId is null && !withdrawn.Contains(p)) == 2 ? 1 : 0;
        return
        [
            new(EntityKind.Edition, Owned(first) + Owned(second), Owned(first), Owned(second), 0),
            new(EntityKind.PurchaseIntent, Intents(first) + Intents(second) - commonIntents, Intents(first), Intents(second), commonIntents),
        ];
    }

    private static async Task<AlbumMergeRecord> MergeRecordAsync(BdthequeDbContext context, Album album, CancellationToken cancellationToken) =>
        new(
            album.Id, album.Title, album.IsManualSortKey ? album.SortKey : null,
            album.Series is { } series ? new SeriesListItem(series.Id, series.Title) : null, album.Series?.SortKey,
            EnumMapping.Map<ContractEnums.AlbumType>(album.Type)!.Value, album.IsSpecialIssue, album.VolumeNumber, album.StartVolumeNumber,
            album.EndVolumeNumber, album.FirstPublicationYear, album.FirstPublicationMonth, album.Summary, album.PersonalNotes,
            EnumMapping.Map<ContractEnums.AlbumRating>(album.Rating), FormReferences.Genres(album.Genres),
            await FormReferences.UniversesInHierarchyAsync(context, album.Universes, cancellationToken),
            [
                .. album.Contributions
                    .OrderBy(c => c.Role).ThenBy(c => c.Author.SortKey).ThenBy(c => c.AuthorId)
                    .Select(c => new MergeContribution(
                        c.Id, new AuthorListItem(c.AuthorId, c.Author.LastName, c.Author.FirstName, c.Author.Pseudonym),
                        EnumMapping.Map<ContractEnums.ContributionRole>(c.Role)!.Value)),
            ],
            album.ModifiedAt, context.VersionOf(album));

    /// <summary>
    /// Merges two albums: the album that remains keeps each data chosen, the type and the volumes an omnibus covers together, the
    /// genres, universes and contributions retained of both, a credit both held being kept once; the editions of both are its
    /// editions, with their intents, and an intent on the whole album is withdrawn once it has one.
    /// </summary>
    private static Task<MergeResult> MergeAsync(AlbumMergeRequest request, BdthequeDbContext context, CancellationToken cancellationToken) =>
        RecordMerge.MergeAsync<Album>(
            context, request.First, request.Second,
            RecordMerge.KeepsFirst(
            [
                request.Series, request.Title, request.SortKey, request.VolumeNumber, request.Type, request.IsSpecialIssue,
                request.FirstPublicationDate, request.Summary, request.PersonalNotes, request.Rating,
                .. request.Genres.Select(g => g.Side), .. request.Universes.Select(u => u.Side), .. request.Contributions.Select(c => c.Side),
            ]),
            pair => AssignMerged(pair, request),
            async pair =>
            {
                // What the album that remains takes of either album, read with them before anything changes.
                foreach (var album in new[] { pair.First, pair.Second })
                    await LoadChildrenAsync(context, album, cancellationToken);

                // A credit both albums hold is kept once, by the contribution of the album that remains, which a saved search
                // designating the other one designates instead (contributions are searched).
                var replaced = pair.Kept.CreditsHeldOf(pair.Merged).Select(c => new MergeReplacement(EntityKind.Contribution, c.Removed, c.Kept)).ToList();
                pair.Kept.TakeEditionsOf(pair.Merged);
                return replaced;
            },
            cancellationToken);

    // The children of an album locked for its merge, which WithAssociations reads with an album otherwise; its editions, intents
    // and contributions are always loaded with it (AlbumConfiguration).
    private static async Task LoadChildrenAsync(BdthequeDbContext context, Album album, CancellationToken cancellationToken)
    {
        var entry = context.Entry(album);
        await entry.Collection(a => a.Genres).LoadAsync(cancellationToken);
        await entry.Collection(a => a.Universes).LoadAsync(cancellationToken);
        await entry.Reference(a => a.Series).LoadAsync(cancellationToken);
        await entry.Collection(a => a.Contributions).Query().Include(c => c.Author).LoadAsync(cancellationToken);
    }

    // The data of the album that remains, each of the album chosen, read from both before it changes.
    private static void AssignMerged(MergePair<Album> pair, AlbumMergeRequest request)
    {
        var kept = pair.Kept;
        var (title, series) = (pair.Of(request.Title).Title, pair.Of(request.Series).Series);
        // Without a title of its own, the album is filed under the sort key of its series, which no sort key entered by hand
        // replaces (visuel/saisie-album/contenu.md): the one chosen then has nothing to apply to.
        var sortKey = title is not null && pair.Of(request.SortKey) is { IsManualSortKey: true } manual ? manual.SortKey : null;
        var typed = pair.Of(request.Type);
        var (type, start, end) = (typed.Type, typed.StartVolumeNumber, typed.EndVolumeNumber);
        var (specialIssue, volume) = (pair.Of(request.IsSpecialIssue).IsSpecialIssue, pair.Of(request.VolumeNumber).VolumeNumber);
        var published = pair.Of(request.FirstPublicationDate);
        var (year, month) = (published.FirstPublicationYear, published.FirstPublicationMonth);
        var (summary, notes, rating) = (pair.Of(request.Summary).Summary, pair.Of(request.PersonalNotes).PersonalNotes, pair.Of(request.Rating).Rating);
        var genres = RecordMerge.Retained(pair, request.Genres, a => a.Genres, g => g.Id);
        var universes = RecordMerge.Retained(pair, request.Universes, a => a.Universes, u => u.Id);
        var contributions = RecordMerge.Retained(pair, request.Contributions, a => a.Contributions, c => (c.AuthorId, c.Role));

        kept.MergeContributions(pair.Merged, [.. contributions.Select(c => c.Id)]);
        kept.SetTitleSeriesAndContributions(title, series, [.. kept.Contributions.Select(c => (c.Author, c.Role))]);
        if (sortKey is not null)
            kept.SetSortKey(sortKey);
        else
            kept.ResetSortKey();
        kept.SetTypeAndVolumeRange(type, start, end);
        kept.SetSpecialIssue(specialIssue);
        kept.SetVolumeNumber(volume);
        kept.SetFirstPublicationDate(year, month);
        kept.SetSummary(summary);
        kept.SetPersonalNotes(notes);
        kept.SetRating(rating);
        FormReferences.ReplaceAssociations(kept.Genres, genres, kept.AddGenre, kept.RemoveGenre);
        FormReferences.ReplaceAssociations(kept.Universes, universes, kept.AddUniverse, kept.RemoveUniverse);
    }

    public static void MapAlbumCatalog(this RouteGroupBuilder catalog)
    {
        catalog.MapGet("/albums", ListAsync);
        catalog.MapGet("/albums/{id:guid}", GetDetailAsync);
    }

    internal static readonly Expression<Func<Album, AlbumSummary>> ListItem = CatalogExpressions.Expand((Album a) => a.ToSummary());

    /// <summary>
    /// An album with what it is linked with: the genres and universes displayed — those of the album and
    /// of its series, without duplicates (fonctionnel/regles-metier/genres-et-univers-d-un-album.md) —, each universe
    /// with its ancestors, its contributions, every edition, whole, which has no record of its own
    /// (fonctionnel/structure-de-l-application.md), and every album of its series, in a number bounded by the series.
    /// </summary>
    private static async Task<AlbumDetail> GetDetailAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot for the reads below, which are assembled into one another.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var album = await context.Albums.AsNoTracking()
                        .Where(a => a.Id == id)
                        .Select(CatalogExpressions.Expand((Album a) => new
                        {
                            Album = a.ToSummary(),
                            State = a.CollectionState(),
                            a.FirstPublicationYear,
                            a.FirstPublicationMonth,
                            a.Summary,
                            a.PersonalNotes,
                            a.Rating,
                            // That of its cards, the record being of the documentary base (fonctionnel/regles-metier/couverture-d-un-album.md).
                            CoverVisual = a.CoverVisualOf(),
                            Genres = context.Genres
                                .Where(g => a.Genres.Any(o => o.Id == g.Id) || a.Series!.Genres.Any(o => o.Id == g.Id))
                                .OrderBy(g => g.Label).ThenBy(g => g.Id)
                                .Select(g => new GenreListItem(g.Id, g.Label)).ToList(),
                            Universes = context.Universes
                                .Where(u => a.Universes.Any(o => o.Id == u.Id) || a.Series!.Universes.Any(o => o.Id == u.Id))
                                .OrderBy(u => u.Name).ThenBy(u => u.Id)
                                .Select(u => u.ToListItem()).ToList(),
                            Contributions = a.Contributions.ToItems().ToList(),
                            PurchaseIntent = a.PurchaseIntents.Where(p => p.EditionId == null).Select(p => p.ToItem()).FirstOrDefault(),
                            a.CreatedAt,
                            a.ModifiedAt,
                        }))
                        .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), id);

        var editions = await EditionEndpoints.ReadDetailsAsync(id, context, cancellationToken);
        var universes = await UniverseTree.InHierarchyAsync(context, album.Universes, cancellationToken);
        var seriesAlbums = album.Album.SeriesId is { } seriesId
            ? await SeriesEndpoints.ReadAlbumsAsync(seriesId, context, cancellationToken)
            : [];

        return new AlbumDetail(
            album.Album, album.State, album.FirstPublicationYear, album.FirstPublicationMonth, album.Summary, album.PersonalNotes,
            EnumMapping.Map<ContractEnums.AlbumRating>(album.Rating), CatalogExpressions.ToCover(album.CoverVisual), album.Genres, universes, album.Contributions, editions,
            album.PurchaseIntent, seriesAlbums, album.CreatedAt, album.ModifiedAt);
    }

    /// <summary>
    /// The list of the albums of the consultation: the albums found by their title or that of their series, part of their label
    /// (fonctionnel/presentation/libelle-d-un-album.md), narrowed by the cross filters (choix-implementation/api/recherche.md)
    /// and the possession. An album without a title of its own is filed under the current sort key and entry of its series.
    /// </summary>
    private static async Task<AlbumCatalogList> ListAsync(
        BdthequeDbContext context, [AsParameters] AlbumFilters filters, CancellationToken cancellationToken, int page = 1,
        int pageSize = Paging.DefaultSize)
    {
        var albums = await filters.ApplyAsync(context.Albums.AsNoTracking(), context, cancellationToken);
        var row = filters.PresentsOwnedEditionsOnly ? CatalogListItemOfOwnedEditions : CatalogListItem;
        var rows = await albums.OrderByAlbum(a => a).ToPageAsync(page, pageSize, row, cancellationToken);
        return new AlbumCatalogList(
            new Page<AlbumCatalogListItem>([.. rows.Items.Select(r => r with { Authors = EachOnce(r.Authors) })], rows.Number, rows.Size, rows.TotalCount),
            await filters.NameAsync(context, cancellationToken));
    }

    // An album as a row of the list of the consultation, with its cover among all its editions, or among its owned ones only.
    private static readonly Expression<Func<Album, AlbumCatalogListItem>> CatalogListItem =
        CatalogExpressions.Expand((Album a) => new AlbumCatalogListItem(
            a.ToSummary(), CatalogExpressions.ToCover(a.CoverVisualOf()), a.CreditedAuthors(), a.FirstPublicationYear, a.FirstPublicationMonth,
            EnumMapping.Map<ContractEnums.AlbumRating>(a.Rating), a.CollectionState()));

    private static readonly Expression<Func<Album, AlbumCatalogListItem>> CatalogListItemOfOwnedEditions =
        CatalogExpressions.Expand((Album a) => new AlbumCatalogListItem(
            a.ToSummary(), CatalogExpressions.ToCover(a.Editions.Where(e => e.IsOwned()).CoverVisualAmong()), a.CreditedAuthors(),
            a.FirstPublicationYear, a.FirstPublicationMonth, EnumMapping.Map<ContractEnums.AlbumRating>(a.Rating), a.CollectionState()));

    /// <summary>
    /// The list of the albums of the administration (visuel/liste-albums/contenu.md): the search, navigation and filters of
    /// the consultation, and where each album stands with regard to the collection, in the same order.
    /// </summary>
    /// <param name="resembling">An album whose merge proposes the albums that resemble it (<see cref="MergeSuggestions"/>):
    /// the list is then restricted to them, the closest first.</param>
    private static async Task<AlbumAdminList> ListForAdministrationAsync(
        BdthequeDbContext context, [AsParameters] AlbumFilters filters, CancellationToken cancellationToken,
        int page = 1, int pageSize = Paging.DefaultSize, Guid? resembling = null)
    {
        var albums = await filters.ApplyAsync(context.Albums.AsNoTracking(), context, cancellationToken);
        var ranked = resembling is { } subject ? await ResemblingAsync(context, subject, pageSize, cancellationToken) : null;
        if (ranked is not null)
            albums = albums.Where(a => ranked.Contains(a.Id));
        var rows = await albums.OrderByAlbum(a => a).ToPageAsync(page, pageSize, AdminListItem, cancellationToken);
        return new AlbumAdminList(
            new Page<AlbumAdminListItem>(
                MergeSuggestions.InRankOrder(rows.Items.Select(WithEachAuthorOnce), ranked, row => row.Album.Id), rows.Number, rows.Size,
                rows.TotalCount),
            await filters.NameAsync(context, cancellationToken));
    }

    /// <summary>
    /// The albums that resemble an album (fonctionnel/regles-metier/fusion-des-fiches.md): by its label, series and title, a title
    /// within the other included (« Le Lotus bleu », « Tintin – Le Lotus bleu »), or, whatever their titles, two albums of a
    /// same series and a same volume. An album without a title of its own resembles another only by its series and volume:
    /// its label, the series and the volume, would be a character away from that of every other volume of its series.
    /// </summary>
    private static async Task<IReadOnlyList<Guid>> ResemblingAsync(
        BdthequeDbContext context, Guid subject, int count, CancellationToken cancellationToken)
    {
        var albums = await context.Albums.AsNoTracking()
            .OrderByAlbum(a => a)
            .Select(a => new { a.Id, a.Title, a.SeriesId, SeriesTitle = a.Series!.Title, a.VolumeNumber })
            .ToListAsync(cancellationToken);
        return MergeSuggestions.Rank(albums, subject, a => a.Id, (a, b) =>
            a.SeriesId is not null && a.SeriesId == b.SeriesId && a.VolumeNumber is not null && a.VolumeNumber == b.VolumeNumber
                ? Resemblance.Identical
                : Resemblance.Compare(Texts(a.Title, a.SeriesTitle), Texts(b.Title, b.SeriesTitle), inclusion: true), count);

        // The title alone, and with its series, as its label reads; none for an album without a title of its own.
        static string?[] Texts(string? title, string? seriesTitle) => (title, seriesTitle) switch
        {
            (null, _) => [],
            (_, null) => [title],
            _ => [title, $"{seriesTitle} {title}"],
        };
    }

    /// <summary>
    /// The page of the editions and purchase intents of an album (visuel/editions-album/contenu.md): the album in brief, as
    /// the list of the albums presents it, its owned editions by year, as the record of the album orders them, and its
    /// intents, each saying whether it turns into one on the other target, by the rule of the domain.
    /// </summary>
    private static async Task<AlbumEditionsPage> GetEditionsPageAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // A single snapshot for the reads below, which are assembled into one page.
        await using var snapshot = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var album = context.Albums.AsNoTracking().Where(a => a.Id == id);
        var row = await album.Select(AdminListItem).SingleOrDefaultAsync(cancellationToken)
                  ?? throw new EntityNotFoundException(typeof(Album), id);
        var createdAt = await album.Select(a => a.CreatedAt).SingleAsync(cancellationToken);

        var editions = await context.Editions.AsNoTracking()
            .Where(e => e.AlbumId == id)
            .Where(CollectionMembership.IsOwned)
            .OrderBy(e => e.PublicationYear).ThenBy(e => e.Id)
            .Select(CatalogExpressions.Expand((Edition e) => new
            {
                Edition = e.ToSummary(),
                Cover = CatalogExpressions.ToCover(e.CoverVisualOf()),
                e.Category,
                e.Binding,
                e.PageCount,
                e.AcquisitionMode,
                e.AcquisitionDate,
                VisualCount = e.Visuals.Count,
            }))
            .ToListAsync(cancellationToken);

        var intents = await context.PurchaseIntents.AsNoTracking()
            .Where(p => p.AlbumId == id)
            .OrderBy(p => p.Id)
            .Select(CatalogExpressions.Expand((PurchaseIntent p) => new
            {
                p.Id,
                Edition = p.Edition == null ? null : p.Edition.ToSummary(),
                Traits = p.Edition == null ? null : p.Edition.ToTraits(),
                VisualCount = p.Edition == null ? 0 : p.Edition.Visuals.Count,
                // fonctionnel/regles-metier/intention-d-acquisition.md: an intent on an edition turns into one on the album only when
                // that edition is the only one of the album; one on the album, which has no edition, always into one on an edition.
                IsConvertible = p.EditionId == null || p.Album.Editions.Count == 1,
                p.CreatedAt,
            }))
            .ToListAsync(cancellationToken);

        return new AlbumEditionsPage(
            WithEachAuthorOnce(row),
            createdAt,
            [.. editions.Select(e => new AlbumEditionItem(
                e.Edition, e.Cover, EnumMapping.Map<ContractEnums.EditionCategory>(e.Category), EnumMapping.Map<ContractEnums.BindingType>(e.Binding),
                e.PageCount, EnumMapping.Map<ContractEnums.AcquisitionMode>(e.AcquisitionMode)!.Value, e.AcquisitionDate?.Year, e.VisualCount))],
            [.. intents.Select(p => new AlbumPurchaseIntentItem(
                p.Id, p.Edition, p.Traits, p.VisualCount, p.IsConvertible, p.CreatedAt))]);
    }

    private static AlbumAdminListItem WithEachAuthorOnce(AlbumAdminListItem item) => item with { Authors = EachOnce(item.Authors) };

    // An author credited with several roles is one of the authors once, at the place of their first role: told apart once
    // read, the query crediting them once per role.
    private static IReadOnlyList<AuthorListItem> EachOnce(IReadOnlyList<AuthorListItem> authors) => [.. authors.DistinctBy(a => a.Id)];

    private static readonly Expression<Func<Album, AlbumAdminListItem>> AdminListItem = CatalogExpressions.Expand((Album a) => new AlbumAdminListItem(
        a.ToSummary(),
        CatalogExpressions.ToCover(a.CoverVisualOf()),
        a.CreditedAuthors(),
        a.FirstPublicationYear,
        a.FirstPublicationMonth,
        a.Editions.Count(e => e.IsOwned()),
        a.PurchaseIntents.Count,
        a.CollectionState(),
        a.ModifiedAt,
        EF.Property<uint>(a, BdthequeDbContext.VersionProperty)));

    private static async Task<AlbumForm> GetAsync(Guid id, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        // Tracked, for the version of the aggregate (BdthequeDbContext.VersionOf): the context lives
        // for this request only.
        var album = await WithAssociations(context.Albums).SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw new EntityNotFoundException(typeof(Album), id);
        return await ToFormAsync(context, album, cancellationToken);
    }

    private static async Task<Created<AlbumForm>> CreateAsync(
        AlbumContent request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var added = await FormReferences.CreateAddedAsync(context, fields, request.NewRecords, cancellationToken);
        var series = await LoadSeriesAsync(request.SeriesId, added, context, cancellationToken);
        var contributions = await FormReferences.LoadContributionsAsync(context, request.Contributions, cancellationToken, added.Authors);
        var album = fields.Create("", () => new Album(request.Title, series, contributions), EnteredOtherwise);
        await ApplyAsync(album, request, added, context, cancellationToken);
        context.Albums.Add(album);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Created($"/admin/albums/{album.Id}", await ToFormAsync(context, album, cancellationToken));
    }

    private static async Task<AlbumForm> UpdateAsync(
        Guid id, UpdateAlbumRequest request, BdthequeDbContext context, ContentFields fields, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var album = await context.LoadAggregateForWriteAsync<Album>(id, request.Version, WithAssociations, cancellationToken);
        fields.Place(album, renamed: EnteredOtherwise);
        var content = request.Content;
        var added = await FormReferences.CreateAddedAsync(context, fields, content.NewRecords, cancellationToken);
        var series = await LoadSeriesAsync(content.SeriesId, added, context, cancellationToken);
        var contributions = await FormReferences.LoadContributionsAsync(context, content.Contributions, cancellationToken, added.Authors);

        album.SetTitleSeriesAndContributions(content.Title, series, contributions);
        await ApplyAsync(album, content, added, context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToFormAsync(context, album, cancellationToken);
    }

    // The field of the content that enters the sort key of the album, which an error designates.
    private static readonly Dictionary<string, string> EnteredOtherwise = new() { [nameof(Album.SortKey)] = "/manualSortKey" };

    // The editions, intents and contributions are always loaded with the album (AlbumConfiguration); its series and the
    // authors of its contributions, for the form to name them.
    private static IQueryable<Album> WithAssociations(IQueryable<Album> query) =>
        query.Include(a => a.Genres).Include(a => a.Universes).Include(a => a.Series).Include(a => a.Contributions).ThenInclude(c => c.Author);

    // The series of the album, unless the form added it on the spot, created with the album.
    private static async Task<Series?> LoadSeriesAsync(
        Guid? seriesId, AddedRecords added, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        if (seriesId is not { } id)
            return null;
        if (added.Series.TryGetValue(id, out var series))
            return series;
        return await context.Series.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
               ?? throw new EntityNotFoundException(typeof(Series), id);
    }

    /// <summary>
    /// Applies the rest of the form to the album, its title, series and contributions being set
    /// together beforehand: the genres and universes it references must still exist (a record
    /// deleted since the form was read is a functional error), unless the form added them on the spot.
    /// </summary>
    private static async Task ApplyAsync(
        Album album, AlbumContent content, AddedRecords added, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        if (content.ManualSortKey is { } manualSortKey)
            album.SetSortKey(manualSortKey);
        else
            album.ResetSortKey();
        album.SetTypeAndVolumeRange(
            EnumMapping.Map<DomainEnums.AlbumType>(content.Type)!.Value, content.StartVolumeNumber, content.EndVolumeNumber);
        album.SetSpecialIssue(content.IsSpecialIssue);
        album.SetVolumeNumber(content.VolumeNumber);
        album.SetFirstPublicationDate(content.FirstPublicationYear, content.FirstPublicationMonth);
        album.SetSummary(content.Summary);
        album.SetPersonalNotes(content.PersonalNotes);
        album.SetRating(EnumMapping.Map<DomainEnums.AlbumRating>(content.Rating));

        var genres = await FormReferences.LoadAsync(context.Genres, content.GenreIds, cancellationToken, added.Genres);
        FormReferences.ReplaceAssociations(album.Genres, genres, album.AddGenre, album.RemoveGenre);
        var universes = await FormReferences.LoadAsync(context.Universes, content.UniverseIds, cancellationToken, added.Universes);
        FormReferences.ReplaceAssociations(album.Universes, universes, album.AddUniverse, album.RemoveUniverse);
    }

    private static async Task<AlbumForm> ToFormAsync(BdthequeDbContext context, Album album, CancellationToken cancellationToken) =>
        new(album.Id, album.SortKey,
            new AlbumContent(
                album.Title,
                album.IsManualSortKey ? album.SortKey : null,
                album.SeriesId,
                EnumMapping.Map<ContractEnums.AlbumType>(album.Type)!.Value,
                album.IsSpecialIssue,
                album.VolumeNumber,
                album.StartVolumeNumber,
                album.EndVolumeNumber,
                album.FirstPublicationYear,
                album.FirstPublicationMonth,
                album.Summary,
                album.PersonalNotes,
                EnumMapping.Map<ContractEnums.AlbumRating>(album.Rating),
                [.. album.Genres.Select(g => g.Id).Order()],
                [.. album.Universes.Select(u => u.Id).Order()],
                FormReferences.ToContents(album.Contributions)),
            context.VersionOf(album),
            new AlbumFormReferences(
                album.Series is { } series ? new SeriesListItem(series.Id, series.Title) : null,
                FormReferences.Genres(album.Genres),
                await FormReferences.UniversesInHierarchyAsync(context, album.Universes, cancellationToken),
                FormReferences.Authors(album.Contributions)),
            await CollectionStateAsync(context, album.Id, cancellationToken),
            album.CreatedAt,
            album.ModifiedAt);

    // Read by the expression the lists share, once the album is saved: never computed a second way.
    private static Task<AlbumCollectionState> CollectionStateAsync(BdthequeDbContext context, Guid id, CancellationToken cancellationToken) =>
        context.Albums.AsNoTracking().Where(a => a.Id == id)
            .Select(CatalogExpressions.Expand((Album a) => a.CollectionState())).SingleAsync(cancellationToken);
}
